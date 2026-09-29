using System;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Momentary push button (no light). Needs a Collider on the same GameObject.
    /// The owning controller subscribes to OnPressed and maps buttonId to an action.
    /// </summary>
    public class SimpleButton : MonoBehaviour
    {
        [Tooltip("Identifier matched by the controller bindings (e.g. WaveSine, AutoScale).")]
        public string buttonId;

        [Header("Optional press animation")]
        [Tooltip("Local direction in which the cap moves when pressed.")]
        public Vector3 pressLocalAxis = new Vector3(0, 0, -1);
        [Tooltip("Travel in local units. 0 disables the animation.")]
        public float pressDepth = 0f;
        public float pressDuration = 0.08f;

        public event Action<SimpleButton> OnPressed;

        MachineState machine;
        Vector3 restLocalPosition;
        float releaseAt = -1f;

        void Awake()
        {
            machine = MachineState.Of(this);
            restLocalPosition = transform.localPosition;
            if (machine == null)
                Debug.LogWarning("[" + buttonId + "] No MachineState found in parents.");
        }

        void OnMouseDown()
        {
            Press();
        }

        /// <summary>Public entry point, also usable from VR interactors or tests.</summary>
        public void Press()
        {
            if (machine != null && !machine.IsPoweredOn)
            {
                Debug.Log("[" + buttonId + "] machine is off, press ignored.");
                return;
            }

            if (pressDepth > 0f)
            {
                transform.localPosition = restLocalPosition + pressLocalAxis.normalized * pressDepth;
                releaseAt = Time.time + pressDuration;
            }

            if (OnPressed != null) OnPressed(this);
        }

        void Update()
        {
            if (releaseAt > 0f && Time.time >= releaseAt)
            {
                transform.localPosition = restLocalPosition;
                releaseAt = -1f;
            }
        }
    }
}
