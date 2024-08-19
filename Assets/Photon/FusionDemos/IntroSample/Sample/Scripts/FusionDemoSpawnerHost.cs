using System;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FusionDemo {
  /// <summary>
  /// Class responsible to spawn the player avatar with the requested skin for each player.
  /// </summary>
  public class FusionDemoSpawnerHost : NetworkBehaviour, IPlayerLeft {
    [Header("Base avatar used if the selected skin avatar from the menu was not found.")] 
    [SerializeField] private NetworkPrefabRef _baseAvatar;

    [SerializeField] private Transform _spawnPoint;
    [Networked, Capacity(4)] private NetworkDictionary<PlayerRef, NetworkObject> _playersAvatar => default;

    public override void Spawned() {
      // Request server to spawn the selected skin
      var playerSettingsView = FindFirstObjectByType<IntroSampleCharacterSelectionUI>(FindObjectsInactive.Include);

      // If the PlayerSettingsView was found, use it to send the RPC with the selected avatar skin and username defined on the menu.
      // Else: Send the RPC with the default base avatar and the local player ref as the player username.
      if (playerSettingsView) {
        RPC_RequestAvatarSpawn(playerSettingsView.GetSelectedSkin(Runner.Topology), Runner.LocalPlayer, playerSettingsView.ConnectionArgs.Username);
      } else {
        RPC_RequestAvatarSpawn(default, Runner.LocalPlayer, Runner.LocalPlayer.ToString());
      }
    }

    // Spawns the avatar for the player with the specified skin and username.
    private void SpawnPlayerPrefab(NetworkPrefabId avatarSkin, PlayerRef inputAuth, string username) {
      NetworkObject avatar;

      var randomPos = Random.onUnitSphere * 2;
      randomPos.y = 0;
      var pos = _spawnPoint.position + randomPos;
      
      // Check if the avatar skin is not the default one
      if (avatarSkin != default) {
        // Spawn the avatar with the specified skin, position, and username
        avatar = Runner.Spawn(avatarSkin, inputAuthority: inputAuth, position: pos, onBeforeSpawned: OnBeforeSpawned);
      } else {
        // Log a warning and spawn the base avatar if the specified skin was not found
        Debug.LogWarning("Skin not found. Using base avatar");
        avatar = Runner.Spawn(_baseAvatar, inputAuthority: inputAuth, position: pos, onBeforeSpawned: OnBeforeSpawned);
      }

      // Register the spawned avatar for the player
      RegisterPlayerAvatar(inputAuth, avatar);
      return;

      void OnBeforeSpawned(NetworkRunner runner, NetworkObject o) {
        o.GetBehaviour<PlayerUsernameLabel>().SetUsernameLabel(username);
      }
    }

    // Registers the avatar associated with the given player.
    private void RegisterPlayerAvatar(PlayerRef player, NetworkObject avatar) {
      // Check if the dictionary has reached its capacity
      if (_playersAvatar.Count >= _playersAvatar.Capacity) {
        // Log an error message and throw an exception if the dictionary is full
        Debug.LogError($"Avatar for player {player} not being registered because the dictionary is full. Increase the dictionary capacity to match the max player count.");
        throw new InvalidOperationException("Avatar dictionary is full.");
      }

      // Add the player and their avatar to the dictionary
      _playersAvatar.Add(player, avatar);

      // register the player object
      Runner.SetPlayerObject(player, avatar);
    }

    // Tries to get the avatar associated with the given player.
    private bool GetPlayerAvatar(PlayerRef player, out NetworkObject avatar) {
      // Try to get the avatar from the dictionary using the player reference as the key
      if (_playersAvatar.TryGet(player, out avatar)) {
        return true; // Avatar found and retrieved successfully
      }

      return false; // Avatar not found
    }

    // Handles the event when a player leaves the game.
    public void PlayerLeft(PlayerRef player) {
      // Log the event of the player leaving
      Debug.Log($"Player {player} left");

      // Only run on state authority
      // This ensures that the following code is only executed on the server
      if (Object.HasStateAuthority == false) return;

      // Get the avatar of the player who left and despawn it if valid
      // This is to clean up any resources associated with the player
      if (GetPlayerAvatar(player, out var avatar)) {
        _playersAvatar.Remove(player);
        Runner.Despawn(avatar);
      }
    }

    // Sends a request to the state authority to spawn an avatar for the joining player.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAvatarSpawn(NetworkPrefabId avatarSkin, PlayerRef inputAuthority, string username) {
      // Call the SpawnPlayerPrefab method to spawn the avatar
      SpawnPlayerPrefab(avatarSkin, inputAuthority, username);
    }
  }
}