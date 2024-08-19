using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionDemo {
  public class PlayerColorSphere : MonoBehaviour {
    [SerializeField] private PlayerColor _playerColor;
    [SerializeField] private MeshRenderer _meshRenderer;

    private void OnEnable() {
      _playerColor.OnColorChanged += ReactToColorChange;
    }

    private void OnDisable() {
      _playerColor.OnColorChanged -= ReactToColorChange;
    }

    private void ReactToColorChange(Color color) {
      _meshRenderer.material.color = color;
    }
  }
}