using System;
using System.Collections.Generic;

namespace LabSim
{
    /// <summary>
    /// Code-defined teaching scenarios (fast to extend: one method per scenario).
    /// Each scenario = setup of the bench + ordered steps with a validation predicate.
    /// </summary>
    public static class ScenarioLibrary
    {
        public static List<Scenario> Build()
        {
            return new List<Scenario>
            {
                SineBasics(),
                LogicSquare(),
                TriggerAdjustment(),
                LoadMismatch(),
                TwoChannelPhase()
            };
        }

        // ------------------------------------------------------------------ shared steps

        static string F(double v, string unit, int digits = 4) { return EngineeringFormat.Format(v, unit, digits); }

        static void AddPowerStep(Scenario s)
        {
            s.Add("Allumez le DG822 Pro et le DHO814 (touches Power).", c => c.BothOn);
        }

        static void AddLinkStep(Scenario s, int gen, int scope)
        {
            ScenarioStep st = s.Add(string.Format(
                "Reliez la sortie CH{0} du générateur à l'entrée CH{1} de l'oscilloscope (clic sur un BNC puis sur l'autre).",
                gen + 1, scope + 1), c => c.Linked(gen, scope));
            st.hint = "Clic sur un connecteur déjà branché = débrancher. Clic droit ou Échap = annuler.";
        }

        static void AddOutputStep(Scenario s, int gen)
        {
            s.Add(string.Format("Activez la sortie CH{0} du générateur (touche Output).", gen + 1),
                  c => c.Gen(gen).outputEnabled);
        }

        static ScenarioStep AddParamStep(Scenario s, int gen, GeneratorParameter p, double target, double relTol, string text)
        {
            ScenarioStep st = s.Add(text, c => ScenarioContext.Near(c.generator.GetParameter(gen, p), target, relTol));
            st.feedback = c => "Actuel : " + F(c.generator.GetParameter(gen, p), GeneratorSpecs.Unit(p), 6) +
                               "   cible : " + F(target, GeneratorSpecs.Unit(p), 6);
            st.hint = "Sélectionnez le paramètre, puis molette (bouton rotatif) ou pavé numérique + unité.";
            return st;
        }

        static string MeasLine(ScenarioContext c, int ch)
        {
            Measurements m = c.Meas(ch);
            if (!m.valid) return "CH" + (ch + 1) + " : pas de mesure";
            return string.Format("CH{0} : f = {1}   Vpp = {2}   Vrms = {3}{4}",
                ch + 1, F(m.frequency, "Hz"), F(m.vpp, "V", 3), F(m.vrms, "V", 3),
                c.scope.TriggerStableTime > 0 ? "   (déclenché)" : "   (non déclenché)");
        }

        public static double MeasuredPhase(ScenarioContext c, int reference, int other)
        {
            AcquisitionResult r = c.scope.LastResult;
            if (r == null || !r.channelValid[reference] || !r.channelValid[other]) return double.NaN;
            return MeasurementEngine.PhaseDegrees(r.volts[reference], r.volts[other], r.sampleCount);
        }

        // ------------------------------------------------------------------ scenarios

        static Scenario SineBasics()
        {
            Scenario s = new Scenario("1. Sinus 1 kHz - 2 Vpp",
                "Générer une sinusoïde et la mesurer à l'oscilloscope.");
            s.setup = c =>
            {
                c.ResetBench();
                c.generator.SetWaveform(0, WaveformType.Square);
                c.generator.SetParameter(0, GeneratorParameter.Frequency, 2000);
            };

            AddPowerStep(s);
            AddLinkStep(s, 0, 0);
            s.Add("Sélectionnez la forme Sinus sur CH1.", c => c.Gen(0).wave.type == WaveformType.Sine);
            AddParamStep(s, 0, GeneratorParameter.Frequency, 1000, 0.001, "Réglez la fréquence à 1 kHz.");
            AddParamStep(s, 0, GeneratorParameter.Amplitude, 2, 0.001, "Réglez l'amplitude à 2 Vpp.");
            AddOutputStep(s, 0);

            ScenarioStep m = s.Add("Activez CH1 sur l'oscilloscope puis appuyez sur AUTO (ou 500 mV/div et 200 µs/div).",
                c => c.Scope(0).enabled && c.scope.TriggerStableTime > 1f &&
                     ScenarioContext.Near(c.Meas(0).frequency, 1000, 0.02) &&
                     ScenarioContext.Near(c.Meas(0).vpp, 2, 0.05));
            m.feedback = c => MeasLine(c, 0);
            m.holdSeconds = 1f;

            s.summary = c => string.Format(
                "Mesures : f = {0}, Vpp = {1}, Vrms = {2} (théorique Vpp / (2·√2) = {3}).",
                F(c.Meas(0).frequency, "Hz"), F(c.Meas(0).vpp, "V", 3), F(c.Meas(0).vrms, "V", 3),
                F(WaveformMath.TheoreticalRms(c.Gen(0).wave), "V", 3));
            return s;
        }

