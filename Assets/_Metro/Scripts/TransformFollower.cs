using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TransformFollower : NetworkBehaviour
{
    public GameObject target;
    public string targetName;

    public bool notShownOnInputAuthority = true;

    private Transform[] children;

    private Renderer[] renderers;
    // Start is called before the first frame update
    public override void Spawned()
    {
        children = GetComponentsInChildren<Transform>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    public override void Render()
    {
        if(HasInputAuthority && notShownOnInputAuthority){
            foreach(var r in renderers){
                r.enabled = false;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            if (target == null)
            {
                target = GameObject.Find(targetName);
            }

            if (target != null && target.activeSelf)
            {
                // Send an RPC to the host to update the transform
                RPC_UpdateTransform(target.transform.position, target.transform.rotation);
            } else {
                
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateTransform(Vector3 position, Quaternion rotation)
    {
        // Host updates the network transform
        transform.position = position;
        transform.rotation = rotation;
    }
}
