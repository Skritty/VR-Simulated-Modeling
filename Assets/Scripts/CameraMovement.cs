using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 1f;
    public float sensitivity = 10f;
    public float maxYAngle = 80f;
    private Vector2 currentRotation;

    void Update()
    {
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += transform.forward;
        if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
        if (Input.GetKey(KeyCode.D)) dir += transform.right;
        if (Input.GetKey(KeyCode.A)) dir -= transform.right;
         transform.position += dir.normalized * moveSpeed * Time.deltaTime;

        if (Input.GetMouseButtonDown(1))
            Cursor.lockState = CursorLockMode.Locked;
        if (Input.GetMouseButton(1))
        {
            currentRotation.x += Input.GetAxis("Mouse X") * sensitivity;
            currentRotation.y -= Input.GetAxis("Mouse Y") * sensitivity;
            currentRotation.x = Mathf.Repeat(currentRotation.x, 360);
            currentRotation.y = Mathf.Clamp(currentRotation.y, -maxYAngle, maxYAngle);
            Camera.main.transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);
        }
        if(Input.GetMouseButtonUp(1))
            Cursor.lockState = CursorLockMode.None;

    }
}
