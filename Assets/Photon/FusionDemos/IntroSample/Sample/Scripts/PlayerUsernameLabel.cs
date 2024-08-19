using Fusion;
using TMPro;
using UnityEngine;

namespace FusionDemo {
  /// <summary>
  /// Class responsible for displaying the player username on top of the avatar.
  /// </summary>
  public class PlayerUsernameLabel : NetworkBehaviour {
    // Networked string to store the player username.
    [Networked] private NetworkString<_32> _username { get; set; }

    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private PlayerColor _playerColor;

    /// <summary>
    /// Set the player username.
    /// </summary>
    public void SetUsernameLabel(string username) {
      _username = username;
    }

    private void OnEnable() {
      _playerColor.OnColorChanged += ReactToColorChange;
    }

    private void OnDisable() {
      _playerColor.OnColorChanged -= ReactToColorChange;
    }

    public override void Spawned() {
      _usernameText.SetText(_username.ToString());
    }

    // Set the username text based on the player active color value.
    private void ReactToColorChange(Color color) {
      _usernameText.color = color;
    }
  }
}