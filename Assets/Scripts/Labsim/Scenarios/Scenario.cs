using System;
using System.Collections.Generic;

namespace LabSim
{
    /// <summary>Everything a scenario step may read or modify.</summary>
    public class ScenarioContext
    {
        public readonly FunctionGeneratorController generator;
        public readonly OscilloscopeController scope;
        public readonly CableManager cables;

        public ScenarioContext(FunctionGeneratorController g, OscilloscopeController s, CableManager c)
        {
            generator = g;
            scope = s;
            cables = c;
        }

        public bool GeneratorOn { get { return generator.IsPowered; } }
        public bool ScopeOn { get { return scope.IsPowered; } }
        public bool BothOn { get { return GeneratorOn && ScopeOn; } }

        public GeneratorChannel Gen(int ch) { return generator.channels[ch]; }
        public ScopeChannel Scope(int ch) { return scope.channels[ch]; }
        public Measurements Meas(int scopeCh) { return scope.ChannelMeasurements[scopeCh]; }

        /// <summary>True when generator output 'genCh' is wired to scope input 'scopeCh'.</summary>
        public bool Linked(int genCh, int scopeCh)
        {
            return CableManager.AreLinked(generator.outputConnectors[genCh], scope.inputConnectors[scopeCh]);
        }

        public void Link(int genCh, int scopeCh)
        {
            cables.Connect(generator.outputConnectors[genCh], scope.inputConnectors[scopeCh]);
        }

        /// <summary>|value - target| &lt;= relTol * |target| (NaN = false).</summary>
        public static bool Near(double value, double target, double relTol)
        {
            if (double.IsNaN(value)) return false;
            return Math.Abs(value - target) <= Math.Abs(target) * relTol;
        }

        /// <summary>Bench back to a known state: cables removed, instruments preset and switched off.</summary>
        public void ResetBench()
        {
            cables.DisconnectAll();
            generator.Machine.SetPower(true);   // presets need a powered instrument
            scope.Machine.SetPower(true);
            generator.Preset();
            scope.DefaultSetup();
            generator.Machine.SetPower(false);
            scope.Machine.SetPower(false);
        }
    }

    /// <summary>One instruction with its validation condition.</summary>
    public class ScenarioStep
    {
        public string instruction;
        public string hint;
        public Func<ScenarioContext, bool> isComplete;
        /// <summary>Optional live text (current value vs target).</summary>
        public Func<ScenarioContext, string> feedback;
        public Action<ScenarioContext> onEnter;
        /// <summary>The condition must stay true this long (s) to validate, which filters flicker.</summary>
        public float holdSeconds = 0.5f;

        public ScenarioStep(string instruction, Func<ScenarioContext, bool> isComplete)
        {
            this.instruction = instruction;
            this.isComplete = isComplete;
        }
    }

    public class Scenario
    {
        public string title;
        public string objective;
        public Action<ScenarioContext> setup;
        public Func<ScenarioContext, string> summary;
        public readonly List<ScenarioStep> steps = new List<ScenarioStep>();

        public Scenario(string title, string objective)
        {
            this.title = title;
            this.objective = objective;
        }

        public ScenarioStep Add(string instruction, Func<ScenarioContext, bool> isComplete)
        {
            ScenarioStep s = new ScenarioStep(instruction, isComplete);
            steps.Add(s);
            return s;
        }
    }
}
