using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Menu;
using Fusion.Sockets;
using UnityEngine;

namespace FusionDemo {
  public class FusionPlayersService : NetworkBehaviour, IPlayerLeft, INetworkRunnerCallbacks {
    private const int PLAYERS_MAX_COUNT = 20; // this value needs to be bigger than the one on the config file.

    [Networked, Capacity(PLAYERS_MAX_COUNT)]
    private NetworkDictionary<PlayerRef, NetworkString<_16>> _playersUsernames => default;

    private ChangeDetector _changeDetector;
    private FusionMenuConnectionBehaviour _connection;
    private FusionMenuUIGameplay _fusionMenuUIGameplay;

    public List<string> GetPlayersUsernames()
    {
      var playersList = new List<string>();
      foreach (var pair in _playersUsernames)
      {
        playersList.Add(pair.Value.Value);
      }
      return playersList;
    }

    public override void Spawned()
    {
      Runner.AddCallbacks(this);
      
      var connectionBehaviour = FindFirstObjectByType<IntroSampleMenuConnectionBehaviour>(FindObjectsInactive.Include);
      if (connectionBehaviour == false) {
        Log.Error("Connection behaviour not found!");
        return;
      }

      _fusionMenuUIGameplay = FindFirstObjectByType<FusionMenuUIGameplay>(FindObjectsInactive.Include);
      if (_fusionMenuUIGameplay == false) {
        Log.Error("FusionMenuUIGameplay not found!");
        return;
      }
      
      _connection = connectionBehaviour;
      
      CheckMaxPlayerCount();

      _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

      RPC_AddPlayer(Runner.LocalPlayer, PlayerPrefs.GetString("Photon.Menu.Username"));
    }

    /// <summary>
    /// Check if the current config max players match the username dictionary capacity.
    /// </summary>
    /// <returns></returns>
    public void CheckMaxPlayerCount()
    {
      if (Runner.SessionInfo.MaxPlayers > PLAYERS_MAX_COUNT)
      {
        Debug.LogWarning($"Current gameplay overlay max clients capacity ({PLAYERS_MAX_COUNT}) is less than the session max players ({Runner.SessionInfo.MaxPlayers}). Consider increasing.");
      }
    }

    public override void Render() {
      foreach (var change in _changeDetector.DetectChanges(this)) {
        if (change == nameof(_playersUsernames)) {
          OnPlayersChange();
        }
      }
    }

    private void OnPlayersChange() {
      (_connection.Connection as IntroSampleMenuConnection)?.SetSessionUsernames(GetPlayersUsernames());
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_AddPlayer(PlayerRef player, string username)
    {
      _playersUsernames.Add(player, username);
    }

    private void RemovePlayer(PlayerRef player)
    {
      _playersUsernames.Remove(player);
    }

    public void PlayerLeft(PlayerRef player) {
      if (Object.HasStateAuthority) {
        RemovePlayer(player);
      }
    }

    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {
      // Host/Server left
      if (shutdownReason == ShutdownReason.DisconnectedByPluginLogic) {
        await _connection.DisconnectAsync(ConnectFailReason.Disconnect);
        _fusionMenuUIGameplay.Controller.Show<FusionMenuUIMain>();
      }
    }

    #region Unused Callbacks
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
  
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnConnectedToServer(NetworkRunner runner) { }


    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion Unused Callbacks
  }
}