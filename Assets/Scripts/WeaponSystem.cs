using Unity.Netcode;
using UnityEngine;

public enum RoboWeaponType { Bat, Pipe, Hammer, Cone, Crate, BuzzSaw, XSaw }

public class RoboWeapon : NetworkBehaviour
{
    public RoboWeaponType type;
    public float damage = 18f;
    public float throwForce = 7f;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = type == RoboWeaponType.XSaw || type == RoboWeaponType.BuzzSaw ? 5f : 2f;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        var target = collision.collider.GetComponentInParent<CombatBody>();
        if (target == null) return;
        float speed = collision.relativeVelocity.magnitude;
        if (speed < 1.5f) return;
        float multiplier = type == RoboWeaponType.BuzzSaw || type == RoboWeaponType.XSaw ? 1.65f : 1f;
        target.TakeServerDamage(Mathf.Min(40f, damage + speed * 2.2f) * multiplier);
        if (speed > 6f) target.ServerKnockback(collision.relativeVelocity, speed * 0.9f);
    }
}

public static class WeaponFactory
{
    public static RoboWeapon Spawn(RoboWeaponType type, Vector3 position, Quaternion rotation, bool networked = false)
    {
        GameObject root;
        if (type == RoboWeaponType.BuzzSaw || type == RoboWeaponType.XSaw)
            root = CreateSaw(type, position, rotation);
        else
            root = GameObject.CreatePrimitive(type == RoboWeaponType.Cone ? PrimitiveType.Cylinder : PrimitiveType.Cube);

        root.name = type.ToString();
        root.transform.position = position;
        root.transform.rotation = rotation;
        if (type == RoboWeaponType.Bat) root.transform.localScale = new Vector3(.22f, 1.4f, .22f);
        if (type == RoboWeaponType.Pipe) root.transform.localScale = new Vector3(.12f, 1.8f, .12f);
        if (type == RoboWeaponType.Hammer) root.transform.localScale = new Vector3(.35f, 1.4f, .35f);
        if (type == RoboWeaponType.Crate) root.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        if (type == RoboWeaponType.Cone) root.transform.localScale = new Vector3(.65f, .9f, .65f);

        var renderer = root.GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.material.color = type == RoboWeaponType.XSaw || type == RoboWeaponType.BuzzSaw ? new Color(.85f,.08f,.08f) : new Color(.25f,.28f,.32f);
        var weapon = root.AddComponent<RoboWeapon>();
        weapon.type = type;
        weapon.damage = type == RoboWeaponType.Hammer ? 24f : type == RoboWeaponType.BuzzSaw || type == RoboWeaponType.XSaw ? 28f : 18f;
        return weapon;
    }

    static GameObject CreateSaw(RoboWeaponType type, Vector3 position, Quaternion rotation)
    {
        var root = new GameObject(type.ToString());
        root.transform.position = position;
        root.transform.rotation = rotation;
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "SawDisc";
        disc.transform.SetParent(root.transform, false);
        disc.transform.localScale = new Vector3(.7f,.08f,.7f);
        disc.transform.localRotation = Quaternion.Euler(90f,0f,0f);
        var blade = disc.GetComponent<Renderer>();
        blade.material.color = type == RoboWeaponType.XSaw ? new Color(.95f,.15f,.08f) : new Color(.7f,.72f,.76f);
        for (int i=0;i<12;i++)
        {
            var tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tooth.transform.SetParent(root.transform, false);
            tooth.transform.localScale = new Vector3(.1f,.08f,.25f);
            tooth.transform.localPosition = Quaternion.Euler(0, i*30f,0) * Vector3.forward * .42f;
            tooth.transform.localRotation = Quaternion.Euler(0,i*30f,0);
            tooth.GetComponent<Renderer>().material.color = Color.gray;
        }
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 5f;
        return root;
    }
}
