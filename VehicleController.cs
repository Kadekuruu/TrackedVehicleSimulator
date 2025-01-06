using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// controller script for the tracked vehicle physics simulator
public class VehicleController : MonoBehaviour
{
    public float currentSpeedFloat = 0f;
    public Vector3 currentSpeedVector3;
    public Vector3 previousPositionVector3;

    private void Start()
    {
        previousPositionVector3 = transform.position;
    }

    private void FixedUpdate() // Using FixedUpdate since it runs independently of framerate, giving us a accurate calculation
    {
        MeasureSpeed();
    }

    private void MeasureSpeed()
    {
        // Speed = Distance / Time
        // Get the current position of the vehicle
        Vector3 currentposition = transform.position;
        // Compare the current position of the vehicle to the last position, returns a Vector3
        currentSpeedVector3 = (currentposition - previousPositionVector3) / Time.fixedDeltaTime;
        // Make the current position of the vehicle the last position, to compare in next FixedUpdate
        previousPositionVector3 = transform.position;
        // .magnitude returns the length of the vector, this gives us the speed
        currentSpeedFloat = currentSpeedVector3.magnitude;
    }

    private void OnGUI()
    {
        float xPosition = Screen.width -10;
        float baseYPosition = Screen.height - 210;
        float width = 300;
        float height = 25;

        GUI.skin.label.fontSize = 20;
        GUI.skin.label.normal.textColor = Color.black;

        GUI.Label(new Rect(xPosition, baseYPosition, width, height), $"Speed: {currentSpeedFloat:F1}");
    }
}