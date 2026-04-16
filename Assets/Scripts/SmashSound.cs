using UnityEngine;

namespace BreakTheRoom.Destruction
{
    [RequireComponent(typeof(AudioSource))]
    public class SmashSound : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField] private AudioClip[] smashClips;

        [Header("Impact settings")]
        [SerializeField] private float minImpactVelocity = 1.5f;
        [SerializeField] private float maxImpactVelocity = 12f;
        [SerializeField] private float minVolume = 0.15f;
        [SerializeField] private float maxVolume = 1f;

        [Header("Pitch randomization")]
        [SerializeField] private float minPitch = 0.92f;
        [SerializeField] private float maxPitch = 1.08f;

        [Header("Cooldown")]
        [SerializeField] private float playCooldown = 0.08f;

        private AudioSource audioSource;
        private float lastPlayTime = -999f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (smashClips == null || smashClips.Length == 0)
                return;

            if (Time.time < lastPlayTime + playCooldown)
                return;

            float impactVelocity = collision.relativeVelocity.magnitude;
            if (impactVelocity < minImpactVelocity)
                return;

            PlayImpact(impactVelocity);
        }

        public void PlaySmash(float impactVelocity = 5f)
        {
            if (smashClips == null || smashClips.Length == 0)
                return;

            if (Time.time < lastPlayTime + playCooldown)
                return;

            PlayImpact(impactVelocity);
        }

        private void PlayImpact(float impactVelocity)
        {
            float normalized = Mathf.InverseLerp(minImpactVelocity, maxImpactVelocity, impactVelocity);
            float volume = Mathf.Lerp(minVolume, maxVolume, normalized);
            float pitch = Random.Range(minPitch, maxPitch);

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(smashClips[Random.Range(0, smashClips.Length)], volume);

            lastPlayTime = Time.time;
        }
    }
}