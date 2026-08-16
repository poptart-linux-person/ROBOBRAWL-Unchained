using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(CharacterController))]
public class GorillaLocomotion : MonoBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public float bodyRadius = .22f;
    public float armReach = 1.35f;
    public float handPushMultiplier = 1.65f;
    public float gravity = -18f;
    public LayerMask locomotionMask = ~0;

    CharacterController controller;
    Vector3 lastLeft;
    Vector3 lastRight;
    Vector3 velocity;
    bool leftTouch;
    bool rightTouch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.radius = bodyRadius;
        controller.height = .65f;
        lastLeft = transform.position + Vector3.left * .35f;
        lastRight = transform.position + Vector3.right * .35f;
    }

    void Update()
    {
        if (head != null)
        {
            Vector3 headOffset = head.position - transform.position;
            headOffset.y = 0f;
            if (headOffset.magnitude > .5f) transform.position += headOffset;
        }

        Vector3 lp = GetHandPosition(XRNode.LeftHand, leftHand, Vector3.left);
        Vector3 rp = GetHandPosition(XRNode.RightHand, rightHand, Vector3.right);
        bool l = CheckPalm(lp, ref lastLeft, out Vector3 lDelta);
        bool r = CheckPalm(rp, ref lastRight, out Vector3 rDelta);
        leftTouch = l;
        rightTouch = r;

        Vector3 climbMove = Vector3.zero;
        if (l) climbMove -= lDelta * handPushMultiplier;
        if (r) climbMove -= rDelta * handPushMultiplier;
        if (l || r) velocity.y = Mathf.Min(velocity.y, 0f);
        else velocity.y += gravity * Time.deltaTime;

        controller.Move((climbMove + velocity) * Time.deltaTime);
        if (controller.isGrounded && velocity.y < 0f) velocity.y = -1.5f;
        transform.position = new Vector3(transform.position.x, Mathf.Max(.35f, transform.position.y), transform.position.z);
    }

    Vector3 GetHandPosition(XRNode node, Transform fallback, Vector3 side)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
        {
            Transform origin = Camera.main ? Camera.main.transform : transform;
            return origin.TransformPoint(pos);
        }
        return fallback ? fallback.position : transform.position + side * .45f + Vector3.up * .15f;
    }

    bool CheckPalm(Vector3 handPos, ref Vector3 last, out Vector3 delta)
    {
        delta = handPos - last;
        bool touching = Physics.CheckSphere(handPos, .085f, locomotionMask, QueryTriggerInteraction.Ignore);
        if (!touching) touching = Physics.SphereCast(last, .085f, delta.normalized, out _, Mathf.Min(delta.magnitude, .25f), locomotionMask, QueryTriggerInteraction.Ignore);
        if (!touching) delta = Vector3.zero;
        last = handPos;
        return touching;
    }
}
