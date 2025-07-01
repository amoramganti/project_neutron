using UnityEngine;
using UnityEngine.VFX; // Required for Visual Effect Graph

[RequireComponent(typeof(AudioSource))]
public class VFXVoiceReactor : MonoBehaviour
{
    // --- Assign in Inspector ---
    [Header("Connections")]
    public AudioSource audioSource; // The AudioSource playing the TTS
    public VisualEffect vfxGraph;   // The Visual Effect Graph you want to control

    [Header("Reactivity Settings")]
    [Tooltip("How sensitive the amplitude reaction is.")]
    public float amplitudeMultiplier = 10.0f;
    [Tooltip("How quickly the effect smoothes out and returns to baseline.")]
    public float smoothingSpeed = 5.0f;
    [Tooltip("The color of the orb when silent.")]
    public Color silentColor = Color.blue;
    [Tooltip("The color of the orb when speaking.")]
    public Color speakingColor = Color.cyan;
    
    // --- Internal Data ---
    private float[] audioSamples;
    private float[] spectrumData;
    private float smoothedAmplitude = 0f;
    
    void Start()
    {
        // We need audio data to analyze. 1024 samples is a good balance.
        audioSamples = new float[1024];
        spectrumData = new float[512]; // FFT data

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Set initial "silent" state
        vfxGraph.SetVector4("Color", silentColor);
    }

    void Update()
    {
        // 1. Get the live audio data from the AudioSource
        // GetOutputData is for listening to what's currently playing
        audioSource.GetOutputData(audioSamples, 0);

        // 2. Calculate the current volume (RMS amplitude)
        float currentAmplitude = 0;
        foreach (float sample in audioSamples)
        {
            currentAmplitude += Mathf.Abs(sample);
        }
        currentAmplitude /= audioSamples.Length;
        currentAmplitude *= amplitudeMultiplier;

        // 3. Smooth the amplitude value for a more organic feel
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, currentAmplitude, Time.deltaTime * smoothingSpeed);

        // --- Apply the analysis to the VFX Graph ---

        // A. Control Size/Intensity with Amplitude
        // Assumes you have exposed properties named "SphereRadius" and "TurbulenceIntensity"
        vfxGraph.SetFloat("Size", 1.0f + smoothedAmplitude * 0.5f); // Start at radius 1, expand from there
        vfxGraph.SetFloat("TurbulenceIntensity", 5.0f + smoothedAmplitude * 20.0f); // Base intensity 5, more chaos on speech

        // B. Control Color with Amplitude
        // Lerp between two colors based on how loud the sound is
        Color voiceColor = Color.Lerp(silentColor, speakingColor, smoothedAmplitude);
        vfxGraph.SetVector4("Color", voiceColor);
        
        // C. (Advanced) Control another property with Frequency
        // GetSpectrumData gives you the frequency bands (bass, mids, treble)
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
        
        // Let's check the "treble" range (e.g., the last quarter of the spectrum data)
        float trebleEnergy = 0;
        int trebleStart = spectrumData.Length / 4 * 3;
        for (int i = trebleStart; i < spectrumData.Length; i++)
        {
            trebleEnergy += spectrumData[i];
        }
        // Use treble to control the lifetime of the electric trails
        vfxGraph.SetFloat("TrailLifetime", 0.5f + trebleEnergy * 10f);
    }
}