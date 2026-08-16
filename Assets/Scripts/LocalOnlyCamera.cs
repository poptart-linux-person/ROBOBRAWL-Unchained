using Unity.Netcode;
using UnityEngine;

public class LocalOnlyCamera : NetworkBehaviour
{
    Camera cam;
    void Awake() => cam = GetComponent<Camera>();
    public override void OnNetworkSpawn()
    {
        if (cam != null) cam.enabled = IsOwner;
        if (IsOwner) gameObject.tag = "MainCamera";
    }
}
