using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Menu;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionDemo {
  public class IntroSampleMenuConnection : IFusionMenuConnection {
    
    public IntroSampleMenuConnection(IFusionMenuConfig config, Action<NetworkRunner> onGameStartCallback) {
      _config = config;
      _onGameStartCallback = onGameStartCallback;
    }

    public string SessionName { get; private set; }
    public int MaxPlayerCount { get; private set; }
    public string Region { get; private set; }
    public string AppVersion { get; private set; }
    public List<string> Usernames { get; private set; }
    public bool IsConnected => _runner && _runner.IsRunning;
    public int Ping => (int)(IsConnected ? _runner.GetPlayerRtt(_runner.LocalPlayer) * 1000 : 0);

    private NetworkRunner _runner;
    private IFusionMenuConfig _config;
    private Action<NetworkRunner> _onGameStartCallback;
    private bool _connectingSafeCheck;
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    
    public async Task<ConnectResult> ConnectAsync(IFusionMenuConnectArgs connectArgs) {
      // Safety
      if (_connectingSafeCheck) return new ConnectResult(){CustomResultHandling = true, Success = false, FailReason = ConnectFailReason.None};
      
      _connectingSafeCheck = true;
      if (_runner && _runner.IsRunning) {
        await _runner.Shutdown();
      }
      
      // Create and prepare Runner object
      _runner = CreateRunner();
      var sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
      sceneManager.IsSceneTakeOverEnabled = false;

      // Copy and update AppSettings
      var appSettings = CopyAppSettings(connectArgs);
      
      // Solve StartGameArgs
      var args = new StartGameArgs();
      args.OnGameStarted = _onGameStartCallback;
      args.CustomPhotonAppSettings = appSettings;
      args.GameMode = ResolveGameMode(connectArgs);
      args.SessionName = SessionName = connectArgs.Session;
      args.PlayerCount = MaxPlayerCount = connectArgs.MaxPlayerCount;
      
      // Scene info
      var sceneInfo = new NetworkSceneInfo();
      sceneInfo.AddSceneRef(sceneManager.GetSceneRef(connectArgs.Scene.ScenePath), LoadSceneMode.Additive);
      args.Scene = sceneInfo;
      
      // Cancellation Token
      _cancellationTokenSource?.Dispose();
      _cancellationTokenSource = new CancellationTokenSource();
      _cancellationToken = _cancellationTokenSource.Token;
      args.StartGameCancellationToken = _cancellationToken;
      
      var regionIndex = _config.AvailableRegions.IndexOf(connectArgs.Region);
      args.SessionNameGenerator = () => _config.CodeGenerator.EncodeRegion(_config.CodeGenerator.Create(), regionIndex);
      var startGameResult = default(StartGameResult);
      var connectResult = new ConnectResult();
      startGameResult = await _runner.StartGame(args);

      connectResult.Success = startGameResult.Ok;
      connectResult.FailReason = ResolveConnectFailReason(startGameResult.ShutdownReason);
      _connectingSafeCheck = false;

      if (connectResult.Success) {
        SessionName = _runner.SessionInfo.Name;
      }
      
      return connectResult;
    }

    public async Task DisconnectAsync(int reason) {
      var peerMode = _runner.Config?.PeerMode;
      _cancellationTokenSource.Cancel();
      await _runner.Shutdown(shutdownReason: ResolveShutdownReason(reason));

      if (peerMode is NetworkProjectConfig.PeerModes.Multiple) return;
      
      for (int i = SceneManager.sceneCount-1; i > 0; i--) {
        SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
      }
    }

    public Task<List<FusionMenuOnlineRegion>> RequestAvailableOnlineRegionsAsync(IFusionMenuConnectArgs connectArgs) {
      // Force best region
      return Task.FromResult(new List<FusionMenuOnlineRegion>(){new FusionMenuOnlineRegion(){Code = string.Empty, Ping = 0}});
    }

    public void SetSessionUsernames(List<string> usernames) {
      Usernames = usernames;
    }

    private GameMode ResolveGameMode(IFusionMenuConnectArgs args) {
      bool isSharedSession = args.Scene.SceneName.Contains("Shared");
      if (args.Creating) {
        // Create session
        return isSharedSession ? GameMode.Shared : GameMode.Host;
      }

      if (string.IsNullOrEmpty(args.Session)) {
        // QuickJoin
        return isSharedSession ? GameMode.Shared : GameMode.AutoHostOrClient;
      }

      // Join session
      return isSharedSession ? GameMode.Shared : GameMode.Client;
    }

    private ShutdownReason ResolveShutdownReason(int reason) {
      switch (reason) {
        case ConnectFailReason.UserRequest:
          return ShutdownReason.Ok;
        case ConnectFailReason.ApplicationQuit:
          return ShutdownReason.Ok;
        case ConnectFailReason.Disconnect:
          return ShutdownReason.DisconnectedByPluginLogic;
        default:
          return ShutdownReason.Error;
      }
    }
    
    private int ResolveConnectFailReason(ShutdownReason reason) {
      switch (reason) {
        case ShutdownReason.Ok:
        case ShutdownReason.OperationCanceled:
          return ConnectFailReason.UserRequest;
        case ShutdownReason.DisconnectedByPluginLogic:
        case ShutdownReason.Error:
          return ConnectFailReason.Disconnect;
        default:
          return ConnectFailReason.None;
      }
    }

    private NetworkRunner CreateRunner() {
      var runner = new GameObject("NetworkRunner", typeof(NetworkRunner)).GetComponent<NetworkRunner>();
      runner.gameObject.AddComponent<DemoInputPooling>();
      // runner.gameObject.AddComponent<RunnerSimulatePhysics3D>();
      return runner;
    }

    private FusionAppSettings CopyAppSettings(IFusionMenuConnectArgs connectArgs) {
      FusionAppSettings appSettings = new FusionAppSettings();
      PhotonAppSettings.Global.AppSettings.CopyTo(appSettings);
      appSettings.FixedRegion = Region = connectArgs.Region;
      appSettings.AppVersion = AppVersion = connectArgs.AppVersion;
      return appSettings;
    }
  }
}