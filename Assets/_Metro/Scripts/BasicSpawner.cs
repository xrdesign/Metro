using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections.Generic;
using Fusion.Sockets;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public GameObject userPrefab;
    public NetworkObject localPlayspace;
    private Dictionary<PlayerRef, NetworkObject> _spawnedUsers = new Dictionary<PlayerRef, NetworkObject>();

    private NetworkRunner _runner;
    async void StartGame(GameMode mode)
    {
        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/_Metro/Scenes/Metro.unity"));
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        // Disable the current scene's camera
        Camera.main.gameObject.SetActive(false);
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            ObjectProvider = new BakingObjectProvider()
        });

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // The user's prefab has to be spawned by the host
        if (runner.IsServer && userPrefab != null)
        {
            Debug.Log($"OnPlayerJoined. PlayerId: {player.PlayerId}");

            // We make sure to give the input authority to the connecting player for their user's object
            NetworkObject networkPlayerObject = runner.Spawn(userPrefab, position: transform.position, rotation: transform.rotation, inputAuthority: player, (runner, obj) =>
            {
            });

            // Keep track of the player avatars so we can remove it when they disconnect
            _spawnedUsers.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Find and remove the players avatar (only the host would have stored the spawned game object)
        if (_spawnedUsers.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedUsers.Remove(player);
        }
    }

    private void OnGUI()
    {
        if (_runner == null){
            // Create the Fusion runner and let it know that we will be providing user input
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        // If not startGame, show the host and join buttons
        if (_runner.State != NetworkRunner.States.Running)
        {

            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }

        // show the list of current user in the room bottom left
        var players = _runner.ActivePlayers;
        var playerCount = 0;
        foreach (PlayerRef player in players)
        {
            GUI.Label(new Rect(0, 80 + 20 * playerCount, 200, 20), $"Player: {player.PlayerId}");
            playerCount++;
        }

        // show the ip and port of the server and self
        playerCount+=1;
        var lobbyName = _runner.LobbyInfo.Name;
        var sessionName = _runner.SessionInfo.Name;
        var sessionPlayerCount = _runner.SessionInfo.PlayerCount;
        GUI.Label(new Rect(0, 80 + 20 * playerCount, 200, 20), $"Lobby: {lobbyName}");
        GUI.Label(new Rect(0, 100 + 20 * playerCount, 200, 20), $"Session: {sessionName} ({sessionPlayerCount} players)");
    }

}
