using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float mouseSensitivity = 100f;

    float xRotation = 0f;
    float yRotation = 0f;

    public float topClamp = -90f;
    public float bottomClamp = 90f;

    void Start()
    {
        // locking the cursorto the middle of the screen and making it invisable
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // getting the mouse inputs
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // rotation around the x axis (looking up and down)
        xRotation -= mouseY;

        // clamp the rotation
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);

        // rotation around the x axis (looking left and right)
        yRotation += mouseX;

        // apply rotation to our transform
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

    }
}
