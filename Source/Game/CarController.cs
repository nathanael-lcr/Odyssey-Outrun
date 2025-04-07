using System;
using System.Runtime.ConstrainedExecution;
using FlaxEngine;

public class CarController : Script
{
    public WheeledVehicle Car;
    public Actor CameraTarget;
    public Camera Camera;

    // References to wheel actors for position
    private Actor[] WheelActors = new Actor[4];

    public float CameraSmoothing = 20.0f;

    public bool UseMouse = true;
    public float CameraDistance = 700.0f;

    public float MouseSensitivity = 0.5f;

    private float _pitch = 10.0f;
    private float _yaw = 90.0f;
    private float _horizontal;
    private float _vertical;

    private bool HandbrakePressed = false;

    // Arrays to store the angle and grip data for each wheel
    private float[] PrevAngles = new float[4];
    private float[] CurrentAngles = new float[4];
    private float[] SmoothedGripLevels = new float[4] { 1f, 1f, 1f, 1f };

    private float TempsEcoule;

    public float smoothingFactor = 0.1f; // Facteur de lissage (0 à 1)

    private bool performanceMode = false;

    // Wheel position offsets for displaying text
    public Vector3[] WheelTextOffsets = new Vector3[4] {
        new Vector3(0, 30, 0),  // Front Left
        new Vector3(0, 30, 0),  // Front Right
        new Vector3(0, 30, 0),  // Rear Left
        new Vector3(0, 30, 0)   // Rear Right
    };

    public Actor[] WheelsPosition;

    // Add these fields to your class
    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private float _shakeDecay = 1.0f; // How quickly the shake effect fades
    private System.Random _random = new System.Random();

    // Add these fields to your class (if not already present)
    private float _speedShakeThreshold = 80.0f; // Speed at which shake begins (adjust based on your game's scale)
    private float _maxSpeedShake = 130.0f;      // Speed at which shake reaches maximum intensity
    private float _baseSpeedShakeIntensity = 0.8f; // Base intensity of the speed shake
    private float _lastSpeedShakeTime = 0f;    // To avoid applying shake every frame

    public float[] gearRatios = new float[] { -2.5f, 0f, 2.8f, 2.0f, 1.5f, 1.2f, 1.0f }; // R, N, 1, 2, 3, 4, 5

    /// <summary>
    /// Adds the movement and rotation to the camera (as input).
    /// </summary>
    /// <param name="horizontal">The horizontal input.</param>
    /// <param name="vertical">The vertical input.</param>
    /// <param name="pitch">The pitch rotation input.</param>
    /// <param name="yaw">The yaw rotation input.</param>
    public void AddMovementRotation(float horizontal, float vertical, float pitch, float yaw)
    {
        _pitch += pitch;
        _yaw += yaw;
        _horizontal += horizontal;
        _vertical += vertical;
    }

    public override void OnStart()
    {
        // If wheel actors are not set, try to find them as children of the car
        if (WheelActors[0] == null && Car != null)
        {
            // Try to automatically assign wheel actors based on children with expected names
            // This is a fallback in case the wheel actors weren't manually assigned
            for (int i = 0; i < Car.ChildrenCount && i < 4; i++)
            {
                var child = Car.GetChild(i);
                if (child.Name.Contains("Wheel") || child.Name.Contains("wheel"))
                {
                    WheelActors[i] = child;
                }
            }
        }
    }

