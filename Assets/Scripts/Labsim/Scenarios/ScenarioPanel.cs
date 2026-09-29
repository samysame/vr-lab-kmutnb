using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LabSim
{
    /// <summary>
    /// Screen-space overlay Canvas showing the scenario: title, step counter, instruction,
    /// live feedback, hint, hovered object and a progress bar. Buttons: previous, next, restart.
    /// </summary>
    public class ScenarioPanel : MonoBehaviour
    {
        public ScenarioManager manager;

        [Header("Texts")]
        public TMP_Text titleText;
        public TMP_Text stepCounterText;
        public TMP_Text instructionText;
        public TMP_Text feedbackText;
        public TMP_Text hintText;
        public TMP_Text hoverText;
        public TMP_Text messageText;

        [Header("Controls")]
        public TMP_Dropdown scenarioDropdown;
        public Button previousButton;
        public Button nextButton;
        public Button restartButton;
        [Tooltip("Image of type Filled (horizontal) used as progress bar.")]
        public Image progressFill;

        [Range(1f, 30f)] public float liveRefreshRate = 8f;

        float refreshTimer;
        float messageHideAt;

        void Start()
        {
            if (manager == null) manager = FindManager();
            if (manager == null) { enabled = false; return; }

            manager.OnProgressChanged += Refresh;
            HoverHighlight.OnHoverChanged += HandleHover;
            if (CableManager.Instance != null) CableManager.Instance.OnMessage += ShowMessage;

            if (previousButton != null) previousButton.onClick.AddListener(manager.PreviousStep);
            if (nextButton != null) nextButton.onClick.AddListener(manager.NextStep);
            if (restartButton != null) restartButton.onClick.AddListener(manager.Restart);

            if (scenarioDropdown != null)
            {
                scenarioDropdown.ClearOptions();
                List<string> titles = new List<string>();
                foreach (Scenario s in manager.Scenarios) titles.Add(s.title);
                scenarioDropdown.AddOptions(titles);
                scenarioDropdown.SetValueWithoutNotify(Mathf.Max(0, manager.ScenarioIndex));
                scenarioDropdown.onValueChanged.AddListener(manager.StartScenario);
            }
            if (hoverText != null) hoverText.text = "";
            if (messageText != null) messageText.text = "";
            Refresh();
        }

        ScenarioManager FindManager()
        {
            ScenarioManager m = GetComponentInParent<ScenarioManager>();
            if (m == null) Debug.LogError("[ScenarioPanel] Assign the ScenarioManager.");
            return m;
        }

        void OnDestroy()
        {
            if (manager != null) manager.OnProgressChanged -= Refresh;
            HoverHighlight.OnHoverChanged -= HandleHover;
            if (CableManager.Instance != null) CableManager.Instance.OnMessage -= ShowMessage;
        }

        void Update()
        {
            if (messageText != null && messageHideAt > 0 && Time.time > messageHideAt)
            {
                messageText.text = "";
                messageHideAt = 0;
            }

            refreshTimer += Time.deltaTime;
            if (refreshTimer < 1f / liveRefreshRate) return;
            refreshTimer = 0f;
            if (feedbackText != null) feedbackText.text = manager.LiveText();
            if (progressFill != null) progressFill.fillAmount = Progress();
        }

        void Refresh()
        {
            Scenario s = manager.Current;
            if (s == null) return;

            if (titleText != null) titleText.text = s.title;
            if (scenarioDropdown != null) scenarioDropdown.SetValueWithoutNotify(manager.ScenarioIndex);

            if (manager.IsCompleted)
            {
                if (stepCounterText != null) stepCounterText.text = "Terminé";
                if (instructionText != null) instructionText.text = "<color=#3CFF6A>Scénario réussi.</color> " + s.objective;
                if (hintText != null) hintText.text = "";
            }
            else
            {
                ScenarioStep step = manager.CurrentStep;
                if (stepCounterText != null) stepCounterText.text = "Étape " + (manager.StepIndex + 1) + " / " + s.steps.Count;
                if (instructionText != null) instructionText.text = step.instruction;
                if (hintText != null) hintText.text = string.IsNullOrEmpty(step.hint) ? "" : "Astuce : " + step.hint;
            }
            if (feedbackText != null) feedbackText.text = manager.LiveText();
            if (progressFill != null) progressFill.fillAmount = Progress();
        }

        float Progress()
        {
            Scenario s = manager.Current;
            if (s == null || s.steps.Count == 0) return 0f;
            if (manager.IsCompleted) return 1f;
            return (manager.StepIndex + manager.HoldProgress) / s.steps.Count;
        }

        void HandleHover(string label)
        {
            if (hoverText != null) hoverText.text = string.IsNullOrEmpty(label) ? "" : label;
        }

        void ShowMessage(string msg)
        {
            if (messageText == null) return;
            messageText.text = msg;
            messageHideAt = Time.time + 3f;
        }
    }
}
