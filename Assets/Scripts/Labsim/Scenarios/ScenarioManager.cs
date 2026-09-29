using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Runs the scenarios of ScenarioLibrary: applies the setup, evaluates the current step
    /// every frame and moves to the next one when its condition holds for 'holdSeconds'.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        public FunctionGeneratorController generator;
        public OscilloscopeController scope;
        public CableManager cables;

        public bool autoStart = true;
        public int startScenarioIndex = 0;

        public IList<Scenario> Scenarios { get { return scenarios; } }
        public Scenario Current { get { return scenarioIndex >= 0 ? scenarios[scenarioIndex] : null; } }
        public int ScenarioIndex { get { return scenarioIndex; } }
        public int StepIndex { get; private set; }
        public bool IsCompleted { get; private set; }
        public ScenarioContext Context { get; private set; }
        public float HoldProgress { get; private set; }

        public ScenarioStep CurrentStep
        {
            get
            {
                Scenario s = Current;
                if (s == null || IsCompleted || StepIndex >= s.steps.Count) return null;
                return s.steps[StepIndex];
            }
        }

        /// <summary>Raised when the scenario or the step changes (UI refresh).</summary>
        public event Action OnProgressChanged;

        List<Scenario> scenarios = new List<Scenario>();
        int scenarioIndex = -1;
        float holdTimer;

        void Start()
        {
            if (cables == null) cables = CableManager.Instance;
            if (generator == null || scope == null || cables == null)
            {
                Debug.LogError("[ScenarioManager] Assign generator, scope and cable manager.");
                enabled = false;
                return;
            }
            Context = new ScenarioContext(generator, scope, cables);
            scenarios = ScenarioLibrary.Build();
            if (autoStart) StartScenario(startScenarioIndex);
        }

        void Update()
        {
            ScenarioStep step = CurrentStep;
            if (step == null) return;

            bool ok = false;
            try { ok = step.isComplete != null && step.isComplete(Context); }
            catch (Exception e) { Debug.LogWarning("[Scenario] Condition error: " + e.Message); }

            holdTimer = ok ? holdTimer + Time.deltaTime : 0f;
            HoldProgress = step.holdSeconds > 0 ? Mathf.Clamp01(holdTimer / step.holdSeconds) : (ok ? 1f : 0f);
            if (ok && holdTimer >= step.holdSeconds) GoToStep(StepIndex + 1);
        }

        public void StartScenario(int index)
        {
            if (scenarios.Count == 0) return;
            scenarioIndex = Mathf.Clamp(index, 0, scenarios.Count - 1);
            Scenario s = scenarios[scenarioIndex];
            Debug.Log("[Scenario] Start: " + s.title);
            if (s.setup != null) s.setup(Context);
            GoToStep(0);
        }

        public void Restart() { StartScenario(scenarioIndex); }
        public void NextStep() { if (Current != null) GoToStep(StepIndex + 1); }
        public void PreviousStep() { if (Current != null) GoToStep(Mathf.Max(0, StepIndex - 1)); }

        void GoToStep(int index)
        {
            Scenario s = Current;
            holdTimer = 0f;
            HoldProgress = 0f;
            StepIndex = Mathf.Clamp(index, 0, s.steps.Count);
            IsCompleted = StepIndex >= s.steps.Count;

            ScenarioStep step = CurrentStep;
            if (step != null && step.onEnter != null) step.onEnter(Context);
            if (IsCompleted) Debug.Log("[Scenario] Completed: " + s.title);
            if (OnProgressChanged != null) OnProgressChanged();
        }

        /// <summary>Live text of the current step, or the summary once completed.</summary>
        public string LiveText()
        {
            try
            {
                if (IsCompleted && Current != null && Current.summary != null) return Current.summary(Context);
                ScenarioStep step = CurrentStep;
                if (step != null && step.feedback != null) return step.feedback(Context);
            }
            catch (Exception e) { return "(" + e.Message + ")"; }
            return "";
        }
    }
}
