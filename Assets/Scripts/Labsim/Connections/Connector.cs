using System.Collections.Generic;
using UnityEngine;

namespace LabSim
{
    public enum ConnectorRole
    {
        Output,
        Input
    }

    /// <summary>
    /// BNC socket on an instrument. Place it on the BNC mesh (with a Collider).
    /// The instrument controller fills Source (outputs) or Sink (inputs) at Awake.
    /// </summary>
    public class Connector : MonoBehaviour
    {
        [Tooltip("Short id, unique inside the machine, e.g. OUT1 or CH1.")]
        public string connectorId = "CH1";
        public ConnectorRole role = ConnectorRole.Output;

        [Tooltip("Where the cable end attaches. Its local axis given below must point out of the panel.")]
        public Transform plugPoint;
        public Vector3 localOutwardAxis = Vector3.forward;

        static readonly List<Connector> all = new List<Connector>();
        public static IList<Connector> All { get { return all; } }

        public ISignalSource Source { get; set; }
        public ISignalSink Sink { get; set; }
        public Cable Cable { get; internal set; }
        public MachineState Machine { get; private set; }

        public bool IsConnected { get { return Cable != null; } }
        public Connector Peer { get { return Cable == null ? null : Cable.OtherEnd(this); } }

        public string DisplayName
        {
            get { return (Machine != null ? Machine.machineId : "?") + "/" + connectorId; }
        }

        public Vector3 PlugPosition
        {
            get { return plugPoint != null ? plugPoint.position : transform.position; }
        }

        public Vector3 PlugDirection
        {
            get
            {
                Transform t = plugPoint != null ? plugPoint : transform;
                return t.TransformDirection(localOutwardAxis).normalized;
            }
        }

        void Awake()
        {
            Machine = MachineState.Of(this);
            if (GetComponent<Collider>() == null)
                Debug.LogWarning("[Connector " + name + "] needs a Collider to be clickable.");
        }

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        void OnMouseDown()
        {
            if (CableManager.Instance != null) CableManager.Instance.HandleConnectorClicked(this);
            else Debug.LogWarning("No CableManager in the scene.");
        }

        /// <summary>
        /// Voltage seen at an INPUT connector at time t: open-circuit voltage of the peer source
        /// divided between the source output impedance and this input impedance.
        /// Returns 0 V when floating or when the source output is off.
        /// </summary>
        public double ReadVoltage(double t)
        {
            if (role != ConnectorRole.Input) return 0.0;
            Connector peer = Peer;
            if (peer == null || peer.Source == null || !peer.Source.IsOutputActive) return 0.0;

            double vOpen = peer.Source.SampleOpenCircuit(t);
            double rLoad = Sink != null ? Sink.InputImpedance : 1e6;
            return SignalMath.Divider(vOpen, peer.Source.OutputImpedance, rLoad);
        }

        public static Connector Find(string machineId, string connectorId)
        {
            for (int i = 0; i < all.Count; i++)
            {
                Connector c = all[i];
                if (c.connectorId == connectorId && c.Machine != null && c.Machine.machineId == machineId) return c;
            }
            return null;
        }
    }
}
