using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Game;


/// <summary>
/// GearboxController Script.
/// </summary>
public class GearboxController : Script
{
    /// <inheritdoc/>
    /// 
    public WheeledVehicle vehicle;
    public UIControl GearLabel;
    private string[] gears = ["R", "N", "1", "2", "3", "4", "5", "6"];

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        if (vehicle.CurrentGear == -1)
        {
            GearLabel.Control = new Label { Text = gears[0], Scale = new Float2(1.3f, 1.3f) };
        }
        else
        {
            GearLabel.Control = new Label { Text = gears[vehicle.CurrentGear + 1], Scale = new Float2(1.3f, 1.3f) };

        }
    }
}
