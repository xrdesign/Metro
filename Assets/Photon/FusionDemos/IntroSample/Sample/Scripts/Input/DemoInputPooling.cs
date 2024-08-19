using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace FusionDemo {
  public class DemoInputPooling : MonoBehaviour, INetworkRunnerCallbacks {
    // Pooling the input
    public void OnInput(NetworkRunner runner, NetworkInput input) {
      var myInput = new DemoNetworkInput();

      var horizontal = Input.GetAxisRaw("Horizontal");
      var vertical = Input.GetAxisRaw("Vertical");

      myInput.Buttons.Set(DemoNetworkInput.BUTTON_FORWARD, vertical > 0);
      myInput.Buttons.Set(DemoNetworkInput.BUTTON_BACKWARD, vertical < 0);
      myInput.Buttons.Set(DemoNetworkInput.BUTTON_RIGHT, horizontal > 0);
      myInput.Buttons.Set(DemoNetworkInput.BUTTON_LEFT, horizontal < 0);
      myInput.Buttons.Set(DemoNetworkInput.BUTTON_INTERACT, Input.GetKey(KeyCode.E));

      input.Set(myInput);
    }

    #region Other callbacks

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
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

    #endregion
  }
}