using UnityEngine;
using UnityEngine.VFX; // Required for Visual Effect Graph

[RequireComponent(typeof(AudioSource))]
public class VFXVoiceReactorMic : MonoBehaviour
{
    // --- Assign in Inspector ---
    [Header("Connections")]
    public AudioSource audioSource; // The AudioSource to channel the mic through
    public VisualEffect vfxGraph;   // The Visual Effect Graph you want to control

    [Header("Reactivity Settings")]
    [Tooltip("How sensitive the amplitude reaction is.")]
    public float amplitudeMultiplier = 100.0f;
    [Tooltip("How quickly the effect smoothes out and returns to baseline.")]
    public float smoothingSpeed = 50.0f;
    [Tooltip("The color of the orb when silent.")]
    public Color silentColor = Color.white;
    [Tooltip("The color of the orb when speaking.")]
    public Color speakingColor = Color.cyan;
    
    // --- Internal Data ---
    private float[] audioSamples;
    private float[] spectrumData;
    private float smoothedAmplitude = 0f;
    private AudioClip micClip;

    void Start()
    {
        // --- NEW MICROPHONE SETUP ---
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected! Please connect a microphone.");
            return; // Stop the script if no mic is found
        }

        string micName = Microphone.devices[0]; // Use the default microphone
        Debug.Log("Using microphone: " + micName);

        // Start recording with a short, looping audio clip (1 second long, 44100 Hz)
        micClip = Microphone.Start(micName, true, 1, 44100);
        audioSource.clip = micClip;
        audioSource.loop = true; // Make sure the AudioSource loops

        // Wait until the microphone has started recording
        while (!(Microphone.GetPosition(micName) > 0)) { }

        audioSource.Play(); // Start playing the audio source
        Debug.Log("Microphone is live and playing through AudioSource.");
        // --- END NEW MICROPHONE SETUP ---

        // The rest of the setup is the same as before
        audioSamples = new float[1024];
        spectrumData = new float[512];
        
        // Set initial "silent" state
        vfxGraph.SetVector4("Color", silentColor);
    }

    void Update()
    {
        // This part is exactly the same! It analyzes the audio from the AudioSource.
        // It doesn't care if the source is a TTS clip or a live microphone.

        // 1. Get the live audio data
        audioSource.GetOutputData(audioSamples, 0);

        // 2. Calculate volume (RMS amplitude)
        float currentAmplitude = 0;
        foreach (float sample in audioSamples)
        {
            currentAmplitude += Mathf.Abs(sample);
        }
        currentAmplitude /= audioSamples.Length;
        currentAmplitude *= amplitudeMultiplier;

        // 3. Smooth the value
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, currentAmplitude, Time.deltaTime * smoothingSpeed);

        // --- Apply the analysis to the VFX Graph ---

        // A. Control Size/Intensity with Amplitude
        vfxGraph.SetFloat("Size", 2.0f + smoothedAmplitude * 0.5f);
        vfxGraph.SetFloat("TurbulenceIntensity", 5.0f + smoothedAmplitude * 20.0f);

        // B. Control Color with Amplitude
        Color voiceColor = Color.Lerp(silentColor, speakingColor, smoothedAmplitude);
        vfxGraph.SetVector4("Color", voiceColor);
        
        // C. (Advanced) Control another property with Frequency
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
        
        float trebleEnergy = 0;
        int trebleStart = spectrumData.Length / 4 * 3;
        for (int i = trebleStart; i < spectrumData.Length; i++)
        {
            trebleEnergy += spectrumData[i];
        }
        vfxGraph.SetFloat("TrailLifetime", 0.5f + trebleEnergy * 10f);
    }
}
