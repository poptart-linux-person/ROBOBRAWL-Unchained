using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class GorillaLocomotion : MonoBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public float bodyRadius=.22f;
    public float handPushMultiplier=1.65f;
    public float gravity=-18f;
    public LayerMask locomotionMask=~0;
    CharacterController controller; Vector3 lastLeft,lastRight,velocity;

    void Awake(){controller=GetComponent<CharacterController>();controller.radius=bodyRadius;controller.height=.65f;lastLeft=transform.position+Vector3.left*.35f;lastRight=transform.position+Vector3.right*.35f;}
    void Update()
    {
        var no=GetComponent<NetworkObject>(); if(no!=null && !no.IsOwner) return;
        Vector3 lp=GetHandPosition(XRNode.LeftHand,leftHand,Vector3.left), rp=GetHandPosition(XRNode.RightHand,rightHand,Vector3.right);
        bool l=CheckPalm(lp,ref lastLeft,out var ld), r=CheckPalm(rp,ref lastRight,out var rd);
        Vector3 climb=Vector3.zero; if(l)climb-=ld*handPushMultiplier; if(r)climb-=rd*handPushMultiplier;
        if(l||r)velocity.y=Mathf.Min(velocity.y,0f); else velocity.y+=gravity*Time.deltaTime;
        controller.Move((climb+velocity)*Time.deltaTime); if(controller.isGrounded&&velocity.y<0)velocity.y=-1.5f;
    }
    Vector3 GetHandPosition(XRNode node,Transform fallback,Vector3 side)
    {
        var d=InputDevices.GetDeviceAtXRNode(node);
        if(d.isValid&&d.TryGetFeatureValue(CommonUsages.devicePosition,out var pos)){var origin=Camera.main?Camera.main.transform:transform;return origin.TransformPoint(pos);}
        return fallback?fallback.position:transform.position+side*.45f+Vector3.up*.15f;
    }
    bool CheckPalm(Vector3 p,ref Vector3 last,out Vector3 delta)
    {
        delta=p-last; bool touching=Physics.CheckSphere(p,.085f,locomotionMask,QueryTriggerInteraction.Ignore);
        if(!touching&&delta.sqrMagnitude>.0001f)touching=Physics.SphereCast(last,.085f,delta.normalized,out _,Mathf.Min(delta.magnitude,.25f),locomotionMask,QueryTriggerInteraction.Ignore);
        if(!touching)delta=Vector3.zero; last=p; return touching;
    }
}
