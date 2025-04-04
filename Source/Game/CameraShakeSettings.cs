using System;
using FlaxEngine;

public class CameraShakeSettings : Script
{
    // Reference to the camera to shake
    public Camera TargetCamera;
    
    // Camera position and movement settings
    public Actor CameraTarget;
    public float CameraDistance = 700.0f;
    public float CameraSmoothing = 20.0f;
    
    // Shake properties
    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private float _shakeDecay = 1.0f; // How quickly the shake effect fades
    private System.Random _random = new System.Random();
    
    // Speed shake properties
    public float SpeedShakeThreshold = 80.0f; // Speed at which shake begins
    public float MaxSpeedShake = 130.0f;      // Speed at which shake reaches maximum intensity
    public float BaseSpeedShakeIntensity = 0.8f; // Base intensity of the speed shake
    private float _lastSpeedShakeTime = 0f;    // To avoid applying shake every frame
    
    // For receiving vehicle data
    private WheeledVehicle _trackedVehicle;
    public bool TrackVehicleSpeed = true;
    
    public override void OnStart()
    {
        // Try to find references if not set
        if (TargetCamera == null)
        {
            TargetCamera = Actor.GetChild<Camera>();
            if (TargetCamera == null)
            {
                Debug.LogWarning("CameraShake: No target camera assigned");
            }
        }
        
        // Try to find a vehicle in the scene if we're tracking speed
        if (TrackVehicleSpeed && _trackedVehicle == null)
        {
            _trackedVehicle = Level.FindActor<WheeledVehicle>();
        }
    }
    
    public override void OnUpdate()
    {
        // Check if we should update speed-based shake
        if (TrackVehicleSpeed && _trackedVehicle != null)
        {
            UpdateSpeedCameraShake(Mathf.Abs(_trackedVehicle.ForwardSpeed * 3.6f) / 100);
        }
    }
    
    public override void OnFixedUpdate()
    {
        if (TargetCamera != null)
        {
            UpdateCameraWithShake();
        }
    }
    
    // Call this method to trigger camera shake
    public void ApplyCameraShake(float intensity, float duration, float decay = 1.0f)
    {
        Debug.Log($"Camera shake triggered: intensity={intensity}, duration={duration}");
        _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        _shakeDuration = Mathf.Max(_shakeDuration, duration);
        _shakeDecay = decay;
    }
    
    // Set the vehicle to track for speed-based camera shake
    public void SetTrackedVehicle(WheeledVehicle vehicle)
    {
        _trackedVehicle = vehicle;
    }
    
    // Update method to check speed and apply shake
    private void UpdateSpeedCameraShake(float currentSpeed)
    {
        // Only check for speed shake every 0.1 seconds to avoid constant small shakes
        if (Time.GameTime > _lastSpeedShakeTime + 0.1f)
        {
            _lastSpeedShakeTime = Time.GameTime;
            
            // If speed is above threshold, apply shake
            if (currentSpeed > SpeedShakeThreshold)
            {
                // Calculate shake intensity based on how far over the threshold we are
                float speedFactor = Mathf.Clamp((currentSpeed - SpeedShakeThreshold) /
                                               (MaxSpeedShake - SpeedShakeThreshold), 0f, 1f);
                
                float intensity = BaseSpeedShakeIntensity * speedFactor;
                
                // Apply a short, continuous shake
                ApplyCameraShake(intensity, 0.15f, 4.0f);
            }
        }
    }
    
    // Modify camera transform to include shake effect
    private void UpdateCameraWithShake()
    {
        var camTrans = TargetCamera.Transform;
        
        // Only apply shake if active
        if (_shakeDuration > 0)
        {
            // Generate random offset based on intensity
            Vector3 shakeOffset = new Vector3(
                (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity,
                (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity,
                (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity
            );
            
            // Apply position shake
            camTrans.Translation += shakeOffset;
            
            // Apply rotation shake
            float rotationShake = _shakeIntensity * 0.5f; // Reduced factor for rotation
            camTrans.Orientation *= Quaternion.Euler(
                (float)(_random.NextDouble() * 2 - 1) * rotationShake,
                (float)(_random.NextDouble() * 2 - 1) * rotationShake,
                (float)(_random.NextDouble() * 2 - 1) * rotationShake
            );
            
            // Reduce shake duration and intensity over time
            _shakeDuration -= Time.DeltaTime;
            _shakeIntensity = Mathf.Max(0, _shakeIntensity - (_shakeDecay * Time.DeltaTime));
            
            if (_shakeDuration <= 0)
            {
                _shakeIntensity = 0;
            }
            
            // Apply the modified transform
            TargetCamera.Transform = camTrans;
        }
    }
    
    // Public method to trigger camera shake via event or message
    public void TriggerShake(float intensity = 1.0f, float duration = 0.5f)
    {
        ApplyCameraShake(intensity, duration);
    }
}