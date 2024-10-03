using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Fusion;

/// <summary>
/// Container behavior for TrackSegments. Coupled to TransportLine such that each TransportLine has a Tracks child that manages TrackSegments.
/// </summary>
public class Tracks : NetworkBehaviour
{

    #region Parent References

    [Networked] public MetroGame gameInstance { get; set; }
    [Networked] public TransportLine line { get; set; }

    #endregion

    public List<Vector3> cp = new List<Vector3>();
    [Networked, Capacity(80)] public NetworkArray<float> lengths => default;
    [Networked] public int lengthCount { get; set; } = 0;

    [Networked] public TrackSegment head { get; set; }
    [Networked] public TrackSegment tail { get; set; }
    [Networked, Capacity(20)] public NetworkArray<TrackSegment> segments => default;
    [Networked] public int segmentCount { get; set; } = 0;

    [Networked, Capacity(10)] public NetworkArray<TrackSegment> uiSegments => default;
    [Networked] public int uiSegmentCount { get; set; } = 0;


    [Networked] public bool needsUpdate { get; set; } = true;

    //total length of tracks
    [Networked] public float totalLength { get; set; } = 0;

    private bool isInitialized = false;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // Start()
            for (int i = 0; i < 2; i++)
            {
                uiSegmentCount = FusionUtils.Add(uiSegments, uiSegmentCount, CreateSegment(-10));
                // uiSegments[i].gameObject.SetActive(false);
            }
            head = CreateSegment(-1);
            // head.gameObject.SetActive(false);
            tail = CreateSegment(9999);
            // tail.gameObject.SetActive(false);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NewTrackOnClient()
    {
        if (HasStateAuthority)
        {
            needsUpdate = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!needsUpdate) return;
        UpdateTracks();
        RPC_UpdateTracks();
        needsUpdate = false;
    }

    public override void Render()
    {
        if (!isInitialized)
        {
            UpdateTracks();
            isInitialized = true;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = true)]
    public void RPC_UpdateTracks()
    {
        UpdateTracks();
    }

    public void UpdateTracks()
    {
        UpdateControlPoints();
        // Debug.Log("Control point count: " + cp.Count);
        UpdateSegments();
        // Debug.Log("Segment count: " + segmentCount);
        UpdateLengths();
        // Debug.Log("UpdateTracks");
        // Debug.Log("total tracks length: " + this.totalLength);
        //makes a shallow copy of track lengths
        // gameInstance.trackLengths = this.lengths;
        if (HasStateAuthority)
        {
            gameInstance.trackLengths.CopyFrom(lengths.ToArray(), 0, lengthCount);
            gameInstance.trackLengthCount = this.lengthCount;
        }
    }

    void UpdateLengths()
    {
        lengths.Clear();
        totalLength = 0;

        for (int i = 0; i < line.stopCount - 1; i++)
        {
            var p0 = line.stops[i].transform.position;
            var p1 = line.stops[i + 1].transform.position;

            // lengths.Add(Vector3.Distance(p0,p1));
            lengthCount = FusionUtils.Add(lengths, lengthCount, Vector3.Distance(p0, p1));

            Debug.Log("tracks length: " + this.lengths[i]);
            this.totalLength += this.lengths[i];
            Debug.Log("total tracks length update: " + this.totalLength);

            gameInstance.totalTrackLength = this.totalLength;
        }
    }

    void UpdateControlPoints()
    {
        cp.Clear();
        if (line.stopCount == 0) return;

        cp.Add(line.stops[0].transform.position);
        Debug.Log("LineStopCount:" + line.stopCount);
        for (int i = 0; i < line.stopCount - 1; i++)
        {
            var p0 = line.stops[i].transform.position;
            var p1 = line.stops[i + 1].transform.position;

            // default line
            var cp0 = p0 + (p1 - p0) * 0.15f;
            var cp1 = p1 + (p0 - p1) * 0.15f;

            // look back to i-1 
            if (i > 0)
            {
                var p00 = line.stops[i - 1].transform.position;
                var v = Vector3.Normalize(p1 - p00);
                cp0 = p0 + v * 0.1f;

            }
            // look forward to i+2
            if (i < line.stopCount - 2)
            {
                var p2 = line.stops[i + 2].transform.position;
                var v = Vector3.Normalize(p2 - p0);
                cp1 = p1 - v * 0.1f;
            }
            cp.Add(cp0);
            cp.Add(cp1);
            cp.Add(p1);
        }
    }

    void UpdateSegments()
    {

        if (HasStateAuthority)
        {
            foreach (var s in segments.GetFilledElements(segmentCount))
            {
                Runner.Despawn(s.Object);
            }
            segments.Clear();
            segmentCount = 0;
        }

        for (int i = 0; i < line.stopCount - 1; i++)
        {

            TrackSegment track = default;
            if (HasStateAuthority)
            {
                track = CreateSegment(i);
                segmentCount = FusionUtils.Add(segments, segmentCount, track);
            }
            else
            {
                track = segments[i];
            }

            track.cp[0] = cp[3 * i];
            track.cp[1] = cp[3 * i + 1];
            track.cp[2] = cp[3 * i + 2];
            track.cp[3] = cp[3 * i + 3];
            track.needsUpdate = true;
        }

        if (line.stopCount > 1)
        {
            head.cp[0] = cp[0];
            head.cp[1] = cp[0];
            head.cp[2] = cp[0] - (cp[1] - cp[0]).normalized * 0.15f;
            head.cp[3] = cp[0] - (cp[1] - cp[0]).normalized * 0.15f;
            tail.cp[0] = cp[cp.Count - 1];
            tail.cp[1] = cp[cp.Count - 1];
            tail.cp[2] = cp[cp.Count - 1] - (cp[cp.Count - 2] - cp[cp.Count - 1]).normalized * 0.15f;
            tail.cp[3] = cp[cp.Count - 1] - (cp[cp.Count - 2] - cp[cp.Count - 1]).normalized * 0.15f;

            head.gameObject.SetActive(true);
            head.needsUpdate = true;
            tail.gameObject.SetActive(true);
            tail.index = segmentCount;
            tail.needsUpdate = true;
        }
        else
        {
            head.gameObject.SetActive(false);
            tail.gameObject.SetActive(false);
            // Debug.Log("No stops");
        }


    }

