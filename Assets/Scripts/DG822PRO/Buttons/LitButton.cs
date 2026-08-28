using UnityEngine;
using System;

public class LitButton : MonoBehaviour
{
    [Header("Identifiant")]
    public string buttonId;

    [Header("Références au materiau emissif")]
    public Renderer emissionRenderer;
    public int emissionSlotIndex = 0;

    [Header("Intensité de l'émission")]
    [Tooltip("La couleur de base vient du matériau Blender. On ne fait que multiplier son intensité ici.")]
    public float emissionIntensityOn = 2f;
    public float emissionIntensityOff = 0f;

    protected bool isOn = false;
    protected Material emissionMaterialInstance;
    protected Color baseEmissionColor; // récupérée depuis le matériau d'origine (créé dans Blender)

    // Événement pour brancher une action externe (écran, paramètre...) sans coupler le code
    public event Action<LitButton, bool> OnStateChanged;

    protected virtual void Start()
    {
        emissionMaterialInstance = emissionRenderer.materials[emissionSlotIndex];

        // On récupère la couleur telle que définie dans le matériau Blender/Unity
        baseEmissionColor = emissionMaterialInstance.GetColor("_EmissionColor");
        // On la normalise (au cas où Blender a déjà exporté une valeur avec intensité incluse)
        baseEmissionColor = NormalizeColor(baseEmissionColor);

        if (MachineState.Instance != null)
            MachineState.Instance.OnPowerChanged += HandlePowerChanged;

        SetState(false);
    }

    void OnDestroy()
    {
        if (MachineState.Instance != null)
            MachineState.Instance.OnPowerChanged -= HandlePowerChanged;
    }

    void HandlePowerChanged(bool powerState)
    {
        if (!powerState) SetState(false);
    }

    void OnMouseDown()
    {
        if (MachineState.Instance == null || !MachineState.Instance.IsPoweredOn)
        {
            Debug.Log(buttonId + " : machine éteinte, action ignorée.");
            return;
        }

        Toggle();
    }

    public void Toggle() => SetState(!isOn);

    public virtual void SetState(bool state)
    {
        isOn = state;
        float intensity = isOn ? emissionIntensityOn : emissionIntensityOff;
        emissionMaterialInstance.SetColor("_EmissionColor", baseEmissionColor * intensity);

        // Déclenche l'action associée (mode, paramètre écran, etc.)
        TriggerAction();

        // Notifie tout système externe abonné (ex: ScreenManager)
        OnStateChanged?.Invoke(this, isOn);
    }

    protected virtual void TriggerAction()
    {
        // À compléter plus tard selon le bouton :
        // ex. if (isOn) ScreenManager.Instance.SetMode(buttonId);
        Debug.Log(buttonId + " action déclenchée, état: " + isOn);
    }

    protected Color NormalizeColor(Color c)
    {
        float max = Mathf.Max(c.r, c.g, c.b);
        if (max <= 0f) return Color.white; // fallback si le matériau exporté n'a pas de couleur définie
        return new Color(c.r / max, c.g / max, c.b / max, 1f);
    }

    public bool IsOn => isOn;
}