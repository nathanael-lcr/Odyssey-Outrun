using FlaxEngine;
using System.Reflection;
using System.Collections.Generic;

public class DebugConsole : Script
{
    public WheeledVehicle vehicle;
    private bool showDebugInfo = false;
    private List<string> debugLines = new List<string>();

    // List of properties/fields to ignore
    public List<string> ignoredProperties = new List<string>
    {
        "Transform",  // Example: Skip transform (if it exists)
        "Tag",        // Example: Skip tag
        "Layer",
        "UseCCD",
        "StartAwake",
        "EnableGravity",
        "IsKinematic",
        "OverrideMass",
        "EnableSimulation"
    };

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyboardKeys.F2))
        {
            showDebugInfo = !showDebugInfo;
            if (showDebugInfo)
                UpdateDebugText();
        }

        if (showDebugInfo)
            DrawDebugInfo();
    }

    private void UpdateDebugText()
    {
        debugLines.Clear();

        if (vehicle == null)
        {
            debugLines.Add("Vehicle not assigned!");
            return;
        }

        debugLines.Add("=== Vehicle Debug Info ===");
        debugLines.Add($"Name: {vehicle.Name}");
        debugLines.Add($"Mass: {vehicle.Mass}");
        debugLines.Add($"Wheel Count: {vehicle.Wheels}");

        // Get properties, skipping ignored ones
        debugLines.Add("\n=== Properties ===");
        debugLines.Add("\n");
        foreach (var prop in typeof(WheeledVehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (ignoredProperties.Contains(prop.Name)) continue; // Skip ignored properties
            
            object value = prop.GetValue(vehicle);
            debugLines.Add($"{prop.Name}: {value}");
        }

        // Get fields, skipping ignored ones
        debugLines.Add("\n=== Fields ===");
        foreach (var field in typeof(WheeledVehicle).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (ignoredProperties.Contains(field.Name)) continue; // Skip ignored fields
            
            object value = field.GetValue(vehicle);
            debugLines.Add($"{field.Name}: {value}");
        }
    }

    private void DrawDebugInfo()
    {
        Vector2 screenPos = new Vector2(20, 40);

        for (int i = 0; i < debugLines.Count; i++)
        {
            DebugDraw.DrawText(debugLines[i], screenPos + new Vector2(0, i * 17), Color.White, 10);
        }
    }
}
