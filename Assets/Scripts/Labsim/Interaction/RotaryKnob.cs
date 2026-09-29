using System;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Incremental rotary encoder (no end stops), like the knobs of the DG822 Pro and DHO814.
    /// Mouse: wheel = turn, left click = +1 step (clockwise), right click = -1 step, middle click = push.
    /// Needs a Collider.
    /// </summary>
    public class RotaryKnob : MonoBehaviour
    {
        [Tooltip("Identifier matched by the controller bindings (e.g. VerticalScale).")]
        public string knobId;

        [Header("Visual rotation")]
        public Vector3 localRotationAxis = Vector3.forward;
        public float degreesPerStep = 15f;
        [Tooltip("Invert if the knob turns the wrong way on screen.")]
        public bool invertDirection = false;

        /// <summary>steps &gt; 0 means clockwise.</summary>
        public event Action<RotaryKnob, int> OnTurned;
        public event Action<RotaryKnob> OnPushed;

        MachineState machine;

        void Awake()
        {
            machine = MachineState.Of(this);
        }

        void OnMouseOver()
        {
            int steps = 0;
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.01f) steps += 1;
            else if (scroll < -0.01f) steps -= 1;
            if (Input.GetMouseButtonDown(0)) steps += 1;
            if (Input.GetMouseButtonDown(1)) steps -= 1;

            if (steps != 0) Turn(steps);
            if (Input.GetMouseButtonDown(2)) Push();
        }

        bool Powered()
        {
            return machine == null || machine.IsPoweredOn;
        }

        public void Turn(int steps)
        {
            if (!Powered() || steps == 0) return;
            if (invertDirection) steps = -steps;
            transform.Rotate(localRotationAxis, -steps * degreesPerStep, Space.Self);
            if (OnTurned != null) OnTurned(this, steps);
        }

        public void Push()
        {
            if (!Powered()) return;
            if (OnPushed != null) OnPushed(this);
        }
    }
}
