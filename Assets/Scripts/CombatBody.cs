using Unity.Netcode;
using UnityEngine;

public class CombatBody : NetworkBehaviour
{
    public float maxHealth = 100f;
    public float impactDamageScale = 5f;
    public float maxImpactDamage = 30f;
    public float launchScale = 1.2f;

    public NetworkVariable<float> Health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> Ragdolled = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    Rigidbody body;
    bool IsRobot => GetComponent<RobotBrain>() != null;

    void Awake() => body = GetComponent<Rigidbody>();
    public override void OnNetworkSpawn() { if (IsServer) Health.Value = maxHealth; }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !enabled || body == null) return;
        float speed = collision.relativeVelocity.magnitude;
        if (speed < 2.5f) return;
        float damage = Mathf.Min(maxImpactDamage, Mathf.Pow(speed - 2.25f, 1.35f) * impactDamageScale * 0.1f);
        var otherBody = collision.collider.GetComponentInParent<CombatBody>();
        if (otherBody != null && otherBody != this)
        {
            damage *= 1.25f;
            otherBody.TakeServerDamage(damage);
            otherBody.ServerKnockback(collision.relativeVelocity, speed * launchScale);
        }
        if (IsRobot) TakeServerDamage(damage);
        if (collision.collider.name.Contains("Wall") && speed > 5f) TakeServerDamage(damage * 1.5f);
    }

    public void TakeServerDamage(float amount)
    {
        if (!IsServer || Health.Value <= 0f) return;
        Health.Value = Mathf.Max(0f, Health.Value - amount);
        if (Health.Value <= 0f) EnterRagdoll();
    }

    public void ServerKnockback(Vector3 direction, float force)
    {
        if (!IsServer || body == null || direction.sqrMagnitude < 0.01f) return;
        body.AddForce(direction.normalized * force, ForceMode.Impulse);
        if (force > 9f) EnterRagdoll();
    }

    public void EnterRagdoll()
    {
        if (!IsServer || Ragdolled.Value) return;
        Ragdolled.Value = true;
        if (body != null)
        {
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.None;
            body.mass = 45f;
            body.AddTorque(Random.onUnitSphere * 8f, ForceMode.Impulse);
        }
        var robot = GetComponent<RobotBrain>();
        if (robot != null) robot.enabled = false;
        Invoke(nameof(RespawnOrReset), 4f);
    }

    void RespawnOrReset()
    {
        if (!IsServer || !IsRobot) return;
        Health.Value = maxHealth;
        Ragdolled.Value = false;
        transform.position = RobotArenaBootstrap.GetRobotSpawn();
        transform.rotation = Quaternion.identity;
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.mass = 25f;
        }
        var robot = GetComponent<RobotBrain>();
        if (robot != null) robot.enabled = true;
    }
}
