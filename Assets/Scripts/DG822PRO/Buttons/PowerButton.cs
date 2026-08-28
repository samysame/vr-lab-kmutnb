using UnityEngine;

public class PowerButton : LitButton
{
    protected override void Start()
    {
        emissionMaterialInstance = emissionRenderer.materials[emissionSlotIndex];

        // Ce qui manquait : récupérer la couleur de base du matériau Blender
        baseEmissionColor = emissionMaterialInstance.GetColor("_EmissionColor");
        baseEmissionColor = NormalizeColor(baseEmissionColor);

        SetState(MachineState.Instance != null && MachineState.Instance.IsPoweredOn);
    }

    void OnMouseDown()
    {
        MachineState.Instance.TogglePower();
        SetState(MachineState.Instance.IsPoweredOn);
    }
}