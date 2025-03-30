using FlaxEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor.Content.Settings;
using FlaxEditor;

public class DebugConsole : Script
{
    public WheeledVehicle vehicle;
    private bool showDebugInfo = false;
    private Dictionary<string, List<DebugProperty>> debugCategories = new Dictionary<string, List<DebugProperty>>();
    private Vector2 scrollPosition = Vector2.Zero;
    private float scrollSpeed = 300f; // Pixels per second
    private int currentPage = 0;
    private int propertiesPerPage = 20;
    private string currentFilter = "";
    private bool[] categoryExpanded = new bool[5]; // For the collapsible sections
    
    // New: update timer to control refresh rate
    private float updateTimer = 0f;
    private float updateInterval = 0.1f; // Update values 10 times per second

    // List of properties/fields to ignore
    public List<string> ignoredProperties = new List<string>
    {
        "Transform", "Tag", "Layer", "UseCCD", "StartAwake", 
        "EnableGravity", "IsKinematic", "OverrideMass", "EnableSimulation"
    };

    // Define property categories
    private Dictionary<string, List<string>> propertyCategories = new Dictionary<string, List<string>>
    {
        { "Core", new List<string> { "Name", "Mass", "Wheels", "Speed", "Velocity" } },
        { "Engine", new List<string> { "EngineTorque", "EngineRPM", "MaxRPM", "GearRatio" } },
        { "Physics", new List<string> { "LinearDamping", "AngularDamping", "Friction" } },
        { "Input", new List<string> { "Throttle", "Brake", "Steering" } },
        { "Other", new List<string>() } // Catch-all for uncategorized properties
    };

    private struct DebugProperty
    {
        public string Name;
        public string Value;
    }

    public override void OnStart()
    {
        // Initialize expanded state for categories
        for (int i = 0; i < categoryExpanded.Length; i++)
            categoryExpanded[i] = true;
    }

    public override void OnUpdate()
    {
        // Toggle debug view
        if (Input.GetKeyDown(KeyboardKeys.F2))
        {
            showDebugInfo = !showDebugInfo;
            if (showDebugInfo)
                UpdateDebugText();
        }

        if (!showDebugInfo)
            return;

        // Filter input (example: F3 to enable filter mode)
        if (Input.GetKeyDown(KeyboardKeys.F3))
        {
            // In a real implementation, you'd use a UI input field here
            // This is just a placeholder for the concept
            currentFilter = ""; // Reset filter or implement a way to enter text
        }

        // Handle scrolling
        if (Input.GetKey(KeyboardKeys.PageDown))
            scrollPosition.Y += scrollSpeed * Time.DeltaTime;
        if (Input.GetKey(KeyboardKeys.PageUp))
            scrollPosition.Y -= scrollSpeed * Time.DeltaTime;

        // Page navigation
        if (Input.GetKeyDown(KeyboardKeys.ArrowRight))
            currentPage++;
        if (Input.GetKeyDown(KeyboardKeys.ArrowLeft) && currentPage > 0)
            currentPage--;

        // Toggle category expansion
        if (Input.GetKeyDown(KeyboardKeys.Alpha1))
            categoryExpanded[0] = !categoryExpanded[0]; // Core
        if (Input.GetKeyDown(KeyboardKeys.Alpha2))
            categoryExpanded[1] = !categoryExpanded[1]; // Engine
        if (Input.GetKeyDown(KeyboardKeys.Alpha3))
            categoryExpanded[2] = !categoryExpanded[2]; // Physics
        if (Input.GetKeyDown(KeyboardKeys.Alpha4))
            categoryExpanded[3] = !categoryExpanded[3]; // Input
        if (Input.GetKeyDown(KeyboardKeys.Alpha5))
            categoryExpanded[4] = !categoryExpanded[4]; // Other

        // Update the debug text at controlled intervals for better performance
        updateTimer += Time.DeltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateDebugText();
            updateTimer = 0f;
        }

