using System;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Button with a backlight driven by the emission of its material (base color comes from Blender).
    /// - toggleOnClick = true: the button toggles itself (stand-alone behaviour).
    /// - toggleOnClick = false: a controller decides the light state (radio groups, output keys...).
    ///   Controllers set it to false automatically when they hook the button.
    /// </summary>
    public class LitButton : MonoBehaviour
    {
        [Header("Identifier")]
        public string buttonId;

        [Header("Emissive material")]
        public Renderer emissionRenderer;
        public int emissionSlotIndex = 0;

        [Header("Emission intensity")]
        [Tooltip("The base color comes from the Blender material; only its intensity is multiplied here.")]
        public float emissionIntensityOn = 2f;
        public float emissionIntensityOff = 0f;

        [Header("Behaviour")]
        public bool toggleOnClick = true;

        protected bool isOn = false;
        protected MachineState machine;
        protected Material emissionMaterialInstance;
        protected Color baseEmissionColor;

        /// <summary>Raised when the light state changes through SetState (not SetStateWithoutNotify).</summary>
        public event Action<LitButton, bool> OnStateChanged;

        /// <summary>Raised on every accepted click, before any toggle.</summary>
        public event Action<LitButton> OnClicked;

        public bool IsOn { get { return isOn; } }

        protected virtual void Awake()
        {
            machine = MachineState.Of(this);
            InitMaterial();
            ApplyVisual();
        }

        protected virtual void OnEnable()
        {
            if (machine != null) machine.OnPowerChanged += HandlePowerChanged;
        }

        protected virtual void OnDisable()
        {
            if (machine != null) machine.OnPowerChanged -= HandlePowerChanged;
        }

        void InitMaterial()
        {
            if (emissionRenderer == null) emissionRenderer = GetComponent<Renderer>();
            if (emissionRenderer == null)
            {
                Debug.LogWarning("[" + buttonId + "] No emission renderer assigned.");
                return;
            }

            Material[] mats = emissionRenderer.materials; // creates per-object instances
            if (emissionSlotIndex < 0 || emissionSlotIndex >= mats.Length)
            {
                Debug.LogWarning("[" + buttonId + "] Invalid emission slot index.");
                return;
            }

            emissionMaterialInstance = mats[emissionSlotIndex];
            emissionMaterialInstance.EnableKeyword("_EMISSION");
            baseEmissionColor = emissionMaterialInstance.HasProperty("_EmissionColor")
                ? NormalizeColor(emissionMaterialInstance.GetColor("_EmissionColor"))
                : Color.white;
        }

        protected virtual void HandlePowerChanged(bool powered)
        {
            if (!powered) SetState(false);
        }

        protected virtual void OnMouseDown()
        {
            if (machine != null && !machine.IsPoweredOn)
            {
                Debug.Log("[" + buttonId + "] machine is off, click ignored.");
                return;
            }

            if (OnClicked != null) OnClicked(this);
            if (toggleOnClick) Toggle();
        }

        public void Toggle()
        {
            SetState(!isOn);
        }

        public virtual void SetState(bool state)
        {
            ApplyState(state, true);
        }

        /// <summary>Updates the light without raising events (used by controllers).</summary>
        public void SetStateWithoutNotify(bool state)
        {
            ApplyState(state, false);
        }

        void ApplyState(bool state, bool notify)
        {
            isOn = state;
            ApplyVisual();
            if (!notify) return;
            TriggerAction();
            if (OnStateChanged != null) OnStateChanged(this, isOn);
        }

        void ApplyVisual()
        {
            if (emissionMaterialInstance == null) return;
            float intensity = isOn ? emissionIntensityOn : emissionIntensityOff;
            emissionMaterialInstance.SetColor("_EmissionColor", baseEmissionColor * intensity);
        }

        /// <summary>Hook for subclasses. Default: debug log only.</summary>
        protected virtual void TriggerAction()
        {
            Debug.Log("[" + buttonId + "] state: " + isOn);
        }

        protected Color NormalizeColor(Color c)
        {
            float max = Mathf.Max(c.r, c.g, c.b);
            if (max <= 0f) return Color.white; // fallback when the exported material has no emission color
            return new Color(c.r / max, c.g / max, c.b / max, 1f);
        }
    }
}
