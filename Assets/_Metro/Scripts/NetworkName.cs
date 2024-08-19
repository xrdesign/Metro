using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkName : NetworkBehaviour
{
    [Tooltip("The name of the network object")]
    [Networked]
    public string syncedName {get; set;} = "NetworkObject";

    [Networked]
    public uint prefabId {get; set;} = BakingObjectProvider.CUSTOM_PREFAB_FLAG_MIN;

    // init the game object name by the network object name Spawned()
    public override void Spawned()
    {
        gameObject.name = syncedName;
    }

}
