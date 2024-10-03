using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Fusion;


public class TransportLine : NetworkBehaviour
{
    [Networked] public int id { get; set; }
    [Networked] public int uuid { get; set; }
    [Networked] public bool isDeployed { get; set; } = false;

    // public List<Station> stops = new List<Station>();
    [Networked, Capacity(20)] public NetworkArray<Station> stops => default;
    [Networked] public int stopCount { get; set; } = 0;

    // public List<Train> trains = new List<Train>();
    [Networked, Capacity(20)] public NetworkArray<Train> trains => default;
    [Networked] public int trainCount { get; set; } = 0;


    [Networked] public Tracks tracks { get; set; } = null;


    [Networked] public Color color { get; set; }
    [Networked] public int nextIndex { get; set; } = 0;

    [Networked] public MetroGame gameInstance { get; set; }


    public override void Spawned()
    {
        // Awake();
        // GameObject go  = new GameObject();
        // go.name = "Tracks, Transport Line " + color.ToString();
        // tracks = go.AddComponent<Tracks>();
        // prefab: Resources.Load("Prefabs/Tracks")
        if (!HasStateAuthority)
        {
            return;
        }
        var prefab = Resources.Load("Prefabs/Tracks") as GameObject;
        tracks = Runner.Spawn(prefab, onBeforeSpawned: (runner, obj) =>
        {
            obj.GetComponent<NetworkName>().syncedName = "Tracks, Transport Line " + color.ToString();
            obj.transform.SetParent(this.transform);
            obj.GetComponent<Tracks>().line = this;
            obj.GetComponent<Tracks>().gameInstance = gameInstance;
        }).GetComponent<Tracks>();
    }


    // append station as last stop on line
    public void AddStation(Station station)
    {
        InsertStation(stopCount, station);
    }

    // insert station as stop at arbitrary index
    public void InsertStation(int stopIndex, Station station)
    {
        RPC_InsertStation(stopIndex, station);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, TickAligned = false)]
    public void RPC_InsertStation(int stopIndex, Station station)
    {
        // if(this.stops.Contains(station)){ NetworkArray does not support Contains
        if (FusionUtils.Contains(stops, stopCount, station))
        {
            // only allow if making looping line
            // TODO
            return;
        }
        Debug.Log("insert station");
        // stops.Insert(stopIndex, station);
        stopCount = FusionUtils.Insert(stops, stopCount, stopIndex, station);
        // if(!station.lines.Contains(this)) station.lines.Add(this);
        if (!FusionUtils.Contains(station.lines, station.lineCount, this)) station.lineCount = FusionUtils.Insert(station.lines, station.lineCount, station.lineCount, this);
        if (!isDeployed)
        {
            gameInstance.linesCreated++;
        }
        isDeployed = true;
        // if(stops.Count >= 2 && trains.Count == 0){
        if (stopCount >= 2 && trainCount == 0)
        {
            if (HasStateAuthority)
            {
                AddTrain(0.0f, 1.0f);
            }
        }
        tracks.needsUpdate = true;
        gameInstance.insertions++;
        MetroManager.SendEvent($"Action: InsertStation, Game: {gameInstance.gameId}");
        RPC_NotifyStationInserted(stopIndex, station);
    }

    // notify the station is inserted
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, TickAligned = false)]
    public void RPC_NotifyStationInserted(int stopIndex, Station station)
    {
        Debug.Log("notify station inserted: " + stopIndex);
    }


    public void RemoveStation(Station station)
    {
        RPC_RemoveStation(station);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RemoveStation(Station station)
    {
        // station.lines.Remove(this);
        station.lineCount = FusionUtils.Remove(station.lines, station.lineCount, this);
        // stops.Remove(station);
        stopCount = FusionUtils.Remove(stops, stopCount, station);
        if (stopCount <= 1)
            this.RemoveAll();
        tracks.needsUpdate = true;
        gameInstance.deletions++;
        MetroManager.SendEvent($"Action: RemoveStation, Game: {gameInstance.gameId}");
    }

    public void RemoveAll()
    {
        // make sure only server actually handles this
        RPC_RemoveAll();
        MetroManager.SendEvent($"Action: RemoveLine, Game: {gameInstance.gameId}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RemoveAll()
    {
        for (int i = 0; i < stopCount; i++)
        {
            stops[i].lineCount = FusionUtils.Remove(stops[i].lines, stops[i].lineCount, this);
        }
        stops.Clear();
        stopCount = 0;
        tracks.needsUpdate = true;
        // foreach(var t in trains){
        //     Destroy(t.gameObject);
        // }
        for (int i = 0; i < trainCount; i++)
        {
            Runner.Despawn(trains[i].Object);
        }
        gameInstance.freeTrains += trainCount;
        trains.Clear();
        trainCount = 0;
        isDeployed = false;
        gameInstance.linesRemoved++;
    }

    //add train at a specific location
    public void AddTrain(float position, float direction)
    {
        // make sure only server actually handles this
        RPC_AddTrain(position, direction);
        MetroManager.SendEvent($"Action: AddTrain, Game: {gameInstance.gameId}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddTrain(float position, float direction)
    {
        if (gameInstance.freeTrains == 0) return;
        gameInstance.freeTrains -= 1;

        var prefab = Resources.Load("Prefabs/Train") as GameObject;

        var t = Runner.Spawn(prefab, onBeforeSpawned: (runner, obj) =>
        {
            obj.GetComponent<NetworkName>().syncedName = "Train";
            obj.GetComponent<Train>().gameInstance = gameInstance;
            obj.GetComponent<Train>().position = position;
            obj.GetComponent<Train>().direction = direction;
            obj.GetComponent<Train>().speed = 0.0f;
            obj.GetComponent<Train>().line = this;
            obj.GetComponent<Train>().uuid = obj.GetInstanceID();
            obj.GetComponent<Train>().color = color;
            obj.transform.SetParent(gameInstance.trainOrganizer.transform);
        }).GetComponent<Train>();

        // trains.Add(t);
        trainCount = FusionUtils.Add(trains, trainCount, t);
        gameInstance.trainsAdded++;
    }

    public void RemoveTrain()
    {
        // make sure only server actually handles this
        RPC_RemoveTrain();
        MetroManager.SendEvent($"Action: RemoveTrain, Game: {gameInstance.gameId}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RemoveTrain()
    {
        if (this.trainCount <= 0) return;
        trains[0].shouldRemove = true;
        gameInstance.trainsRemoved++;
    }

    public Station FindDestination(int from, int direction, StationType type)
    {
        var distance = 999;
        var index = -1;
        Station station = null;

        for (var i = 0; i < stopCount; i++)
        {
            var stop = stops[i];
            var d = (i - from);

            if (stop.type == type && System.Math.Abs(d) < distance && d * direction > 0)
            {
                station = stop;
                index = i;
                distance = d;
            }
        }
        return station;
    }

    public Station FindTransfer(int from, int direction, StationType type)
    {
        return null;
    }






}
