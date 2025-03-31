using System;
using FlaxEngine;

/// <summary>
/// Camera FOV controller that dynamically adjusts based on vehicle speed.
/// </summary>
public class CameraFOVController : Script
{
    [Header("FOV Settings")]
    [Tooltip("The base/default field of view in degrees")]
    public float DefaultFOV = 60.0f;
    
    [Tooltip("The speed threshold above which FOV starts to increase")]
    public float SpeedThreshold = 80.0f;
    
    [Tooltip("Maximum possible FOV in degrees")]
    public float MaxFOV = 90.0f;
    
    [Tooltip("How much to increase FOV per speed unit above the threshold")]
    public float FOVIncreaseRate = 0.2f;
    
    [Header("Lerping Settings")]
    [Tooltip("How quickly FOV transitions (higher = faster)")]
    public float LerpSpeed = 3.0f;
    
    [Header("References")]
    [Tooltip("Reference to the camera (if null, will try to find on parent)")]
    public Camera TargetCamera;
    
    [Tooltip("Reference to the vehicle controller to get speed from")]
    public WheeledVehicle Vehicle;
    
    // Method name to get speed from the vehicle controller
    private const string SPEED_METHOD_NAME = "GetCurrentSpeed";
    
    // Current FOV value
    private float _currentFOV;
    private float _targetFOV;
    
    /// <inheritdoc/>
    public override void OnStart()
    {
        // Find camera if not assigned
        if (TargetCamera == null)
        {
            TargetCamera = Actor.GetChild<Camera>();
            if (TargetCamera == null)
            {
                Debug.LogError("No camera found for FOV controller. Please assign a camera reference.");
                Enabled = false;
                return;
            }
        }
        
        // Initialize with default FOV
        _currentFOV = DefaultFOV;
        _targetFOV = DefaultFOV;
        TargetCamera.FieldOfView = DefaultFOV;
    }
    
    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Get current speed from vehicle controller
        float currentSpeed = GetVehicleSpeed();
        
        // Calculate target FOV based on speed
        if (currentSpeed > SpeedThreshold)
        {
            float speedOverThreshold = currentSpeed - SpeedThreshold;
            _targetFOV = Math.Min(DefaultFOV + (speedOverThreshold * FOVIncreaseRate), MaxFOV);
        }
        else
        {
            _targetFOV = DefaultFOV;
        }
        
        // Smoothly interpolate to target FOV
        _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, LerpSpeed * Time.DeltaTime);
        
        // Apply FOV to camera
        TargetCamera.FieldOfView = _currentFOV;
    }
    
    private float GetVehicleSpeed()
    {
        
        // Try to call GetCurrentSpeed method on the vehicle controller
        try
        {
            return Mathf.Abs(Vehicle.ForwardSpeed * 3.6f) / 100;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get speed from vehicle controller: {ex.Message}");
        }
        
        return 0.0f;
    }
}