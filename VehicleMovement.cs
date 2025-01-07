using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//Movement script for TrackedVehicleSimulator
public class VehicleMovement : MonoBehaviour
{
    // Track Rigidbody components
    [SerializeField] private Rigidbody leftTrackRigidbody;
    [SerializeField] private Rigidbody rightTrackRigidbody;

    // Gear system variables
    [SerializeField] private List<float> gearRatios = new List<float> { }; // Create a new list that will be populated in the Unity scene
    public int maxGearIndex = 0;
    private int minGearIndex = 0;
    public int defaultGearIndex = 1;
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
        Application.targetFrameRate = 120; // Limit framerate to avoid excessive Update calls
    }

    // Start is called before first frame update
    private void Start()
    {
        maxGearIndex = gearRatios.Count - (1 + defaultGearIndex);
        currentGearIndex = defaultGearIndex;
    }

    // Update is called once per frame
    private void Update()
    {
        HandleEngineToggle(); // A bug was encountered where excessive Update calls meant this was called twice, resolved by capping framerate
        HandleGearShift();
    }

    private void FixedUpdate()
    {
        // These must be called in order, since they depend on eachother
        UpdateEngineRPM();
        CalculateRadianRPM();

        if (isEngineOn) // Only runs if engine is on
        {
            // Calculate the four calculations needed
            CombinedCalculations();

            var (isLeftTrackActive, isRightTrackActive, isReversing) = GetTrackInputs(); // Take apart the tuple returned by GetTrackInputs()
            ApplyDrivingForceToTracks(isLeftTrackActive, isRightTrackActive, isReversing); // And use the tuple to call ApplyDrivingForceToTracks()
        }
    }

    // This function gets key inputs and uses logic sequences to determine which tracks are active, and if the vehicle is in reverse
    private (bool isLeftTrackActive, bool isRightTrackActive, bool isReversing) GetTrackInputs()
    {
        bool isForward = Input.GetKey(KeyCode.W); // Input.GetKey instead of GetKeyDown, since it checks if the key is being held instead of pressed
        bool isLeft = Input.GetKey(KeyCode.A);
        bool isBackward = Input.GetKey(KeyCode.S);
        bool isRight = Input.GetKey(KeyCode.D);

        // Logic sequence used to determine active tracks
        bool isLeftTrackActive = (isForward && !isRight) || (isBackward && !isLeft) || isLeft;
        bool isRightTrackActive = (isForward && !isLeft) || (isBackward && !isRight) || isRight;
        bool isReversing = isBackward;

        isAccelerating = isLeftTrackActive || isRightTrackActive || isReversing; // Logic sequence used to determine if vehicle is accelerating


        return (isLeftTrackActive, isRightTrackActive, isReversing);  // Return a tuple since only one thing can be returned from a function
    }

    // This function takes the previous outputs and applys the force accordingly
    private void ApplyDrivingForceToTracks(bool isLeftTrackActive, bool isRightTrackActive, bool isReversing)
    {
        float forceDirection = isReversing ? 1.0f : -1.0f; // Depending on if isReversing is True or False, apply positive or negative force to simulate reversing
        Vector3 drivingForceVector = new Vector3(0f, 0f, drivingForce * forceDirection); // Create a new Vector3 on Z axis to simulate force applied by tracks in that direction

        if (isLeftTrackActive)
        {
            leftTrackRigidbody.AddRelativeForce(drivingForceVector); // Add relative force, since it applies locally to the Rigidbody instead of globally
        }
        if (isRightTrackActive)
        {
            rightTrackRigidbody.AddRelativeForce(drivingForceVector);
        }
    }

    // Function that toggles engine when keydown is detected, not held
    private void HandleEngineToggle()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isEngineOn = !isEngineOn;
        }
    }

    // Function that increments or decreases the index of the gear when keydown
    private void HandleGearShift()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentGearIndex < maxGearIndex) // Ensure index doesn't go above maximum assigned index
            {
                currentGearIndex++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentGearIndex > minGearIndex) // Ensure index doesn't go below minimum assigned index
            {
                currentGearIndex--;
            }
        }
        UpdateReverseStatus();
    }

    // Determine if the current gear is reverse or not
    private void UpdateReverseStatus()
    {
        if (gearRatios[currentGearIndex] < defaultGearIndex) // If the current gear is below the default gear, it has to be in reverse gear
        {
            isInReverse = true;
        }
        else if (gearRatios[currentGearIndex] >= currentGearIndex)
        {
            isInReverse = false;
        }
    }

    // Simple implementation to simulate the increase and decreasing of engine RPM, can be modified to be more advanced in the future
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
        else if (!isEngineOn && engineRPM > 0.0f) // Decrease RPM to 0 if engine is off
        {
            engineRPM -= 500f * Time.deltaTime;
            if (engineRPM < 0.0f)
            {
                engineRPM = 0.0f;
            }
        }
    }

    private void CombinedCalculations()
    {
        engineTorque = enginePowerInKW / engineRPMInRadians; // Calculate the engine torque
        gearboxTorque = engineTorque * gearRatios[currentGearIndex] * Mathf.Clamp(engineRPM / maxEngineRPM, 0.1f, 1.0f); // Calculate the torque at gearbox, Mathf.Clamp limits the lower multiplier to 0.1 and upper to 1.0 to prevent unrealistic results
        outputTorque = gearboxTorque * finalDriveRatio; // Calculate the output torque at the final drive
        drivingForce = outputTorque / trackArea; // Calculate the final driving force
    }

    // These really don't need to be separate functions, combined into the one above
    //private void CalculateEngineTorque()
    //{
    //    engineTorque = enginePowerInKW / engineRPMInRadians;
    //}

    //private void CalculateGearboxTorque()
    //{
    //    gearboxTorque = engineTorque * gearRatios[currentGearIndex] * Mathf.Clamp(engineRPM / maxEngineRPM, 0.1f, 1.0f);
    //}

    //private void CalculateOutputTorque()
    //{
    //    outputTorque = gearboxTorque * finalDriveRatio;
    //}

    //private void CalculateDrivingForce()
    //{
    //    drivingForce = outputTorque / trackArea;
    //}

    // Calculate the engine RPM in radians
    public float CalculateRadianRPM()
    {
        engineRPMInRadians = engineRPM * (float)Math.PI / 60;
        return engineRPMInRadians;
    }

    // Development GUI since we don't need anything more advanced currently
    private void OnGUI()
    {
        float baseYPosition = Screen.height - 210;
        float labelSpacing = 30;
        float width = 300;
        float height = 25;

        GUI.skin.label.fontSize = 20;
        GUI.skin.label.normal.textColor = Color.black;

        float leftXPosition = 10;
        GUI.Label(new Rect(leftXPosition, baseYPosition, width, height), $"Gear: {currentGearIndex:F1} {(isInReverse ? "Reverse" : "Forward")}");
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing, width, height), $"Engine: {(isEngineOn ? "On" : "Off")}");
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing * 2, width, height), $"RPM: {engineRPM:F1}");
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing * 3, width, height), $"Engine Torque: {engineTorque:F1}");
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing * 4, width, height), $"Gearbox Torque: {gearboxTorque:F1}");
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing * 5, width, height), $"Output Torque: {outputTorque:F1}");
        GUI.Label(new Rect(leftXPosition, baseYPosition + labelSpacing * 6, width, height), $"Driving Force: {drivingForce:F1}");
    }
}