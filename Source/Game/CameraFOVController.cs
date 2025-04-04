using System;
using FlaxEngine;

/// <summary>
/// Camera FOV controller that dynamically adjusts based on vehicle speed using a sigmoidal function.
/// Ensures slow increase at low speeds and a rapid boost near 130 km/h.
/// </summary>
public class CameraFOVController : Script
{
    [Header("FOV Settings")]
    public float DefaultFOV = 60.0f;
    public float MaxFOV = 80.0f;

    [Tooltip("Contrôle la vitesse de transition vers le FOV cible.")]
    public float LerpSpeed = 3.0f;

    [Header("Speed Scaling")]
    [Tooltip("Vitesse à laquelle le FOV commence à accélérer son augmentation.")]
    public float SpeedMidpoint = 100.0f;

    [Tooltip("Contrôle la pente de la montée rapide du FOV. Plus haut = transition plus brutale.")]
    public float SpeedSharpness = 0.1f;

    [Header("References")]
    public Camera TargetCamera;
    public WheeledVehicle Vehicle;

    private float _currentFOV;
    private float _targetFOV;

    public override void OnStart()
    {
        if (TargetCamera == null)
        {
            TargetCamera = Actor.GetChild<Camera>() ?? Actor.Parent?.GetChild<Camera>();
            if (TargetCamera == null)
            {
                Debug.LogError("Aucune caméra trouvée.");
                Enabled = false;
                return;
            }
        }

        if (Vehicle == null)
        {
            Debug.LogError("La référence 'Vehicle' n'est pas définie.");
            Enabled = false;
            return;
        }

        _currentFOV = DefaultFOV;
        TargetCamera.FieldOfView = DefaultFOV;
    }

    public override void OnUpdate()
    {
        float currentSpeed = GetVehicleSpeed();

        // --- Calcul du FOV avec courbe sigmoïde ---
        float normalizedSpeed = (currentSpeed - SpeedMidpoint) * SpeedSharpness;
        float sigmoidFactor = 1.0f / (1.0f + Mathf.Exp(-normalizedSpeed)); // Sigmoid function

        _targetFOV = Mathf.Lerp(DefaultFOV, MaxFOV, sigmoidFactor);

        // --- Interpolation vers la valeur cible ---
        float lerpFactor = Mathf.Clamp(LerpSpeed * Time.DeltaTime, 0f, 1f);
        _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, lerpFactor);
        TargetCamera.FieldOfView = _currentFOV;
    }

    private float GetVehicleSpeed()
    {
        if (Vehicle == null) return 0.0f;
        try
        {
            return Mathf.Abs(Vehicle.ForwardSpeed * 3.6f) / 100;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Erreur de récupération de vitesse : {ex.Message}");
            return 0.0f;
        }
    }
}
