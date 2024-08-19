using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Menu;
using UnityEngine;

namespace FusionDemo {
  public class IntroSampleCharacterSelectionUI : FusionMenuUIScreen {
    // List of avatars
    [SerializeField] private List<NetworkPrefabRef> _availableHostModeAvatars;
    [SerializeField] private List<NetworkPrefabRef> _availableSharedModeAvatars;
    [SerializeField] private Transform _avatarHolder;
    [SerializeField] private NetworkProjectConfigAsset _networkProjectConfig;

    private GameObject _currentAvatarModel;
    private Quaternion _prevAvatarRotation;
    private List<NetworkPrefabRef> _availableAvatars;

    // Current selected avatar
    private int _currentIndex;

    private NetworkProjectConfigAsset NetworkProjectConfig {
      get {
        if (_networkProjectConfig == null) {
          _networkProjectConfig = NetworkProjectConfigAsset.Global;
        }

        return _networkProjectConfig;
      }
      set => _networkProjectConfig = value;
    }

    public override void Show() {
      base.Show();
      // Set the available avatars as the host avatars just to render because we cant confirm the topology at this point.
      _availableAvatars = _availableHostModeAvatars;
      RenderAvatar();
    }

    public override void Hide() {
      base.Hide();
      if (_currentAvatarModel)
        Destroy(_currentAvatarModel);
    }

    public void NextAvatar() {
      _currentIndex = (_currentIndex + 1) % _availableAvatars.Count;
      RenderAvatar();
    }

    public void PreviousAvatar() {
      if (_currentIndex <= 0) {
        _currentIndex = _availableAvatars.Count - 1;
      } else {
        _currentIndex = (_currentIndex - 1) % _availableAvatars.Count;
      }

      RenderAvatar();
    }

    public NetworkPrefabId GetSelectedSkin(Topologies topology) {
      _availableAvatars = topology == Topologies.Shared ? _availableSharedModeAvatars : _availableHostModeAvatars;
      return NetworkProjectConfig.Config.PrefabTable.GetId((NetworkObjectGuid)_availableAvatars[_currentIndex]);
    }

    private void RenderAvatar() {
      if (_currentAvatarModel) {
        _prevAvatarRotation = _currentAvatarModel.transform.rotation;
        Destroy(_currentAvatarModel);
      }

      var prefabRef = _availableAvatars[_currentIndex];
      if (!prefabRef.IsValid) {
        throw new ArgumentException($"Not valid.", nameof(prefabRef));
      }

      var model = NetworkProjectConfig.Config.PrefabTable.Load(NetworkProjectConfig.Config.PrefabTable.GetId((NetworkObjectGuid)prefabRef), true);
      _currentAvatarModel = Instantiate(model.gameObject, _avatarHolder);
      _currentAvatarModel.AddComponent<RotateAvatar>();
      _currentAvatarModel.transform.rotation = _prevAvatarRotation;
    }
    
    /// <summary>
    /// Is called when the <see cref="_backButton"/> is pressed using SendMessage() from the UI object.
    /// </summary>
    public virtual void OnBackButtonPressed() {
      Controller.Show<FusionMenuUIMain>();
    }
  }
}