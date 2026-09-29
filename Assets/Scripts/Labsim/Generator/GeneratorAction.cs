using System;

namespace LabSim
{
    /// <summary>Every action a DG822 Pro key can trigger. A key whose buttonId equals the enum name works without binding.</summary>
    public enum GeneratorAction
    {
        None,
        SelectChannel1, SelectChannel2, ToggleChannel,
        ToggleOutput1, ToggleOutput2,
        WaveSine, WaveSquare, WaveRamp, WavePulse, WaveNoise, WaveDC,
        SelectFrequency, SelectAmplitude, SelectOffset, SelectPhase, SelectDutyCycle, SelectSymmetry, NextParameter,
        CursorLeft, CursorRight, ValueUp, ValueDown,
        Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
        DecimalPoint, Sign, Backspace, CancelEntry,
        EnterBase, EnterKilo, EnterMega, EnterMilli, EnterMicro,
        ToggleLoad, Preset
    }

    /// <summary>Inspector mapping between a modeled key (buttonId) and an action.</summary>
    [Serializable]
    public class GeneratorBinding
    {
        public string controlId;
        public GeneratorAction action;

        public GeneratorBinding(string id, GeneratorAction a)
        {
            controlId = id;
            action = a;
        }
    }
}
