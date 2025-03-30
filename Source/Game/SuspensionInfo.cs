using FlaxEngine;

public class SuspensionInfo : Script
{
    // Référence au véhicule et au TextRender
    public WheeledVehicle Vehicle;
    public TextRender InfoText;
    
    public override void OnUpdate()
    {
        if (Vehicle == null || InfoText == null)
            return;
        
        try
        {
            string info = $"gear : {Vehicle.CurrentGear}\n Speed : {(Mathf.Abs(Vehicle.ForwardSpeed * 3.6f)/100).ToString("F0")} km/h";
            
            InfoText.Text = info;
        }
        catch (System.Exception ex)
        {
            InfoText.Text = "Erreur: " + ex.Message;
        }
    }
}