        static Scenario LogicSquare()
        {
            Scenario s = new Scenario("2. Carré logique 0 - 3,3 V",
                "Construire un signal logique avec offset et rapport cyclique, puis le vérifier.");
            s.setup = c => c.ResetBench();

            AddPowerStep(s);
            AddLinkStep(s, 0, 0);
            s.Add("Sélectionnez la forme Carré sur CH1.", c => c.Gen(0).wave.type == WaveformType.Square);
            AddParamStep(s, 0, GeneratorParameter.Frequency, 5000, 0.001, "Réglez la fréquence à 5 kHz.");
            AddParamStep(s, 0, GeneratorParameter.Amplitude, 3.3, 0.001, "Réglez l'amplitude à 3,3 Vpp.");
            AddParamStep(s, 0, GeneratorParameter.Offset, 1.65, 0.001, "Réglez l'offset à 1,65 V (niveau bas à 0 V).");
            AddParamStep(s, 0, GeneratorParameter.DutyCycle, 25, 0.001, "Réglez le rapport cyclique à 25 %.");
            AddOutputStep(s, 0);

            ScenarioStep m = s.Add("Observez CH1 (AUTO) : Vmin ≈ 0 V, Vmax ≈ 3,3 V, rapport cyclique ≈ 25 %.",
                c => c.Scope(0).enabled && c.scope.TriggerStableTime > 1f &&
                     Math.Abs(c.Meas(0).vmin) < 0.1 &&
                     ScenarioContext.Near(c.Meas(0).vmax, 3.3, 0.05) &&
                     Math.Abs(c.Meas(0).dutyPercent - 25) < 2);
            m.feedback = c => string.Format("Vmin = {0}   Vmax = {1}   RC = {2:F1} %",
                F(c.Meas(0).vmin, "V", 3), F(c.Meas(0).vmax, "V", 3), c.Meas(0).dutyPercent);
            m.holdSeconds = 1f;

            s.summary = c => string.Format(
                "Moyenne mesurée {0} (théorique {1}), Vrms mesurée {2} (théorique {3}).",
                F(c.Meas(0).vavg, "V", 3), F(WaveformMath.TheoreticalMean(c.Gen(0).wave), "V", 3),
                F(c.Meas(0).vrms, "V", 3), F(WaveformMath.TheoreticalRms(c.Gen(0).wave), "V", 3));
            return s;
        }

        static Scenario TriggerAdjustment()
        {
            Scenario s = new Scenario("3. Réglage du déclenchement",
                "Comprendre le rôle du niveau et du front de déclenchement.");
            s.setup = c =>
            {
                c.ResetBench();
                c.Link(0, 0);
                c.generator.SetWaveform(0, WaveformType.Ramp);
                c.generator.SetParameter(0, GeneratorParameter.Symmetry, 50);
                c.generator.SetParameter(0, GeneratorParameter.Frequency, 500);
                c.generator.SetParameter(0, GeneratorParameter.Amplitude, 4);
                c.scope.SetChannelEnabled(0, true);
                c.scope.SetVoltsPerDiv(0, 1.0);
                c.scope.SetTimePerDiv(500e-6);
                c.scope.SetTriggerMode(TriggerMode.Auto);
                c.scope.SetTriggerLevel(3.5);
            };

            AddPowerStep(s);
            AddOutputStep(s, 0);

            ScenarioStep lvl = s.Add(
                "La trace défile : le niveau de déclenchement (3,5 V) est hors du signal (±2 V). " +
                "Tournez le bouton TriggerLevel pour le ramener dans le signal.",
                c => Math.Abs(c.scope.trigger.level) < 1.9 && c.scope.TriggerStableTime > 1.5f);
            lvl.feedback = c => "Niveau : " + F(c.scope.trigger.level, "V", 3) +
                                (c.scope.TriggerStableTime > 0 ? "   (déclenché)" : "   (non déclenché)");
            lvl.hint = "Appui sur le bouton TriggerLevel = niveau à 50 %.";

            ScenarioStep slope = s.Add("Passez le déclenchement sur front descendant et observez le décalage de la trace.",
                c => c.scope.trigger.slope == TriggerSlope.Falling && c.scope.TriggerStableTime > 1f);
            slope.hint = "Touche TriggerSlopeToggle.";

            s.summary = c => "La trace est stable dès que le niveau coupe le signal : l'instant de déclenchement est " +
                             "placé au centre de l'écran à chaque acquisition.";
            return s;
        }

