using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ToolTutorialOpener : MonoBehaviour
{
    [Header("Panel manager")]
    public TutorialPanelManager tutorialPanelManager;

    [Header("Tutorial content")]
    public string tutorialTitle;
    public Sprite[] tutorialPages;

    [Header("PC test")]
    public bool enableRightClickTest = true;
    public Camera raycastCamera;
    public float raycastDistance = 10f;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (raycastCamera == null)
            raycastCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();

        if (interactable != null)
            interactable.selectEntered.AddListener(OnXRSelectEntered);
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnXRSelectEntered);
    }

    private void Update()
    {
        if (!enableRightClickTest)
            return;

        if (RightClickPressed())
        {
            TryOpenWithMouseRaycast();
        }
    }

    private bool RightClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(1))
            return true;
#endif

        return false;
    }

    private void TryOpenWithMouseRaycast()
    {
        if (raycastCamera == null)
            raycastCamera = Camera.main;

        if (raycastCamera == null)
        {
            Debug.LogWarning("Aucune caméra trouvée pour le raycast souris.");
            return;
        }

        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
            {
                OpenTutorial();
            }
        }
    }

    private void OnXRSelectEntered(SelectEnterEventArgs args)
    {
        OpenTutorial();
    }

    public void OpenTutorial()
    {
        if (tutorialPanelManager == null)
        {
            Debug.LogWarning("TutorialPanelManager non assigné sur " + gameObject.name);
            return;
        }

        tutorialPanelManager.OpenTutorial(tutorialTitle, tutorialPages);
    }
}