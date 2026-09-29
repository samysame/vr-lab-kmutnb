using UnityEngine;

namespace LabSim
{
    /// <summary>Power key of an instrument. Its light always mirrors the MachineState.</summary>
    public class PowerButton : LitButton
    {
        protected override void Awake()
        {
            toggleOnClick = false;
            base.Awake();
        }

        void Start()
        {
            SetStateWithoutNotify(machine != null && machine.IsPoweredOn);
        }

        protected override void OnMouseDown()
        {
            if (machine == null)
            {
                Debug.LogWarning("[" + buttonId + "] PowerButton without MachineState in parents.");
                return;
            }
            machine.TogglePower();
        }

        protected override void HandlePowerChanged(bool powered)
        {
            SetStateWithoutNotify(powered);
        }
    }
}
