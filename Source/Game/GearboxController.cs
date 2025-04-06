using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Game;

/// <summary>
/// Enhanced GearboxController Script with throttle cut during shifts.
/// </summary>
public class GearboxController : Script
{
    /// <summary>
    /// Reference to the vehicle.
    /// </summary>
    public WheeledVehicle vehicle;
    
    /// <summary>
    /// UI element to display current gear.
    /// </summary>
    public UIControl GearLabel;
    
    /// <summary>
    /// Names of the gears for UI display.
    /// </summary>
    private string[] gears = ["R", "N", "1", "2", "3", "4", "5", "6"];
    
    /// <summary>
    /// Gear ratios for each gear: R, N, 1, 2, 3, 4, 5, 6
    /// </summary>
    public float[] gearRatios = new float[] { -2.5f, 0f, 2.8f, 2.0f, 1.5f, 1.2f, 1.0f, 0.9f };
    
    /// <summary>
    /// The current gear index (starting with 2 for 1st gear).
    /// </summary>
    public int currentGear = 2; // Start in 1st gear
    
    /// <summary>
    /// Final drive ratio that multiplies all gear ratios.
    /// </summary>
    public float finalDriveRatio = 3.42f;
    
    /// <summary>
    /// Duration of throttle cut during gear changes in seconds.
    /// </summary>
    public float throttleCutDuration = 0.5f;
    
    /// <summary>
    /// Timer to track throttle cut duration.
    /// </summary>
    private float throttleCutTimer = 0.0f;
    
    /// <summary>
    /// Flag to indicate if throttle cut is active.
    /// </summary>
    private bool isShifting = false;
    
    /// <summary>
    /// Store the original throttle value before cutting.
    /// </summary>
    private float originalThrottleValue = 0.0f;
    
    /// <summary>
    /// Last throttle input from player before modification.
    /// </summary>
    private float playerThrottleInput = 0.0f;
    
    /// <summary>
    /// Update called every frame.
    /// </summary>
    public override void OnUpdate()
    {
        UpdateGearDisplay();
        HandleGearShifting();
        HandleThrottleCut();
        
        // Get player throttle input
        float rawThrottle = 0.0f;
        if (Input.GetKey(KeyboardKeys.W))
        {
            rawThrottle = 1.0f;
        }
        else if (Input.GetKey(KeyboardKeys.S))
        {
            rawThrottle = -1.0f;
        }
        playerThrottleInput = rawThrottle;
        
        // Apply throttle (possibly modified by gear shifting)
        float effectiveThrottle = isShifting ? 0.0f : playerThrottleInput;
        vehicle.SetThrottle(effectiveThrottle);
        
        // Debug engine speed with total ratio
        if (Time.GameTime % 1.0f < 0.016f) // Log approximately once per second
        {
            Debug.Log($"Engine RPM: {vehicle.EngineRotationSpeed}, Current Gear: {currentGear}, Ratio: {GetTotalRatio()}");
        }
    }
    
    /// <summary>
    /// Updates the gear display in the UI.
    /// </summary>
    private void UpdateGearDisplay()
    {
        string gearText;
        
        // Handle reverse gear display
        if (currentGear == 0)
        {
            gearText = gears[0]; // "R"
        }
        // Handle neutral and forward gears
        else
        {
            gearText = gears[currentGear];
        }
        
        // Update UI
        if (GearLabel != null)
        {
            GearLabel.Control = new Label { 
                Text = gearText, 
                Scale = new Float2(1.3f, 1.3f),
                TextColor = isShifting ? Color.Orange : Color.White // Visual indication of shifting
            };
        }
    }
    
    /// <summary>
    /// Handles gear shifting based on input.
    /// </summary>
    private void HandleGearShifting()
    {
        // Only allow shifting if not already in a shift
        if (!isShifting)
        {
            // Upshift
            if (Input.GetKeyDown(KeyboardKeys.E) && currentGear < gearRatios.Length - 1)
            {
                // Store current throttle value before cutting
                originalThrottleValue = playerThrottleInput;
                InitiateShift(currentGear + 1);
            }
            // Downshift
            else if (Input.GetKeyDown(KeyboardKeys.Q) && currentGear > 0)
            {
                // Store current throttle value before cutting
                originalThrottleValue = playerThrottleInput;
                InitiateShift(currentGear - 1);
            }
        }
    }
    
    /// <summary>
    /// Initiates a gear shift to the specified gear.
    /// </summary>
    private void InitiateShift(int targetGear)
    {
        // Start throttle cut
        isShifting = true;
        throttleCutTimer = throttleCutDuration;
        
        // Cut throttle immediately
        vehicle.SetThrottle(0.0f);
        
        // Update gear
        currentGear = targetGear;
        
        // Update vehicle's gear (handle reverse gear properly)
        if (currentGear == 0)
        {
            vehicle.CurrentGear = -1; // Reverse
        }
        else if (currentGear == 1)
        {
            vehicle.CurrentGear = 0; // Neutral
        }
        else
        {
            vehicle.CurrentGear = currentGear - 1; // Forward gears
        }
    }
    
    /// <summary>
    /// Handles the throttle cut timer and restoration.
    /// </summary>
    private void HandleThrottleCut()
    {
        if (isShifting)
        {
            throttleCutTimer -= Time.DeltaTime;
            
            // Restore throttle after the cut duration
            if (throttleCutTimer <= 0.0f)
            {
                isShifting = false;
                // The throttle will be set in the next OnUpdate frame
                // based on current input
            }
        }
    }
    
    /// <summary>
    /// Gets the total gear ratio including final drive.
    /// </summary>
    public float GetTotalRatio()
    {
        return gearRatios[currentGear] * finalDriveRatio;
    }
    
    /// <summary>
    /// Called when script is enabled.
    /// </summary>
    public override void OnEnable()
    {
        // Initialize
        if (vehicle != null)
        {
            // Set initial gear
            if (currentGear == 0)
            {
                vehicle.CurrentGear = -1; // Reverse
            }
            else if (currentGear == 1)
            {
                vehicle.CurrentGear = 0; // Neutral
            }
            else
            {
                vehicle.CurrentGear = currentGear - 1; // Forward gears
            }
        }
    }
    
    /// <summary>
    /// Called when script starts.
    /// </summary>
    public override void OnStart()
    {
        // Initialize gear display
        UpdateGearDisplay();
    }
}