    public override void OnUpdate()
    {
        if (UseMouse)
        {
            // Cursor
            Screen.CursorVisible = false;
            Screen.CursorLock = CursorLockMode.Locked;

            // Mouse
            var mouseDelta = new Float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch + mouseDelta.Y, -6, 88);
            _yaw += mouseDelta.X;
        }
    }

    // Calculate grip level for a wheel
    private float CalculateGripLevel(WheeledVehicle.WheelState wheelState, int wheelIndex)
    {
        float wheelRadius = Car.Wheels[wheelIndex].Radius;
        float theoricalRotationSpeed = Mathf.Abs(Car.ForwardSpeed) / (2 * Mathf.Pi * wheelRadius);

        float normalizedRotation = Mathf.Abs(wheelState.RotationAngle) / 1800 * 5 % 1f;
        float rotationRadians = normalizedRotation * 2 * Mathf.Pi;

        CurrentAngles[wheelIndex] = rotationRadians;
        float vitesseAngulaire = (CurrentAngles[wheelIndex] - PrevAngles[wheelIndex]) / TempsEcoule;
        float vitesseAngulairerpm = Mathf.Abs(vitesseAngulaire) / (2 * Mathf.Pi);

        // Store current angle for next calculation
        PrevAngles[wheelIndex] = CurrentAngles[wheelIndex];

        // Calculate grip level
        float gripLevel = 1f; // Default to max grip
        if (theoricalRotationSpeed != 0f)
        {
            float difference = Mathf.Abs(vitesseAngulairerpm - theoricalRotationSpeed);
            gripLevel = Mathf.Clamp(1f - (difference / theoricalRotationSpeed), 0f, 1f);
            SmoothedGripLevels[wheelIndex] = (smoothingFactor * gripLevel) + ((1f - smoothingFactor) * SmoothedGripLevels[wheelIndex]);
        }

        return SmoothedGripLevels[wheelIndex];
    }

    public override void OnFixedUpdate()
    {
        UpdateCameraWithShake();
        UpdateSpeedCameraShake(Mathf.Abs(Car.ForwardSpeed * 3.6f) / 100);

        // Separate keyboard and controller input handling
        float keyboardH = Input.GetAxis("Horizontal");
        float keyboardV = Input.GetAxis("Vertical");
        float controllerH = _horizontal; // Assuming this comes from controller
        float controllerV = _vertical;   // Assuming this comes from controller

        // Use the inputs (combined or with priority)
        float inputH = keyboardH + controllerH;
        float inputV = keyboardV + controllerV;

        // Reset controller variables for next frame
        _horizontal = 0;
        _vertical = 0;

        var velocity = new Float3(inputH, 0.0f, inputV);
        //velocity.Normalize();

        if (Input.GetKeyDown(KeyboardKeys.F1) && performanceMode == false)
        {
            Debug.Log("Performance Mode Enabled");
            Time.DrawFPS = 60;
            Time.PhysicsFPS = 60;
            Time.UpdateFPS = 60;
            performanceMode = true;
        }
        else if (Input.GetKeyDown(KeyboardKeys.F1) && performanceMode == true)
        {
            Debug.Log("Quality Mode Enabled");
            Time.DrawFPS = 120;
            Time.PhysicsFPS = 120;
            Time.UpdateFPS = 120;
            performanceMode = false;
        }


        if (Input.GetKeyDown(KeyboardKeys.Return))
        {
            ApplyCameraShake(1f, 0.7f, 1.8f);
        }


        //Car.SetThrottle(velocity.Z);
        Car.SetSteering(velocity.X);

        if (Input.GetAction("Handbrake"))
        {
            Car.SetHandbrake(1.0f);
            HandbrakePressed = true;
        }
        else
        {
            Car.SetHandbrake(0.0f);
            HandbrakePressed = false;
        }

        // Get the screen dimensions
        Vector2 screenSize = Screen.Size;

        // Calculate bottom right position (with some padding)
        Vector2 bottomRightPosition = new Vector2(screenSize.X - 200, screenSize.Y - 30);

        // Draw first debug text line
        DebugDraw.DrawText(
            Car.EngineRotationSpeed.ToString("F0") + " rpm",
            bottomRightPosition,
            Color.White,
            12,
            0F
        );

        // Draw second debug text line (above the first one)
        DebugDraw.DrawText(
            velocity.Z.ToString(),
            new Vector2(bottomRightPosition.X, bottomRightPosition.Y - 17), // 17 pixels up
            Color.White,
            12,
            0F
        );

        // Draw second debug text line (above the first one)
        DebugDraw.DrawText(
            "Frametime : " + (1000 / Engine.FramesPerSecond).ToString() + "ms",
            new Vector2(bottomRightPosition.X, bottomRightPosition.Y - 34), // 34 pixels up
            Color.Green,
            12,
            0F
        );

        // Update time for angle calculations
        TempsEcoule = Time.DeltaTime;

        // Array to store wheel states
        WheeledVehicle.WheelState[] wheelStates = new WheeledVehicle.WheelState[4];

        // Get state for each wheel
        for (int i = 0; i < 4 && i < Car.Wheels.Length; i++)
        {
            Car.GetWheelState(i, out wheelStates[i]);
        }

        // Calculate and display grip for each wheel
        string[] wheelNames = { "FL", "FR", "RL", "RR" }; // Front Left, Front Right, Rear Left, Rear Right

        for (int i = 0; i < 4 && i < Car.Wheels.Length; i++)
        {
            float gripLevel = CalculateGripLevel(wheelStates[i], i);

            // Only display if we have a reference to the wheel actor
            if (WheelActors[i] != null)
            {
                // Get wheel position from the actor and add offset for better visibility
                Vector3 textPosition = WheelActors[i].Position + WheelTextOffsets[i];

                // Color code based on grip level
                Color textColor = gripLevel < 0.35f ? Color.Red : Color.Green;

                // Display the wheel grip with wheel identifier
                DebugDraw.DrawText(
                    wheelNames[i] + ": " + gripLevel.ToString("F1"),
                    textPosition,
                    textColor,
                    17,
                    0F,
                    1F
                );
            }
            else
            {
                // If no wheel actor reference is available, display at car position with an offset
                Vector3 offset = new Vector3(
                    (i % 2 == 0 ? -1 : 1) * 100,  // Left vs Right
                    30,
                    (i < 2 ? 1 : -1) * 100        // Front vs Rear
                );

                Color textColor = gripLevel < 0.35f ? Color.Red : Color.Green;

                DebugDraw.DrawText(
                    wheelNames[i] + ": " + gripLevel.ToString("F1"),
                    WheelsPosition[i].Position,
                    textColor,
                    17,
                    0F,
                    1F
                );
            }
        }

        // Display speed at the car position
        DebugDraw.DrawText(
            (Mathf.Abs(Car.ForwardSpeed * 3.6f) / 100).ToString("F1") + " km/h",
            Car.Position + new Vector3(0, 70, 0),
            Color.Blue,
            17,
            0F,
            1F
        );

        // Display gear at the car position
        DebugDraw.DrawText(
            Car.CurrentGear + " gear",
            Car.Position + new Vector3(0, 50, 0),
            Color.Yellow,
            17,
            0F,
            1F
        );
    }

    // Call this method to trigger camera shake
    public void ApplyCameraShake(float intensity, float duration, float decay = 1.0f)
    {
        _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        _shakeDuration = Mathf.Max(_shakeDuration, duration);
        _shakeDecay = decay;
    }

    // Modify your camera update code to include shake effect
    private void UpdateCameraWithShake()
    {
        var camTrans = Camera.Transform;
        var camFactor = Mathf.Saturate(CameraSmoothing * Time.DeltaTime);

        // Apply normal camera movement
        CameraTarget.LocalOrientation = Quaternion.Lerp(CameraTarget.LocalOrientation, Quaternion.Euler(_pitch, _yaw, 0), camFactor);

        // Calculate base camera position
        Vector3 basePosition = CameraTarget.Position + CameraTarget.Direction * -CameraDistance;

        // Apply camera shake if active
        Vector3 shakeOffset = Vector3.Zero;
        if (_shakeDuration > 0)
        {
            // Generate random offset based on intensity
            shakeOffset = new Vector3(
                (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity,
                (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity,
                (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity
            );

            // Reduce shake duration and intensity over time
            _shakeDuration -= Time.DeltaTime;
            _shakeIntensity = Mathf.Max(0, _shakeIntensity - (_shakeDecay * Time.DeltaTime));

            if (_shakeDuration <= 0)
            {
                _shakeIntensity = 0;
            }
        }

        // Apply normal movement plus shake
        camTrans.Translation = Vector3.Lerp(camTrans.Translation, basePosition + shakeOffset, camFactor);
        camTrans.Orientation = CameraTarget.Orientation;

        // Apply rotation shake if desired (optional)
        if (_shakeDuration > 0)
        {
            float rotationShake = _shakeIntensity * 0.5f; // Reduced factor for rotation
            camTrans.Orientation *= Quaternion.Euler(
                (float)(_random.NextDouble() * 2 - 1) * rotationShake,
                (float)(_random.NextDouble() * 2 - 1) * rotationShake,
                (float)(_random.NextDouble() * 2 - 1) * rotationShake
            );
        }

        Camera.Transform = camTrans;
    }

    // Update method to check speed and apply shake
    private void UpdateSpeedCameraShake(float currentSpeed)
    {
        // Only check for speed shake every 0.1 seconds to avoid constant small shakes
        if (Time.GameTime > _lastSpeedShakeTime + 0.1f)
        {
            _lastSpeedShakeTime = Time.GameTime;

            // If speed is above threshold, apply shake
            if (currentSpeed > _speedShakeThreshold)
            {
                // Calculate shake intensity based on how far over the threshold we are
                float speedFactor = Mathf.Clamp((currentSpeed - _speedShakeThreshold) /
                                                 (_maxSpeedShake - _speedShakeThreshold), 0f, 1f);

                float intensity = _baseSpeedShakeIntensity * speedFactor;

                // Apply a short, continuous shake
                ApplyCameraShake(intensity, 0.15f, 4.0f);
            }
        }
    }
}