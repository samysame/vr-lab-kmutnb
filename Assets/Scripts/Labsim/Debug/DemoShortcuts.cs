using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Keyboard shortcuts to drive the bench quickly during a demo or a test session.
    /// F1 power both | F2 link CH1->CH1 | F3 link CH2->CH2 | F4 toggle output CH1 | F5 AUTO
    /// F6 next step | F7 restart scenario | F8 unplug all cables
    /// </summary>
    public class DemoShortcuts : MonoBehaviour
    {
        public FunctionGeneratorController generator;
        public OscilloscopeController scope;
        public ScenarioManager scenarios;

        void Update()
        {
            if (generator == null || scope == null) return;
            CableManager cables = CableManager.Instance;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                bool on = !(generator.IsPowered && scope.IsPowered);
                generator.Machine.SetPower(on);
                scope.Machine.SetPower(on);
            }
            if (Input.GetKeyDown(KeyCode.F2) && cables != null)
                cables.Connect(generator.outputConnectors[0], scope.inputConnectors[0]);
            if (Input.GetKeyDown(KeyCode.F3) && cables != null)
                cables.Connect(generator.outputConnectors[1], scope.inputConnectors[1]);
            if (Input.GetKeyDown(KeyCode.F4)) generator.Execute(GeneratorAction.ToggleOutput1);
            if (Input.GetKeyDown(KeyCode.F5)) scope.Execute(ScopeAction.AutoScale);
            if (Input.GetKeyDown(KeyCode.F6) && scenarios != null) scenarios.NextStep();
            if (Input.GetKeyDown(KeyCode.F7) && scenarios != null) scenarios.Restart();
            if (Input.GetKeyDown(KeyCode.F8) && cables != null) cables.DisconnectAll();
        }
    }
}
