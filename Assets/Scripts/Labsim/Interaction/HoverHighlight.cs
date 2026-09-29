using System;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Mouse-over feedback: slight scale-up and a label broadcast to the UI
    /// (the scenario panel shows it). Needs a Collider.
    /// </summary>
    public class HoverHighlight : MonoBehaviour
    {
        [Tooltip("Text shown in the UI while hovering, e.g. 'DG822 Pro - sortie CH1'.")]
        public string label;
        public float hoverScale = 1.08f;

        /// <summary>Raised with the label on enter and with null on exit.</summary>
        public static event Action<string> OnHoverChanged;

        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void OnMouseEnter()
        {
            transform.localScale = baseScale * hoverScale;
            if (OnHoverChanged != null) OnHoverChanged(string.IsNullOrEmpty(label) ? name : label);
        }

        void OnMouseExit()
        {
            transform.localScale = baseScale;
            if (OnHoverChanged != null) OnHoverChanged(null);
        }

        void OnDisable()
        {
            transform.localScale = baseScale;
        }
    }
}