    TrackSegment CreateSegment(int index)
    {
        // var go = new GameObject();
        // go.name = "Track Segment";
        // go.transform.SetParent(this.transform);
        // var track = go.AddComponent<TrackSegment>();
        // track.line = line;
        // track.index = index;
        // track.gameInstance = gameInstance;

        var prefab = Resources.Load("Prefabs/TrackSegment") as GameObject;
        var track = Runner.Spawn(prefab, onBeforeSpawned: (runner, obj) =>
        {
            obj.GetComponent<NetworkName>().syncedName = "Track Segment";
            obj.transform.SetParent(this.transform);
            obj.GetComponent<TrackSegment>().line = line;
            obj.GetComponent<TrackSegment>().index = index;
            obj.GetComponent<TrackSegment>().gameInstance = gameInstance;
        }).GetComponent<TrackSegment>();
        return track;
    }

    public void UpdateUISegment(int i, Vector3 pos, int stopIndex)
    {
        RPC_UpdateUISegment(i, pos, stopIndex);
    }

    //[Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = true)]
    public void RPC_UpdateUISegment(int i, Vector3 pos, int stopIndex)
    {
        if (i >= uiSegmentCount) return;

        // Debug.Log(line.stopCount);
        if (cp.Count == 0 || (3 * stopIndex >= cp.Count)) UpdateControlPoints();

        // if (3 * stopIndex >= cp.Count)
        // {
        //     Debug.Log("RPC_UpdateUISegment " + i + " " + pos + " " + stopIndex);
        //     // Debug.Log("UpdateUISegment");
        //     Debug.Log(i + " " + stopIndex);
        //     Debug.Log(uiSegmentCount + " " + cp.Count);
        //     Debug.Log(line.stopCount);
        // }

        uiSegments[i].gameObject.SetActive(true);
        if (stopIndex == -1) stopIndex = 0;
        uiSegments[i].cp[0] = cp[3 * stopIndex];
        uiSegments[i].cp[1] = cp[3 * stopIndex];
        uiSegments[i].cp[2] = pos;
        uiSegments[i].cp[3] = pos;
        uiSegments[i].needsUpdate = true;
    }

    public void DisableUISegments()
    {
        foreach (var s in uiSegments.GetFilledElements(uiSegmentCount))
            s.gameObject.SetActive(false);
    }

    public Vector3 GetPosition(float p)
    {
        if (segmentCount == 0) return new Vector3(0f, -1000f, 0f);
        var x = p * segmentCount;
        var i = (int)x;
        var v = x - i;
        Vector3 pos;
        if (i == segmentCount) pos = segments[segmentCount - 1].Interpolate(1.0f);
        else pos = segments[i].Interpolate(v);
        return pos;
    }

    /// <summary>
    /// This function returns the speed multiplier of the train based on the position you give it in the track sequence of tracksegments.
    /// It takes into account the other segments in the Track.
    /// </summary>
    /// <param name="p">0 -> 1 value shared between all the TrackSegments. Check the update function for train.cs to see how this is used.</param>
    /// <param name="fullSpeedDistance">The unity distance between stations in which the returned multiplier should be 1. It linearly interpolates from there. For example, if the current segment distance is 10, and the fullSpeedDistance passed is 20, the returned speed multiplier would be 2. Since the train only has to cover half the distance, it does so in half the time/twice the speed.</param>
    /// <returns></returns>
    public float GetTrainDistanceSpeedMultiplier(float p, float fullSpeedDistance)
    {
        if (segmentCount == 0) return 0.0f;
        var x = p * segmentCount;
        var i = (int)x;
        var v = x - i;
        float totalSegmentDistance = 0;

        for (int w = 0; w < segmentCount; w++)
        {
            totalSegmentDistance += segments[w].getRoughSegmentDistance();
        }

        // The logic here is that the speed on any given segment should be fullSpeedDistance / thatSegmentsDistance, and that should be multiplied by the percentage of the total segment length (addition of all the segment lengths) that that segment's distance is.
        // That equation would be (fullSpeedDistance / roughSegmentDistance) * (roughSegmentDistance / totalSegmentDistance), shortened to fullSpeedDistance / totalSegmentDistance


        float speedMult = fullSpeedDistance / totalSegmentDistance;


        return speedMult;
    }

    public Vector3 GetVelocity(float p)
    {
        var x = p * segmentCount;
        var i = (int)x;
        var v = x - i;
        Vector3 vel;
        if (i == segmentCount) vel = segments[segmentCount - 1].Derivative(1.0f);
        else vel = segments[i].Derivative(v);
        return vel;
    }






}
