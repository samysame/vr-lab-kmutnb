using System;
using UnityEngine;

namespace LabSim
{
    public enum Coupling { DC, AC, GND }
    public enum TriggerSlope { Rising, Falling }
    public enum TriggerMode { Auto, Normal, Single }
    public enum ScopeRunState { Running, Stopped, SingleArmed }

    /// <summary>
    /// Characteristics of the Rigol DHO814 used by the simulation.
    /// NOTE: approximate values for the prototype, to be checked against the official datasheet.
    /// </summary>
    public static class ScopeSpecs
    {
        public const int ChannelCount = 4;
        public const int HorizontalDivisions = 10;
        public const int VerticalDivisions = 8;
        public const int AdcBits = 12;
        /// <summary>The ADC full scale covers 10 divisions (screen = 8 div), so clipping happens just off-screen.</summary>
        public const double AdcRangeDivisions = 10.0;
        public const double InputImpedance = 1e6;      // ohms
        public const double BandwidthHz = 100e6;
        public const double MinVoltsPerDiv = 500e-6;
        public const double MaxVoltsPerDiv = 10.0;
        public const double MinTimePerDiv = 5e-9;
        public const double MaxTimePerDiv = 1000.0;

        public static readonly Color32[] ChannelColors =
        {
            new Color32(255, 215, 0, 255),   // CH1 yellow
            new Color32(0, 230, 255, 255),   // CH2 cyan
            new Color32(255, 70, 255, 255),  // CH3 magenta
            new Color32(70, 130, 255, 255)   // CH4 blue
        };

        public static string ColorHex(int ch)
        {
            Color32 c = ChannelColors[ch];
            return "#" + c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
        }
    }

    /// <summary>Vertical settings of one input channel. Acts as the 1 Mohm load seen by the cable.</summary>
    [Serializable]
    public class ScopeChannel : ISignalSink
    {
        public bool enabled = false;
        public double voltsPerDiv = 1.0;
        [Tooltip("Vertical position of the ground marker, in divisions from the center.")]
        public double positionDiv = 0.0;
        public Coupling coupling = Coupling.DC;
        [Tooltip("Probe ratio setting (1X for a BNC cable, 10X for a 10:1 probe).")]
        public double probeRatio = 1.0;
        public bool invert = false;

        [NonSerialized] public int index;
        [NonSerialized] public Connector input;

        public double InputImpedance { get { return ScopeSpecs.InputImpedance; } }
        public string SinkName { get { return "DHO814 CH" + (index + 1); } }
    }

    [Serializable]
    public class Timebase
    {
        public double secondsPerDiv = 500e-6;
        [Tooltip("Horizontal delay: time between the trigger point and the screen center (s).")]
        public double delaySeconds = 0.0;

        public double Window { get { return secondsPerDiv * ScopeSpecs.HorizontalDivisions; } }
    }

    [Serializable]
    public class TriggerSettings
    {
        public int sourceChannel = 0;
        public double level = 0.0;
        public TriggerSlope slope = TriggerSlope.Rising;
        public TriggerMode mode = TriggerMode.Auto;
        [Tooltip("Hysteresis in divisions of the source channel, rejects noise.")]
        public double hysteresisDiv = 0.1;
    }

    /// <summary>One screen record: samples of every enabled channel, in volts and in screen units.</summary>
    public class AcquisitionResult
    {
        public readonly int sampleCount;
        public readonly float[][] volts;
        public readonly float[][] screenY;
        public readonly bool[] channelValid;
        public readonly bool[] clipped;
        public double dt;
        public double firstSampleTime;
        public double triggerTime;
        public bool triggered;

        public AcquisitionResult(int channels, int samples)
        {
            sampleCount = samples;
            volts = new float[channels][];
            screenY = new float[channels][];
            channelValid = new bool[channels];
            clipped = new bool[channels];
            for (int c = 0; c < channels; c++)
            {
                volts[c] = new float[samples];
                screenY[c] = new float[samples];
            }
        }
    }
}