        DrawDebugInfo();
    }

    private void UpdateDebugText()
    {
        debugCategories.Clear();
        if (vehicle == null)
        {
            var errorList = new List<DebugProperty>
            {
                new DebugProperty { Name = "Error", Value = "Vehicle not assigned!" }
            };
            debugCategories["Error"] = errorList;
            return;
        }

        // Initialize categories
        foreach (var category in propertyCategories.Keys)
            debugCategories[category] = new List<DebugProperty>();

        // Process properties
        foreach (var prop in typeof(WheeledVehicle).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (ignoredProperties.Contains(prop.Name)) continue;
            
            try
            {
                object value = prop.GetValue(vehicle);
                var debugProp = new DebugProperty { Name = prop.Name, Value = value?.ToString() ?? "null" };
                
                // Assign to appropriate category
                bool categorized = false;
                foreach (var category in propertyCategories)
                {
                    if (category.Value.Contains(prop.Name))
                    {
                        debugCategories[category.Key].Add(debugProp);
                        categorized = true;
                        break;
                    }
                }
                
                // If not explicitly categorized, add to Other
                if (!categorized)
                    debugCategories["Other"].Add(debugProp);
            }
            catch
            {
                // Handle properties that might throw exceptions when accessed
                debugCategories["Other"].Add(new DebugProperty { Name = prop.Name, Value = "Error accessing" });
            }
        }

        // Process fields (similar to properties)
        foreach (var field in typeof(WheeledVehicle).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (ignoredProperties.Contains(field.Name)) continue;
            
            try
            {
                object value = field.GetValue(vehicle);
                var debugProp = new DebugProperty { Name = field.Name, Value = value?.ToString() ?? "null" };
                
                bool categorized = false;
                foreach (var category in propertyCategories)
                {
                    if (category.Value.Contains(field.Name))
                    {
                        debugCategories[category.Key].Add(debugProp);
                        categorized = true;
                        break;
                    }
                }
                
                if (!categorized)
                    debugCategories["Other"].Add(debugProp);
            }
            catch
            {
                debugCategories["Other"].Add(new DebugProperty { Name = field.Name, Value = "Error accessing" });
            }
        }

        // Sort properties within each category
        foreach (var category in debugCategories.Keys.ToList())
        {
            debugCategories[category] = debugCategories[category]
                .OrderBy(p => p.Name)
                .ToList();
        }

        // Apply filter if one is set
        if (!string.IsNullOrEmpty(currentFilter))
        {
            foreach (var category in debugCategories.Keys.ToList())
            {
                debugCategories[category] = debugCategories[category]
                    .Where(p => p.Name.ToLower().Contains(currentFilter.ToLower()) || 
                               (p.Value?.ToLower().Contains(currentFilter.ToLower()) ?? false))
                    .ToList();
            }
        }
    }

    private void DrawDebugInfo()
    {
        if (debugCategories.Count == 0) 
            UpdateDebugText();

        Vector2 position = new Vector2(20, 40);
        
        // Draw title and controls
        DebugDraw.DrawText("=== Vehicle Debug Info ===", position, Color.Yellow, 14);
        position.Y += 20;
        DebugDraw.DrawText("Controls: PgUp/PgDn to scroll, Left/Right for pages, Keys 1-5 to toggle sections", 
                           position, Color.LightGray, 10);
        position.Y += 20;

        // Display vehicle basic info at the top
        if (vehicle != null)
        {
            DebugDraw.DrawText($"Name: {vehicle.Name} | Mass: {vehicle.Mass} | Wheels: {vehicle.Wheels}", 
                              position, Color.White, 12);
            position.Y += 20;
        }

        // Apply scroll position
        position.Y -= scrollPosition.Y;

        // Track visible properties for pagination
        int visibleProps = 0;
        int startProp = currentPage * propertiesPerPage;
        int endProp = startProp + propertiesPerPage;

        // Draw each category
        int categoryIndex = 0;
        foreach (var category in debugCategories)
        {
            // Skip empty categories
            if (category.Value.Count == 0)
                continue;

            Color categoryColor = new Color(0.8f, 0.8f, 1.0f);
            string toggleSymbol = categoryExpanded[categoryIndex] ? "[-]" : "[+]";
            
            // Category header
            DebugDraw.DrawText($"{toggleSymbol} {category.Key} ({category.Value.Count})", 
                              position, categoryColor, 12);
            position.Y += 20;

            // Draw properties if category is expanded
            if (categoryExpanded[categoryIndex])
            {
                foreach (var prop in category.Value)
                {
                    // Skip if outside page range
                    if (visibleProps < startProp || visibleProps >= endProp)
                    {
                        visibleProps++;
                        continue;
                    }

                    // Only draw if in visible area of screen
                    if (position.Y >= 20 && position.Y <= Screen.Size.Y - 20)
                    {
                        // Draw property with colored value based on type
                        string propText = $"  {prop.Name}: ";
                        float textWidth = 200; // Approximate for alignment
                        
                        DebugDraw.DrawText(propText, position, Color.White, 10);
                        
                        // Determine value color based on property type or value
                        Color valueColor = Color.LightGreen;
                        if (prop.Value == "0" || prop.Value == "False" || prop.Value == "null")
                            valueColor = Color.Gray;
                        else if (float.TryParse(prop.Value, out float numValue))
                        {
                            if (numValue < 0)
                                valueColor = Color.Red;
                            else if (numValue > 1000)
                                valueColor = Color.Yellow;
                        }
                        
                        DebugDraw.DrawText(prop.Value, position + new Vector2(textWidth, 0), valueColor, 10);
                    }
                    
                    position.Y += 17;
                    visibleProps++;
                }
            }
            
            position.Y += 10; // Space between categories
            categoryIndex++;
        }

        // Draw page info
        int totalPages = (int)System.Math.Ceiling((float)GetTotalPropertyCount() / propertiesPerPage);
        Vector2 pageInfoPos = new Vector2(Screen.Size.X - 150, Screen.Size.Y - 30);
        DebugDraw.DrawText($"Page {currentPage + 1}/{totalPages}", pageInfoPos, Color.White, 10);
        
        // Draw filter info if active
        if (!string.IsNullOrEmpty(currentFilter))
        {
            Vector2 filterInfoPos = new Vector2(20, Screen.Size.Y - 30);
            DebugDraw.DrawText($"Filter: {currentFilter}", filterInfoPos, Color.Yellow, 10);
        }
    }

    private int GetTotalPropertyCount()
    {
        int count = 0;
        foreach (var category in debugCategories.Values)
            count += category.Count;
        return count;
    }
}