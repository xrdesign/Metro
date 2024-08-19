using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FusionDemo {
  public class MobileInput : MonoBehaviour {
    public Vector2 JoystickDirection { get; private set; }

    [SerializeField] private RectTransform _joystickHandle;
    [SerializeField] private float _radius;
    [SerializeField] private LayerMask UILayer;

    private Vector2 _handlePosition;
    private Vector2 _touchHoldPosition;
    private bool _interactPressed;
    private bool _touchStartedOverUI;

    void Start() {
#if UNITY_IOS || UNITY_ANDROID
      _handlePosition = _joystickHandle.position;
#else
      gameObject.SetActive(false);
#endif
    }

    void Update() {
      JoystickDirection = Vector2.zero;
      if (Input.touchCount > 0) {
        var touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began) {
          if (IsPointerOverUIElement(touch.position)) {
            _joystickHandle.gameObject.SetActive(false);
            _touchStartedOverUI = true;
            return;
          }

          _touchStartedOverUI = false;
          _touchHoldPosition = touch.position;
          _handlePosition = _touchHoldPosition;
          _joystickHandle.gameObject.SetActive(true);
        } else if (touch.phase == TouchPhase.Ended) {
          _joystickHandle.gameObject.SetActive(false);
          return;
        }

        if (_touchStartedOverUI) return;

        JoystickDirection = touch.position - _touchHoldPosition;
        joystickMove(JoystickDirection);
      }
    }

    void joystickMove(Vector2 direction) {
      if (direction.magnitude > _radius) {
        direction = direction.normalized * _radius;
      }

      _joystickHandle.position = new Vector2(_handlePosition.x + direction.x, _handlePosition.y + direction.y);
    }

    public bool ConsumeInteractInput() {
      if (_interactPressed) {
        _interactPressed = false;
        return true;
      }

      return false;
    }

    public void SetInteractPressedTrue() {
      _interactPressed = true;
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverUIElement(Vector2 pos) {
      return IsPointerOverUIElement(GetEventSystemRaycastResults(pos));
    }


    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults) {
      for (int index = 0; index < eventSystemRaysastResults.Count; index++) {
        RaycastResult curRaysastResult = eventSystemRaysastResults[index];
        Debug.Log(curRaysastResult.gameObject);
        if (1 << curRaysastResult.gameObject.layer == UILayer)
          return true;
      }

      return false;
    }


    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults(Vector2 pos) {
      PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = pos;
      List<RaycastResult> raycastResults = new List<RaycastResult>();
      EventSystem.current.RaycastAll(eventData, raycastResults);
      return raycastResults;
    }
  }
}