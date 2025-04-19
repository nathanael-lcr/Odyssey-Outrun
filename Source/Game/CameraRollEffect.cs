using System;
using FlaxEngine;

/// <summary>
/// Adds roll effect to camera based on vehicle steering input and speed.
/// Works alongside existing camera systems by applying roll rotation.
/// </summary>
public class CameraRollEffect : Script
{
    [Header("References")]
    public Camera TargetCamera;
    public WheeledVehicle Vehicle;
    public Actor CameraTarget; // Reference to the same target your camera controller uses
    
    [Header("Roll Settings")]
    [Tooltip("Maximum roll angle in degrees when turning at high speed")]
    public float MaxRollAngle = 10.0f;
    [Tooltip("Minimum speed required before applying roll effect (km/h)")]
    public float MinRollSpeed = 40.0f;
    [Tooltip("Speed at which roll effect reaches maximum (km/h)")]
    public float MaxRollSpeed = 120.0f;
    [Tooltip("Roll effect interpolation speed")]
    public float RollLerpSpeed = 2.5f;
    
    [Header("Input Settings")]
    [Tooltip("Input axis name used for steering (typically 'Horizontal')")]
    public string SteeringInputAxis = "Horizontal";
    [Tooltip("Whether to smoothly interpolate the input value")]
    public bool SmoothInput = true;
    [Tooltip("Speed of input smoothing")]
    public float InputSmoothingSpeed = 3.0f;
    
    private float _currentRoll = 0.0f;
    private float _targetRoll = 0.0f;
    private float _smoothedSteeringInput = 0.0f;
    
    public override void OnStart()
    {
        if (Vehicle == null)
        {
            Debug.LogError("Vehicle reference is not set in CameraRollEffect.");
            Enabled = false;
            return;
        }
        
        if (CameraTarget == null)
        {
            // Try to get the camera target from parent
            CameraTarget = TargetCamera.Parent;
            if (CameraTarget == null)
            {
                Debug.LogError("Camera target reference is not set in CameraRollEffect.");
                Enabled = false;
                return;
            }
        }
    }
    
    // Override LateUpdate to apply roll after all other camera calculations are complete
    public override void OnLateUpdate()
    {
        float currentSpeed = GetVehicleSpeed();
        UpdateRollEffect(currentSpeed);
        ApplyRollToCamera();
    }
    
    private void UpdateRollEffect(float speed)
    {
        // Get current steering input
        float steeringInput = Input.GetAxis(SteeringInputAxis);
        
        // Apply smoothing if enabled
        if (SmoothInput)
        {
            float smoothFactor = Mathf.Clamp(InputSmoothingSpeed * Time.DeltaTime, 0f, 1f);
            _smoothedSteeringInput = Mathf.Lerp(_smoothedSteeringInput, steeringInput, smoothFactor);
        }
        else
        {
            _smoothedSteeringInput = steeringInput;
        }
        
        // Calculate speed factor
        float speedFactor = CalculateSpeedFactor(speed);
        
        // Set target roll (negative for outward roll)
        // The negative sign makes the camera roll to the outside of the turn
        _targetRoll = -_smoothedSteeringInput * speedFactor * MaxRollAngle;
        
        // Interpolate towards target roll
        float rollLerpFactor = Mathf.Clamp(RollLerpSpeed * Time.DeltaTime, 0f, 1f);
        _currentRoll = Mathf.Lerp(_currentRoll, _targetRoll, rollLerpFactor);
    }
    
    private void ApplyRollToCamera()
    {
        // Get current rotation of the target (your existing camera system already sets this)
        Vector3 currentEuler = CameraTarget.LocalOrientation.EulerAngles;
        
        // Create a new rotation that maintains pitch and yaw but adds our roll
        Quaternion newRotation = Quaternion.Euler(currentEuler.X, currentEuler.Y, _currentRoll);
        
        // Apply the new rotation
        CameraTarget.LocalOrientation = newRotation;
    }
    
    private float CalculateSpeedFactor(float speed)
    {
        // No roll effect below minimum speed
        if (speed < MinRollSpeed)
            return 0;
            
        // Smooth interpolation between min and max speeds
        return Mathf.Clamp((speed - MinRollSpeed) / (MaxRollSpeed - MinRollSpeed), 0f, 1f);
    }
    
    private float GetVehicleSpeed()
    {
        if (Vehicle == null) return 0.0f;
        try
        {
            return Mathf.Abs(Vehicle.ForwardSpeed * 3.6f) / 100; // Convert to km/h
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting vehicle speed: {ex.Message}");
            return 0.0f;
        }
    }
}