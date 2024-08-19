using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct Passenger : INetworkStruct
{
    public StationType destination { get; set; }
    public NetworkId gameInstance { get; set; }
    public float waitTime { get; set; }
    public float travelTime { get; set; }

    public float totalTime { get; set; }

    [Networked, Capacity(20)] public NetworkArray<NetworkId> routes => default;
    public int routeCount { get; set; }

}
