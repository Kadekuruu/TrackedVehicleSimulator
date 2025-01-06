using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleMovement : MonoBehaviour // Tracked Vehicle Movement inherits from TrackedVehicleController, as a child class
{
    // Track Rigidbody components
    [SerializeField] private Rigidbody leftTrackRigidbody;
    [SerializeField] private Rigidbody rightTrackRigidbody;

    // Gear system variables
    [SerializeField] private List<float> gearRatios = new List<float> { };
    public int maxGearIndex = 0;
    private int minGearIndex = 0;
    private int defaultGearIndex = 1;
    public int currentGearIndex = 1;
    public float currentGearRatio = 0.0f;
    public bool isInReverse = false;

    // Engine variables
    public bool isEngineOn = true;
    [SerializeField] private float enginePowerInKW = 2500.0f;
    public bool isAccelerating = false;

    // RPM variables
    private float idleEngineRPM = 500.0f;
    private float maxEngineRPM = 2500.0f;
    public float engineRPMInRadians = 0.0f;
    public float engineRPM = 0.0f;

    // Torque variables
    public float engineTorque = 0.0f;
    public float gearboxTorque = 0.0f;
    public float outputTorque = 0.0f;

    // Final drive ratio
    [SerializeField] private float finalDriveRatio = 3.5f;

    // Driving force variables
    private float drivingForce = 0.0f;
    [SerializeField] private float trackWidth = 0.65f;
    [SerializeField] private float trackLength = 9.0f;
    private float trackArea => trackWidth * trackLength;

    private void Awake()
    {
        Application.targetFrameRate = 120; // Limit frame rate to avoid excessive FixedUpdate calls
    }

    private void Start()
    {
        maxGearIndex = gearRatios.Count - (1 + defaultGearIndex);
        currentGearIndex = defaultGearIndex;
    }

    private void Update()
    {
        HandleEngineToggle();
        HandleGearShift();
    }

    private void FixedUpdate()
    {
        UpdateEngineRPM();

        if (isEngineOn)
        {
            CalculateEngineTorque();
            CalculateGearboxTorque();
            CalculateOutputTorque();
            CalculateDrivingForce();

            var (isLeftTrackActive, isRightTrackActive, isReversing) = GetTrackInputs();
            ApplyDrivingForceToTracks(isLeftTrackActive, isRightTrackActive, isReversing);
        }
    }

    private (bool isLeftTrackActive, bool isRightTrackActive, bool isReversing) GetTrackInputs()
    {
        bool isForward = Input.GetKey(KeyCode.W);
        bool isLeft = Input.GetKey(KeyCode.A);
        bool isBackward = Input.GetKey(KeyCode.S);
        bool isRight = Input.GetKey(KeyCode.D);

        bool isLeftTrackActive = (isForward && !isRight) || (isBackward && !isLeft) || isLeft;
        bool isRightTrackActive = (isForward && !isLeft) || (isBackward && !isRight) || isRight;
        bool isReversing = isBackward;

        isAccelerating = isLeftTrackActive || isRightTrackActive || isReversing;

        return (isLeftTrackActive, isRightTrackActive, isReversing);
    }

    private void ApplyDrivingForceToTracks(bool isLeftTrackActive, bool isRightTrackActive, bool isReversing)
    {
        float forceDirection = isReversing ? 1.0f : -1.0f;
        Vector3 drivingForceVector = new Vector3(0f, 0f, drivingForce * forceDirection);

        if (isLeftTrackActive)
        {
            leftTrackRigidbody.AddRelativeForce(drivingForceVector);
        }
        if (isRightTrackActive)
        {
            rightTrackRigidbody.AddRelativeForce(drivingForceVector);
        }
    }

    private void HandleEngineToggle()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isEngineOn = !isEngineOn;
        }
    }

    private void HandleGearShift()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentGearIndex < maxGearIndex)
            {
                currentGearIndex++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentGearIndex > minGearIndex)
            {
                currentGearIndex--;
            }
        }
        UpdateReverseStatus();
    }

    private void UpdateReverseStatus()
    {
        isInReverse = gearRatios[currentGearIndex] < 0;
    }

    private void UpdateEngineRPM()
    {
        if (isAccelerating)
        {
            engineRPM += 500f * Time.deltaTime;
            if (engineRPM > maxEngineRPM)
            {
                engineRPM = maxEngineRPM;
            }
        }
        else if (isEngineOn)
        {
            engineRPM -= 500f * Time.deltaTime;
            if (engineRPM < idleEngineRPM)
            {
                engineRPM = idleEngineRPM;
            }
        }
        else if (!isEngineOn && engineRPM > 0.0f)
        {
            engineRPM -= 500f * Time.deltaTime;
            if (engineRPM < 0.0f)
            {
                engineRPM = 0.0f;
            }
        }
    }

    private void CalculateEngineTorque()
    {
        engineTorque = enginePowerInKW / engineRPMInRadians;
    }

    private void CalculateGearboxTorque()
    {
        gearboxTorque = engineTorque * gearRatios[currentGearIndex];
    }

    private void CalculateOutputTorque()
    {
        outputTorque = gearboxTorque * finalDriveRatio;
    }

    private void CalculateDrivingForce()
    {
        drivingForce = outputTorque / trackArea;
    }

    private void OnGUI()
    {
        float xPosition = 10;
        float baseYPosition = Screen.height - 210;
        float labelSpacing = 30;
        float width = 300;
        float height = 25;

        GUI.skin.label.fontSize = 20;
        GUI.skin.label.normal.textColor = Color.black;

        GUI.Label(new Rect(xPosition, baseYPosition, width, height), $"Gear: {currentGearIndex:F1} {(isInReverse ? "Reverse" : "Forward")}");
        GUI.Label(new Rect(xPosition, baseYPosition + labelSpacing, width, height), $"Engine: {(isEngineOn ? "On" : "Off")}");
        GUI.Label(new Rect(xPosition, baseYPosition + labelSpacing * 2, width, height), $"RPM: {engineRPM:F1}");
        GUI.Label(new Rect(xPosition, baseYPosition + labelSpacing * 3, width, height), $"Engine Torque: {engineTorque:F1}");
        GUI.Label(new Rect(xPosition, baseYPosition + labelSpacing * 4, width, height), $"Gearbox Torque: {gearboxTorque:F1}");
        GUI.Label(new Rect(xPosition, baseYPosition + labelSpacing * 5, width, height), $"Output Torque: {outputTorque:F1}");
        GUI.Label(new Rect(xPosition, baseYPosition + labelSpacing * 6, width, height), $"Driving Force: {drivingForce:F1}");
    }
}