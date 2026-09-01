using UnityEngine;

/// <summary>
/// Controls the machine's screen (on/off) based on the state
/// broadcast by MachineState.OnPowerChanged. Follows the same logic as LitButton
/// (emissive material, color normalization) to stay consistent.
/// </summary>
public class ScreenManager : MonoBehaviour
{
    [Header("References to the screen's emissive material")]
    public Renderer screenRenderer;
    public int emissionSlotIndex = 0;

    [Header("Emission intensity (backlight)")]
    public float emissionIntensityOn = 1.5f;
    public float emissionIntensityOff = 0f;

    protected Material screenMaterialInstance;
    protected Color baseEmissionColor;

    protected bool isOn = false;
    public bool IsOn => isOn;

    void Start()
    {
        screenMaterialInstance = screenRenderer.materials[emissionSlotIndex];

        // Get the base color defined on the material (Blender/Unity)
        baseEmissionColor = screenMaterialInstance.GetColor("_EmissionColor");
        baseEmissionColor = NormalizeColor(baseEmissionColor);

        if (MachineState.Instance != null)
        {
            MachineState.Instance.OnPowerChanged += HandlePowerChanged;
            // Apply the current state in case the machine is already powered on
            SetScreenState(MachineState.Instance.IsPoweredOn);
        }
        else
        {
            SetScreenState(false);
        }
    }

    void OnDestroy()
    {
        if (MachineState.Instance != null)
            MachineState.Instance.OnPowerChanged -= HandlePowerChanged;
    }

    void HandlePowerChanged(bool powerState)
    {
        SetScreenState(powerState);
    }

    public void SetScreenState(bool state)
    {
        isOn = state;
        float intensity = isOn ? emissionIntensityOn : emissionIntensityOff;
        screenMaterialInstance.SetColor("_EmissionColor", baseEmissionColor * intensity);

        // Some shaders (Standard, URP/Lit) only apply _EmissionColor if the
        // _EMISSION keyword is active on the material instance.
        if (isOn)
            screenMaterialInstance.EnableKeyword("_EMISSION");
        else
            screenMaterialInstance.DisableKeyword("_EMISSION");

        Debug.Log("Screen: " + (isOn ? "on" : "off"));
    }

    protected Color NormalizeColor(Color c)
    {
        float max = Mathf.Max(c.r, c.g, c.b);
        if (max <= 0f) return Color.white;
        return new Color(c.r / max, c.g / max, c.b / max, 1f);
    }
}