using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ToolHaptics : MonoBehaviour
{
    private XRBaseInputInteractor currentInteractor;

    public float hapticStrength = 0.7f;
    public float hapticDuration = 0.12f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject as XRBaseInputInteractor;

        TriggerHaptics(); // test: trillen zodra je oppakt
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
    }

    public void TriggerHaptics()
    {
        if (currentInteractor != null)
        {
            currentInteractor.SendHapticImpulse(hapticStrength, hapticDuration);
        }
    }
}