using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    public float swingDamageScale = 3.5f;
    Vector3 lastPos;

    void FixedUpdate()
    {
        if (!IsServer) return;
        Vector3 delta = transform.position - lastPos;
        float speed = delta.magnitude / Mathf.Max(Time.fixedDeltaTime, .001f);
        if (speed > 4f)
        {
            foreach (var h in Physics.OverlapSphere(transform.position, .6f))
            {
                var c = h.GetComponentInParent<CombatBody>();
                if (c == null || c.GetComponent<RobotBrain>() == null) continue;
                c.TakeServerDamage(Mathf.Min(22f, speed * swingDamageScale));
                c.ServerKnockback((c.transform.position-transform.position).normalized + Vector3.up*.15f, speed*.9f);
            }
        }
        lastPos = transform.position;
    }
}
