using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Power state of ONE instrument. Put exactly one on the root object of each machine
    /// (DG822 Pro root, DHO814 root). Buttons, knobs and connectors find it with GetComponentInParent.
    /// This replaces the former global singleton, which could not handle two machines.
    /// </summary>
    [DisallowMultipleComponent]
    public class MachineState : MonoBehaviour
    {
        [Tooltip("Unique id used by scenarios and logs, e.g. DG822Pro or DHO814.")]
        public string machineId = "Machine";

        [Tooltip("Initial power state when the scene starts.")]
        public bool startPoweredOn = false;

        public bool IsPoweredOn { get; private set; }

        /// <summary>Raised on every power change (true = ON).</summary>
        public event Action<bool> OnPowerChanged;

        static readonly Dictionary<string, MachineState> registry = new Dictionary<string, MachineState>();

        /// <summary>Returns the machine registered with this id, or null.</summary>
        public static MachineState Find(string id)
        {
            MachineState m;
            registry.TryGetValue(id, out m);
            return m;
        }

        public static IEnumerable<MachineState> All { get { return registry.Values; } }

        /// <summary>Returns the machine owning a component (walks up the hierarchy).</summary>
        public static MachineState Of(Component c)
        {
            return c == null ? null : c.GetComponentInParent<MachineState>();
        }

        void Awake()
        {
            MachineState existing;
            if (registry.TryGetValue(machineId, out existing) && existing != null && existing != this)
                Debug.LogWarning("[MachineState] Duplicate machineId '" + machineId + "'. Use unique ids.");
            registry[machineId] = this;
            IsPoweredOn = startPoweredOn;
        }

        void OnDestroy()
        {
            MachineState m;
            if (registry.TryGetValue(machineId, out m) && m == this) registry.Remove(machineId);
        }

        public void SetPower(bool state)
        {
            if (IsPoweredOn == state) return;
            IsPoweredOn = state;
            Debug.Log("[" + machineId + "] power " + (state ? "ON" : "OFF"));
            if (OnPowerChanged != null) OnPowerChanged(state);
        }

        public void TogglePower()
        {
            SetPower(!IsPoweredOn);
        }
    }
}
