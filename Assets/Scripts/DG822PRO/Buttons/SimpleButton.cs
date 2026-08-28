using UnityEngine;

public class SimpleButton : MonoBehaviour
{
    public string buttonId;

    void OnMouseDown()
    {
        if (MachineState.Instance == null || !MachineState.Instance.IsPoweredOn)
        {
            Debug.Log($"[{buttonId}] Machine éteinte, action ignorée.");
            return;
        }

        Debug.Log($"[{buttonId}] Bouton pressé.");
        // Point d'accroche pour plus tard : action sur l'écran, changement de paramètre, etc.
    }
}