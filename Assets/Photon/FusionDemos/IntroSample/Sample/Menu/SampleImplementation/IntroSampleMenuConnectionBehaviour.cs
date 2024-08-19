using System;
using Fusion;
using Fusion.Menu;
using UnityEngine;

namespace FusionDemo {
  public class IntroSampleMenuConnectionBehaviour : FusionMenuConnectionBehaviour {
    [SerializeField] private FusionMenuConfig _config;
    [SerializeField] private Camera _menuCamera;
    [SerializeField] private NetworkPrefabRef _playerListService;

    private void Awake() {
      if (!_menuCamera) {
        _menuCamera = Camera.current;
      }

      if (!_config) {
        Log.Error("Fusion menu configuration file not provided.");
      }

      OnBeforeConnect += DisableMenuCamera;
      OnBeforeDisconnect += EnableMenuCamera;
    }

    public override IFusionMenuConnection Create() {
      return new IntroSampleMenuConnection(_config, SpawnPlayerListService);
    }

    private void ToggleMenuCamera(bool value) {
      _menuCamera.gameObject.SetActive(value);
    }

    private void DisableMenuCamera(IFusionMenuConnectArgs args) {
      ToggleMenuCamera(false);
    }
    
    private void EnableMenuCamera(IFusionMenuConnection fusionMenuConnection, int error) {
      ToggleMenuCamera(true);
    }

    private void SpawnPlayerListService(NetworkRunner runner) {
      if (runner.IsServer || runner.IsSharedModeMasterClient) {
        runner.SpawnAsync(_playerListService);
      }
    }
  }
}
