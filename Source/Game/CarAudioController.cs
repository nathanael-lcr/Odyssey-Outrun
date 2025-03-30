using FlaxEngine;

public class CarAudioController : Script
{
    // Audio source for the engine sound
    public AudioSource EngineAudioSource;

    // Audio clips for different engine states
    public AudioClip IdleSound;
    public AudioClip LowRPMSound;
    public AudioClip MediumRPMSound;
    public AudioClip HighRPMSound;

    // Single audio clip option
    public AudioClip EngineSound;

    // Reference to the car
    public WheeledVehicle Car;

    // Option to use single sound with pitch variation
    public bool UseSingleSoundWithPitchVariation = true;

    // Audio parameters
    public float MinPitch = 0.5f;
    public float MaxPitch = 2.0f;
    public float PitchMultiplier = 0.01f;

        // Loudness control
    [Tooltip("Base volume of the engine sound")]
    public float BaseVolume = 0.5f;  // Default volume between 0 and 1

    [Tooltip("Maximum volume limit")]
    public float MaxVolume = 1.0f;   // Maximum possible volume

    [Tooltip("How quickly volume changes")]
    public float VolumeResponseCurve = 2f;  // Exponent to control volume sensitivity


    public override void OnStart()
    {
        // Ensure audio source is set up
        if (EngineAudioSource == null)
        {
            EngineAudioSource = Actor.AddChild<AudioSource>();
        }

        // Set initial audio source properties
        EngineAudioSource.IsLooping = true;
        
        if (UseSingleSoundWithPitchVariation)
        {
            EngineAudioSource.Clip = EngineSound;
        }
        else
        {
            EngineAudioSource.Clip = IdleSound;
        }

        EngineAudioSource.Play();
    }

    public override void OnUpdate()
    {
        if (Car == null || EngineAudioSource == null) return;

        // Get RPM directly from the car
        float RPM = Car.EngineRotationSpeed;

         // Calculate volume with a custom response curve
        float normalizedRPM = RPM / Car.Engine.MaxRotationSpeed;
        float volumeCurve = Mathf.Pow(normalizedRPM, VolumeResponseCurve);
        
        float finalVolume = Mathf.Clamp(
            BaseVolume + (volumeCurve * (MaxVolume - BaseVolume)), 
            0f, 
            MaxVolume
        );


        if (UseSingleSoundWithPitchVariation)
        {
            float pitch = Mathf.Lerp(MinPitch, MaxPitch, normalizedRPM);
            EngineAudioSource.Pitch = pitch;
            EngineAudioSource.Volume = finalVolume;
        }
        else
        {
            // Determine appropriate sound clip based on RPM
            AudioClip currentClip = DetermineEngineSound(RPM);
            
            // Set the audio clip if it's different from current
            if (EngineAudioSource.Clip != currentClip)
            {
                EngineAudioSource.Clip = currentClip;
                EngineAudioSource.Play();
            }

            // Adjust pitch based on RPM
            float pitch = Mathf.Clamp(
                MinPitch + (RPM * PitchMultiplier), 
                MinPitch, 
                MaxPitch
            );
            EngineAudioSource.Pitch = pitch;

            // Adjust volume based on RPM
            EngineAudioSource.Volume = Mathf.Clamp(RPM / 6000f, 0f, 1f);
        }
    }

    private AudioClip DetermineEngineSound(float rpm)
    {
        if (rpm < 1000f)
            return IdleSound;
        else if (rpm < 3000f)
            return LowRPMSound;
        else if (rpm < 5000f)
            return MediumRPMSound;
        else
            return HighRPMSound;
    }
}