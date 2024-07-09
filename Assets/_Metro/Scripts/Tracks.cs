using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

/// <summary>
/// Container behavior for TrackSegments. Coupled to TransportLine such that
/// each TransportLine has a Tracks child that manages TrackSegments.
/// </summary>
public class Tracks : MonoBehaviour
{

  #region Parent References

  public MetroGame gameInstance;
  public TransportLine line;

  #endregion

  // public List<Vector3> points = new List<Vector3>();
  public List<Vector3> cp = new List<Vector3>();
  public List<float> lengths = new List<float>();

  public TrackSegment head;
  public TrackSegment tail;
  public List<TrackSegment> segments = new List<TrackSegment>();
  public List<TrackSegment> ghostSegments = new List<TrackSegment>();

  public List<TrackSegment> uiSegments = new List<TrackSegment>();

  public bool needsUpdate = true;

  // total length of tracks
  public float totalLength;

  public void Init()
  {
    for (int i = 0; i < 2; i++)
    {
      uiSegments.Add(CreateSegment(-10));
      // uiSegments[i].gameObject.SetActive(false);
    }
    head = CreateSegment(-1);
    // head.gameObject.SetActive(false);
    tail = CreateSegment(9999);
    // tail.gameObject.SetActive(false);
  }

  // Update
  public void ProcessTick()
  {
    if (!needsUpdate)
      return;
    // if(line.stops.Count == 0) return;

    UpdateControlPoints();
    UpdateSegments();
    UpdateLengths();

    needsUpdate = false;
    // makes a shallow copy of track lengths
    gameInstance.trackLengths = this.lengths;
  }

  void UpdateLengths()
  {
    lengths.Clear();
    totalLength = 0;

    for (int i = 0; i < line.stops.Count - 1; i++)
    {
      var p0 = line.stops[i].transform.position;
      var p1 = line.stops[i + 1].transform.position;

      lengths.Add(Vector3.Distance(p0, p1));

      Debug.Log("tracks length: " + this.lengths[i]);
      this.totalLength += this.lengths[i];
      Debug.Log("total tracks length update: " + this.totalLength);

      gameInstance.totalTrackLength = this.totalLength;
    }
  }

  void UpdateControlPoints()
  {
    cp.Clear();
    if (line.stops.Count == 0)
      return;

    cp.Add(line.stops[0].transform.position);

    for (int i = 0; i < line.stops.Count - 1; i++)
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
      if (i < line.stops.Count - 2)
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
    foreach (var s in segments)
      GameObject.Destroy(s.gameObject);
    segments.Clear();

    for (int i = 0; i < line.stops.Count - 1; i++)
    {
      var track = CreateSegment(i);

      track.cp[0] = cp[3 * i];
      track.cp[1] = cp[3 * i + 1];
      track.cp[2] = cp[3 * i + 2];
      track.cp[3] = cp[3 * i + 3];
      track.needsUpdate = true;

      segments.Add(track);
    }
    if (line.stops.Count > 1)
    {
      head.cp[0] = cp[0];
      head.cp[1] = cp[0];
      head.cp[2] = cp[0] - (cp[1] - cp[0]).normalized * 0.15f;
      head.cp[3] = cp[0] - (cp[1] - cp[0]).normalized * 0.15f;
      head.gameObject.SetActive(true);
      head.needsUpdate = true;
      tail.cp[0] = cp[cp.Count - 1];
      tail.cp[1] = cp[cp.Count - 1];
      tail.cp[2] = cp[cp.Count - 1] -
                   (cp[cp.Count - 2] - cp[cp.Count - 1]).normalized * 0.15f;
      tail.cp[3] = cp[cp.Count - 1] -
                   (cp[cp.Count - 2] - cp[cp.Count - 1]).normalized * 0.15f;
      tail.gameObject.SetActive(true);
      tail.index = segments.Count;
      tail.needsUpdate = true;
    }
    else
    {
      head.gameObject.SetActive(false);
      tail.gameObject.SetActive(false);
    }
  }

  TrackSegment CreateSegment(int index)
  {
    var go = new GameObject();
    go.name = "Track Segment";
    go.transform.SetParent(this.transform);
    var track = go.AddComponent<TrackSegment>();
    track.line = line;
    track.index = index;
    track.gameInstance = gameInstance;
    return track;
  }

  public void UpdateUISegment(int i, Vector3 pos, int stopIndex)
  {
    // Debug.Log("UpdateUISegment");
    // Debug.Log(i + " " + stopIndex);
    // Debug.Log(uiSegments.Count + " " + cp.Count);
    if (cp.Count == 0)
      this.UpdateControlPoints();
    uiSegments[i].gameObject.SetActive(true);
    if (stopIndex == -1)
      stopIndex = 0;
    uiSegments[i].cp[0] = cp[3 * stopIndex];
    uiSegments[i].cp[1] = cp[3 * stopIndex];
    uiSegments[i].cp[2] = pos;
    uiSegments[i].cp[3] = pos;
    uiSegments[i].needsUpdate = true;
  }

  public void DisableUISegments()
  {
    foreach (var s in uiSegments)
      s.gameObject.SetActive(false);
  }

  public Vector3 GetPosition(float p)
  {
    if (segments.Count == 0)
      return new Vector3(0f, -1000f, 0f);
    var x = p * segments.Count;
    var i = (int)x;
    var v = x - i;
    Vector3 pos;
    if (i == segments.Count)
      pos = segments[segments.Count - 1].Interpolate(1.0f);
    else
      pos = segments[i].Interpolate(v);
    return pos;
  }

  /// <summary>
  /// This function returns the speed multiplier of the train based on the
  /// position you give it in the track sequence of tracksegments. It takes into
  /// account the other segments in the Track.
  /// </summary>
  /// <param name="p">0 -> 1 value shared between all the TrackSegments. Check
  /// the update function for train.cs to see how this is used.</param> <param
  /// name="fullSpeedDistance">The unity distance between stations in which the
  /// returned multiplier should be 1. It linearly interpolates from there. For
  /// example, if the current segment distance is 10, and the fullSpeedDistance
  /// passed is 20, the returned speed multiplier would be 2. Since the train
  /// only has to cover half the distance, it does so in half the time/twice the
  /// speed.</param> <returns></returns>
  public float GetTrainDistanceSpeedMultiplier(float p,
                                               float fullSpeedDistance)
  {
    if (segments.Count == 0)
      return 0.0f;
    var x = p * segments.Count;
    var i = (int)x;
    var v = x - i;
    float totalSegmentDistance = 0;

    for (int w = 0; w < segments.Count; w++)
    {
      totalSegmentDistance += segments[w].getRoughSegmentDistance();
    }

    // The logic here is that the speed on any given segment should be
    // fullSpeedDistance / thatSegmentsDistance, and that should be multiplied
    // by the percentage of the total segment length (addition of all the
    // segment lengths) that that segment's distance is. That equation would be
    // (fullSpeedDistance / roughSegmentDistance) * (roughSegmentDistance /
    // totalSegmentDistance), shortened to fullSpeedDistance /
    // totalSegmentDistance

    float speedMult = fullSpeedDistance / totalSegmentDistance;

    return speedMult;
  }

  public Vector3 GetVelocity(float p)
  {
    var x = p * segments.Count;
    var i = (int)x;
    var v = x - i;
    Vector3 vel;
    if (i == segments.Count)
      vel = segments[segments.Count - 1].Derivative(1.0f);
    else
      vel = segments[i].Derivative(v);
    return vel;
  }
}
