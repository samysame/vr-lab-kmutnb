using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Two-click cable system (same pattern as the Unreal POC):
    /// click 1 selects a connector (a preview line follows the mouse), click 2 on a compatible
    /// connector creates the cable. Clicking a plugged connector unplugs its cable.
    /// Right click or Escape cancels. One instance per scene.
    /// </summary>
    public class CableManager : MonoBehaviour
    {
        public static CableManager Instance { get; private set; }

        [Header("Cable look")]
        public Material cableMaterial;
        public Color cableColor = new Color(0.08f, 0.08f, 0.08f);
        public float cableWidth = 0.006f;
        public Color previewColor = new Color(1f, 0.8f, 0.1f);

        [Header("Rules")]
        [Tooltip("If false, only Output <-> Input links are allowed.")]
        public bool allowSameRoleConnection = false;

        public event Action<Cable> OnCableConnected;
        public event Action<Connector, Connector> OnCableDisconnected;
        /// <summary>Human-readable feedback for the UI ("Select an input", "Connector busy"...).</summary>
        public event Action<string> OnMessage;

        readonly List<Cable> cables = new List<Cable>();
        public IList<Cable> Cables { get { return cables; } }
        public Connector Pending { get; private set; }

        LineRenderer preview;
        Vector3[] previewPoints = new Vector3[24];

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (cableMaterial == null) cableMaterial = CreateFallbackMaterial();
            preview = CreatePreviewLine();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static Material CreateFallbackMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            return new Material(s);
        }

        LineRenderer CreatePreviewLine()
        {
            GameObject go = new GameObject("CablePreview");
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = previewPoints.Length;
            lr.widthMultiplier = cableWidth * 0.8f;
            Material m = new Material(cableMaterial);
            m.color = previewColor;
            lr.sharedMaterial = m;
            lr.startColor = previewColor;
            lr.endColor = previewColor;
            lr.enabled = false;
            return lr;
        }

        void Update()
        {
            if (Pending == null) return;

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPending();
                return;
            }
            UpdatePreview();
        }

        void UpdatePreview()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Mouse position projected on a plane facing the camera through the pending plug.
            Plane plane = new Plane(-cam.transform.forward, Pending.PlugPosition);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            float enter;
            if (!plane.Raycast(ray, out enter)) return;
            Vector3 end = ray.GetPoint(enter);

            Cable.BuildCurve(Pending.PlugPosition, Pending.PlugDirection, end, Vector3.zero, 0.15f, 0.03f, previewPoints);
            preview.SetPositions(previewPoints);
        }

        /// <summary>Entry point called by Connector.OnMouseDown.</summary>
        public void HandleConnectorClicked(Connector c)
        {
            if (Pending == null)
            {
                if (c.IsConnected)
                {
                    Disconnect(c.Cable);
                    return;
                }
                Pending = c;
                preview.enabled = true;
                UpdatePreview();
                Say("Câble : " + c.DisplayName + " sélectionné, cliquez sur " +
                    (c.role == ConnectorRole.Output ? "une entrée." : "une sortie."));
                return;
            }

            if (c == Pending)
            {
                CancelPending();
                return;
            }
            if (c.IsConnected)
            {
                Say(c.DisplayName + " est déjà occupé.");
                return;
            }
            if (!allowSameRoleConnection && c.role == Pending.role)
            {
                Say("Reliez une sortie à une entrée.");
                return;
            }

            Connect(Pending, c);
            CancelPending();
        }

        public void CancelPending()
        {
            Pending = null;
            if (preview != null) preview.enabled = false;
        }

        /// <summary>Creates a cable between two free connectors (also used by scenarios).</summary>
        public Cable Connect(Connector a, Connector b)
        {
            if (a == null || b == null || a == b) return null;
            if (a.IsConnected) Disconnect(a.Cable);
            if (b.IsConnected) Disconnect(b.Cable);

            GameObject go = new GameObject("Cable_" + a.DisplayName + "_" + b.DisplayName);
            go.transform.SetParent(transform, false);
            go.AddComponent<LineRenderer>();
            Cable cable = go.AddComponent<Cable>();
            cable.Init(a, b, cableMaterial, cableColor, cableWidth);

            a.Cable = cable;
            b.Cable = cable;
            cables.Add(cable);

            Say("Connecté : " + a.DisplayName + " -> " + b.DisplayName);
            if (OnCableConnected != null) OnCableConnected(cable);
            return cable;
        }

        public void Disconnect(Cable cable)
        {
            if (cable == null) return;
            Connector a = cable.A;
            Connector b = cable.B;
            cables.Remove(cable);
            cable.Detach();
            Say("Déconnecté : " + (a != null ? a.DisplayName : "?") + " / " + (b != null ? b.DisplayName : "?"));
            if (OnCableDisconnected != null) OnCableDisconnected(a, b);
        }

        public void DisconnectAll()
        {
            for (int i = cables.Count - 1; i >= 0; i--) Disconnect(cables[i]);
            CancelPending();
        }

        public static bool AreLinked(Connector a, Connector b)
        {
            return a != null && b != null && a.Cable != null && a.Cable == b.Cable;
        }

        void Say(string msg)
        {
            Debug.Log("[Cables] " + msg);
            if (OnMessage != null) OnMessage(msg);
        }
    }
}