        static Scenario LoadMismatch()
        {
            Scenario s = new Scenario("4. Charge 50 Ω / High-Z",
                "Expliquer pourquoi l'amplitude mesurée double quand le générateur est réglé sur 50 Ω.");
            s.setup = c =>
            {
                c.ResetBench();
                c.Link(0, 0);
                c.generator.SetLoad(0, OutputLoad.FiftyOhm);
                c.generator.SetParameter(0, GeneratorParameter.Frequency, 10000);
                c.generator.SetParameter(0, GeneratorParameter.Amplitude, 2);
            };

            AddPowerStep(s);
            AddOutputStep(s, 0);

            ScenarioStep see = s.Add("Appuyez sur AUTO : le générateur affiche 2 Vpp, que mesure l'oscilloscope ?",
                c => c.scope.TriggerStableTime > 1f && ScenarioContext.Near(c.Meas(0).vpp, 4, 0.05));
            see.feedback = c => MeasLine(c, 0) + "\nThéorie : 2 × (50 + 50) / 50 × 1 MΩ / (1 MΩ + 50 Ω) = " +
                                F(2.0 * 2.0 * SignalMath.Divider(1.0, 50, ScopeSpecs.InputImpedance), "V", 4);
            see.holdSeconds = 1.5f;

            ScenarioStep fix = s.Add("Passez la sortie CH1 en High-Z (touche ToggleLoad) : la mesure doit valoir 2 Vpp.",
                c => c.Gen(0).load == OutputLoad.HighZ && c.scope.TriggerStableTime > 1f &&
                     ScenarioContext.Near(c.Meas(0).vpp, 2, 0.05));
            fix.feedback = c => MeasLine(c, 0);

            s.summary = c => "Le générateur (Rs = 50 Ω) calcule sa tension à vide en supposant la charge déclarée. " +
                             "Avec l'entrée 1 MΩ du DHO814, il faut déclarer High-Z.";
            return s;
        }

        static Scenario TwoChannelPhase()
        {
            Scenario s = new Scenario("5. Deux voies et déphasage",
                "Afficher deux signaux synchrones et mesurer un déphasage de 90°.");
            s.setup = c =>
            {
                c.ResetBench();
                for (int ch = 0; ch < 2; ch++)
                {
                    c.generator.SetParameter(ch, GeneratorParameter.Frequency, 1000);
                    c.generator.SetParameter(ch, GeneratorParameter.Amplitude, 2);
                }
            };

            AddPowerStep(s);
            AddLinkStep(s, 0, 0);
            AddLinkStep(s, 1, 1);
            s.Add("Activez les sorties CH1 et CH2 du générateur.", c => c.Gen(0).outputEnabled && c.Gen(1).outputEnabled);
            AddParamStep(s, 1, GeneratorParameter.Phase, 90, 0.005, "Sélectionnez CH2 et réglez sa phase à 90°.");

            ScenarioStep m = s.Add("Activez CH1 et CH2 sur l'oscilloscope puis AUTO. Mesurez le déphasage.",
                c => c.Scope(0).enabled && c.Scope(1).enabled && c.scope.TriggerStableTime > 1f &&
                     Math.Abs(Math.Abs(MeasuredPhase(c, 0, 1)) - 90) < 5);
            m.feedback = c => "Déphasage CH2/CH1 mesuré : " + MeasuredPhase(c, 0, 1).ToString("F1") + " °";
            m.holdSeconds = 1f;

            s.summary = c => "φ = 360° × Δt / T : un quart de période d'avance correspond à +90°.";
            return s;
        }
    }
}
