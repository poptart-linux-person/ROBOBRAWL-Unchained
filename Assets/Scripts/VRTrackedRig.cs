using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

public class VRTrackedRig : MonoBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    void Update()
    {
        var net = GetComponent<NetworkObject>();
        if (net != null && !net.IsOwner) return;
        UpdateNode(XRNode.Head, head, Vector3.up * 1.2f);
        UpdateNode(XRNode.LeftHand, leftHand, Vector3.left * .45f + Vector3.up * 1.15f);
        UpdateNode(XRNode.RightHand, rightHand, Vector3.right * .45f + Vector3.up * 1.15f);
    }

    static void UpdateNode(XRNode node, Transform target, Vector3 fallback)
    {
        if (target == null) return;
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            target.localPosition = pos;
        else
            target.localPosition = fallback;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            target.localRotation = rot;
    }
}
