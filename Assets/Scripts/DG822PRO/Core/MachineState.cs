using UnityEngine;
using System;

public class MachineState : MonoBehaviour
{
    public static MachineState Instance { get; private set; }

    public bool IsPoweredOn { get; private set; } = false;

    // Event every button can subscribe to
    public event Action<bool> OnPowerChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetPower(bool state)
    {
        IsPoweredOn = state;
        OnPowerChanged?.Invoke(IsPoweredOn);
        Debug.Log("Machine power: " + IsPoweredOn);
    }

    public void TogglePower()
    {
        SetPower(!IsPoweredOn);
    }
}