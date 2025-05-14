using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardMove : MonoBehaviour
{
  [SerializeField]
  float maxSpeed;

  [SerializeField]
  float accel;

  Vector3 velocity = Vector3.zero;

  // Start is called before the first frame update
  void Start() { }

  // Update is called once per frame
  void Update()
  {
    Vector3 dir = Vector3.zero;
    if (Input.GetKey(KeyCode.A))
    {
      dir.x = -1;
    }
    if (Input.GetKey(KeyCode.D))
    {
      dir.x = 1;
    }
    if (Input.GetKey(KeyCode.S))
    {
      dir.z = -1;
    }
    if (Input.GetKey(KeyCode.W))
    {
      dir.z = 1;
    }
    if (Input.GetKey(KeyCode.E))
    {
      dir.y = 1;
    }
    if (Input.GetKey(KeyCode.Q))
    {
      dir.y = -1;
    }

    var t = Camera.main.transform;
    Vector3 up = Vector3.up;
    Vector3 right = t.right;
    Vector3 forward = Vector3.Cross(right, up);
    Vector3 finalDir =
        Vector3.Normalize((right * dir.x) + (up * dir.y) + (forward * dir.z));
    velocity =
        Vector3.Lerp(velocity, finalDir * maxSpeed, accel * Time.deltaTime);
    t.position += velocity * Time.deltaTime;
  }
}
