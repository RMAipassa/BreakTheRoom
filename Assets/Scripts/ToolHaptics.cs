using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ToolHaptics : MonoBehaviour
{
    [Header("Grab Haptics")]
    public float grabHapticStrength = 0.7f;
    public float grabHapticDuration = 0.12f;

    [Header("Impact Haptics")]
    public float impactHapticStrength = 1.0f;
    public float impactHapticDuration = 0.15f;
    public float minImpactVelocity = 1.5f;

    [Header("Cooldown")]
    public float hapticCooldown = 0.08f;

    private XRBaseInputInteractor currentInteractor;
    private XRGrabInteractable grabInteractable;
    private float lastHapticTime;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject as XRBaseInputInteractor;
        SendHaptics(grabHapticStrength, grabHapticDuration);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentInteractor == null)
            return;

        if (Time.time - lastHapticTime < hapticCooldown)
            return;

        float impact = collision.relativeVelocity.magnitude;

        if (impact >= minImpactVelocity)
        {
            SendHaptics(impactHapticStrength, impactHapticDuration);
            lastHapticTime = Time.time;
        }
    }

    public void TriggerHaptics()
    {
        SendHaptics(impactHapticStrength, impactHapticDuration);
    }

    public void TriggerHaptics(float strength, float duration)
    {
        SendHaptics(strength, duration);
    }

    private void SendHaptics(float strength, float duration)
    {
        if (currentInteractor != null)
        {
            currentInteractor.SendHapticImpulse(strength, duration);
        }
    }
}