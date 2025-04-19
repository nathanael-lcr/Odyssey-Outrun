using FlaxEngine;
using FlaxEngine.GUI;
using Game;

public class SuspensionInfo : Script
{
    // Référence au véhicule
    public WheeledVehicle Vehicle;

    // Références aux labels
    public UIControl SpeedLabel;
    public UIControl GearLabel;
    public UIControl fpsLabel;
    public UIControl RPMBar;
    public UIControl Watch;

    public TimeOTDHandler TimeHander;

    public FontReference SpeedFont;
    public FontReference GearFont;
    public FontReference WatchFont;

    private string[] gears = ["R", "N", "1", "2", "3", "4", "5", "6"];

    public override void OnStart()
    {
        TimeHander.SetTime(12, 0, 0);
    }

    public override void OnUpdate()
    {
        SpeedLabel.Control = new Label
        {
            Text = (Mathf.Abs(Vehicle.ForwardSpeed * 3.6f) / 100).ToString("F0"),
            Width = 40f,
            Height = 60f,
            Font = SpeedFont

        };

        fpsLabel.Control = new Label
        {
            TextColor = Color.Green,
            Text = Engine.FramesPerSecond.ToString("F0") + "fps",
        };


        if (Vehicle.EngineRotationSpeed > Vehicle.Engine.MaxRotationSpeed - 800)
        {
            RPMBar.Control = new ProgressBar
            {
                Value = Vehicle.EngineRotationSpeed / (Vehicle.Engine.MaxRotationSpeed / 100),
                BarColor = Color.DarkOrange,
                Origin = ProgressBar.BarOrigin.VerticalBottom,
                Width = 30f,
                Height = 100f,
                BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f),
                BarMargin = Margin.Zero
            };
        }
        else
        {
            RPMBar.Control = new ProgressBar
            {
                Value = Vehicle.EngineRotationSpeed / (Vehicle.Engine.MaxRotationSpeed / 100),
                BarColor = Color.Orange,
                Origin = ProgressBar.BarOrigin.VerticalBottom,
                Width = 30f,
                Height = 100f,
                BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.1f),
                BarMargin = Margin.Zero
            };
        }

        Watch.Control = new Label
        {
            Text = TimeHander.GetTimeString(false),
            Font = WatchFont
        };


        if (Vehicle.CurrentGear == -1)
        {
            GearLabel.Control = new Label { Text = gears[0], Font = GearFont };
        }
        else if (Vehicle.CurrentGear == 0)
        {
            GearLabel.Control = new Label { Text = gears[1], Font = GearFont, TextColor = Color.Orange };
        }
        else
        {
            GearLabel.Control = new Label { Text = gears[Vehicle.CurrentGear + 1], Font = GearFont };

        }
    }
}