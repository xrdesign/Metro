using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadHandFollower : MonoBehaviour
{
    public GameObject followee;
    public Vector3 headOffset = new Vector3(0, 0.2f, 1.0f); // Offset from the head position
    public Vector3 handOffset = new Vector3(0, 0.3f, 0.1f); // Offset from the hand position
    public float followSpeed = 3.0f; // Speed at which the followee follows the head or hand
    public string handName = "Right_ShellHandRayPointer(Clone)"; // Name of the hand to follow
    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp
    void Start()
    {
    }

    void LateUpdate()
    {

        Vector3 goalPos = Vector3.zero;
        Quaternion goalRot = Quaternion.identity;
        // check if the MRTK3 right controller game object is available
        GameObject rightController = GameObject.Find(handName);
        if (rightController != null)
        {
            // if the right controller is available, use the right hand.
            goalPos = rightController.transform.position + rightController.transform.rotation * handOffset;
            goalRot = rightController.transform.rotation;
        }
        else
        {
            // if the right controller is not available, use the head position
            // main camera is used to get the head position
            GameObject mainCamera = Camera.main.gameObject;
            if (mainCamera != null)
            {
                goalPos = mainCamera.transform.position + mainCamera.transform.rotation * headOffset;
                goalRot = mainCamera.transform.rotation;
            }

        }

        followee.transform.position = Vector3.SmoothDamp(
            followee.transform.position,
            goalPos,
            ref velocity,
            0.1f           // smoothTime, tweak to taste
        );

        // 4) Smooth rotation
        followee.transform.rotation = Quaternion.Slerp(
            followee.transform.rotation,
            goalRot,
            followSpeed * Time.deltaTime
        );
    }
}
