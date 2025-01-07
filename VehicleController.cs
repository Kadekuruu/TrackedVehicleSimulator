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
        Vector3 currentposition = transform.position; // Get the current position of the vehicle
        currentSpeedVector3 = (currentposition - previousPositionVector3) / Time.fixedDeltaTime; // Compare the current position of the vehicle to the last position, returns a Vector3
        previousPositionVector3 = transform.position; // Make the current position of the vehicle the last position, to compare in next FixedUpdate
        currentSpeedFloat = currentSpeedVector3.magnitude; // .magnitude returns the length of the vector, this gives us the speed
    }

    private void OnGUI()
    {
        float baseYPosition = Screen.height - 210;
        float labelSpacing = 30;
        float width = 300;
        float height = 25;

        GUI.skin.label.fontSize = 20;
        GUI.skin.label.normal.textColor = Color.black;

        float leftXPosition = 10;
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing * -1, width, height), $"Speed: {currentSpeedFloat / 10:F1} km/h");
    }
}