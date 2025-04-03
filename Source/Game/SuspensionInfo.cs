using FlaxEngine;
using FlaxEngine.GUI;

public class SuspensionInfo : Script
{
    // Référence au véhicule
    public WheeledVehicle Vehicle;
    
    // Références aux labels
    public UIControl SpeedLabel;

    public UIControl fpsLabel;

    public override void OnUpdate()
    {
        SpeedLabel.Control = new Label{Text = (Mathf.Abs(Vehicle.ForwardSpeed * 3.6f)/100).ToString("F0") + " Km/h", Scale = new Float2(1.3f, 1.3f)};
        fpsLabel.Control = new Label{TextColor=Color.Green, Text=Engine.FramesPerSecond.ToString("F0") + "fps"};
    }
}   