using Unity.Netcode;
using UnityEngine;

public class RobotBrain : NetworkBehaviour
{
    public float walkSpeed = 2.5f;
    public float sprintSpeed = 5.5f;
    public float attackRange = 2.2f;
    public float attackCooldown = 1.1f;

    Rigidbody rb;
    CombatBody combat;
    float nextAttack;
    Transform target;
    enum State { Roam, Chase, Attack, Evade, Stunned }
    State state;
    float stateTimer;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        combat = GetComponent<CombatBody>();
        if (IsServer)
        {
            rb.isKinematic = true;
            state = State.Roam;
            stateTimer = Random.Range(1f, 4f);
        }
    }

    void Update()
    {
        if (!IsServer || combat == null || combat.Ragdolled.Value) return;
        AcquireTarget();
        stateTimer -= Time.deltaTime;
        switch (state)
        {
            case State.Roam: Roam(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
            case State.Evade: Evade(); break;
        }
    }

    void AcquireTarget()
    {
        if (target != null && Vector3.Distance(transform.position, target.position) < 18f) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, 18f);
        float best = float.MaxValue;
        Transform found = null;
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            float d = (hit.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; found = hit.transform; }
        }
        target = found;
        state = target ? State.Chase : State.Roam;
    }

    void Roam()
    {
        if (target) { state = State.Chase; return; }
        if (stateTimer <= 0f)
        {
            stateTimer = Random.Range(2f, 6f);
            transform.Rotate(0f, Random.Range(-110f, 110f), 0f);
        }
        Move(transform.forward, walkSpeed);
    }

    void Chase()
    {
        if (!target) { state = State.Roam; return; }
        Vector3 delta = target.position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > 20f) { state = State.Chase; Move(delta.normalized, sprintSpeed); }
        else { state = State.Attack; nextAttack = Time.time; }
    }

    void Attack()
    {
        if (!target) { state = State.Roam; return; }
        Vector3 delta = target.position - transform.position;
        delta.y = 0f;
        if (delta.magnitude > attackRange + .35f) { state = State.Chase; return; }
        if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 10f);
        if (Time.time < nextAttack) return;
        nextAttack = Time.time + attackCooldown * Random.Range(.75f, 1.35f);
        var targetCombat = target.GetComponent<CombatBody>();
        if (targetCombat != null)
        {
            float damage = Random.Range(8f, 17f);
            targetCombat.TakeServerDamage(damage);
            targetCombat.ServerKnockback((target.position - transform.position).normalized + Vector3.up * .28f, Random.Range(3f, 7f));
        }
    }

    void Evade()
    {
        if (stateTimer <= 0f) state = target ? State.Chase : State.Roam;
        if (target) Move(Vector3.Cross(Vector3.up, (target.position - transform.position).normalized), sprintSpeed * .8f);
    }

    void Move(Vector3 dir, float speed)
    {
        if (dir.sqrMagnitude < .001f) return;
        Vector3 next = transform.position + dir.normalized * speed * Time.deltaTime;
        next.x = Mathf.Clamp(next.x, -31f, 31f);
        next.z = Mathf.Clamp(next.z, -22f, 22f);
        transform.position = new Vector3(next.x, 1.3f, next.z);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 7f);
    }
}
