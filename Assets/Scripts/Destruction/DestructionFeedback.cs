using System.Collections.Generic;
using UnityEngine;

namespace BreakTheRoom.Destruction
{
    [RequireComponent(typeof(BreakablePiece))]
    public class DestructionFeedback : MonoBehaviour
    {
        public enum SurfaceType
        {
            Generic,
            Wood,
            Glass,
            Concrete,
            Metal
        }

        [SerializeField] private SurfaceType surfaceType = SurfaceType.Generic;
        [SerializeField] private float minImpactImpulse = 1.2f;
        [SerializeField] private float impactCooldown = 0.05f;
        [SerializeField] private bool spawnDecals = false;
        [SerializeField] private float decalLifetime = 4f;
        [SerializeField] private Vector2 decalSizeRange = new Vector2(0.06f, 0.15f);

        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

        private BreakablePiece _breakable;
        private AudioSource _audio;
        private float _nextImpactTime;

        private void Awake()
        {
            _breakable = GetComponent<BreakablePiece>();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.minDistance = 0.8f;
            _audio.maxDistance = 20f;
            _audio.dopplerLevel = 0f;
        }

        private void OnEnable()
        {
            if (_breakable == null)
            {
                return;
            }

            _breakable.Damaged += HandleDamaged;
            _breakable.Broken += HandleBroken;
        }

        private void OnDisable()
        {
            if (_breakable == null)
            {
                return;
            }

            _breakable.Damaged -= HandleDamaged;
            _breakable.Broken -= HandleBroken;
        }

        private void HandleDamaged(BreakablePiece _, float amount, Vector3 hitPoint, Vector3 impulse)
        {
            if (Time.time < _nextImpactTime || impulse.magnitude < minImpactImpulse)
            {
                return;
            }

            var loudness = Mathf.Clamp01((amount * 0.03f) + (impulse.magnitude * 0.06f));
            PlaySurfaceSound(GetClip(false), 0.2f + loudness * 0.45f, Random.Range(0.95f, 1.05f));
            SpawnBurst(hitPoint, impulse, false);
            TrySpawnDecal(hitPoint, impulse, false);
            _nextImpactTime = Time.time + impactCooldown;
        }

        private void HandleBroken(BreakablePiece _)
        {
            var point = transform.position;
            var impulse = Vector3.up * 2f;
            PlaySurfaceSound(GetClip(true), 0.45f, Random.Range(0.93f, 1.02f));
            SpawnBurst(point, impulse, true);
            TrySpawnDecal(point, impulse, true);
        }

        private void TrySpawnDecal(Vector3 position, Vector3 impulse, bool isBreak)
        {
            if (!spawnDecals)
            {
                return;
            }

            var normal = impulse.sqrMagnitude > 0.0001f ? -impulse.normalized : Vector3.up;
            var decal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            decal.name = isBreak ? "BreakDecal" : "ImpactDecal";
            decal.transform.position = position + normal * 0.005f;
            decal.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

            var size = Random.Range(decalSizeRange.x, decalSizeRange.y) * (isBreak ? 1.35f : 1f);
            decal.transform.localScale = new Vector3(size, size, 0.004f);

            var col = decal.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = decal.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = Color.Lerp(GetParticleColor(), Color.black, 0.45f);
                color.a = 0.85f;
                mat.color = color;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                renderer.sharedMaterial = mat;
            }

            Destroy(decal, decalLifetime);
        }

        private void PlaySurfaceSound(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _audio == null)
            {
                return;
            }

            _audio.pitch = pitch;
            _audio.PlayOneShot(clip, volume);
        }

        private void SpawnBurst(Vector3 position, Vector3 impulse, bool isBreak)
        {
            var go = new GameObject(isBreak ? "BreakFx" : "ImpactFx");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = isBreak ? 0.5f : 0.22f;
            main.startSpeed = isBreak ? 3.2f : 1.7f;
            main.startSize = isBreak ? 0.06f : 0.035f;
            main.gravityModifier = 0.7f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = isBreak ? 80 : 24;
            main.startColor = GetParticleColor();
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 24f;
            shape.radius = 0.02f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            var dir = impulse.sqrMagnitude > 0.001f ? impulse.normalized : Vector3.up;
            velocity.x = dir.x * 0.6f;
            velocity.y = Mathf.Max(0.15f, dir.y * 0.6f);
            velocity.z = dir.z * 0.6f;

            ps.Emit(isBreak ? 36 : 10);
        }

        private Color GetParticleColor()
        {
            switch (surfaceType)
            {
                case SurfaceType.Wood: return new Color(0.43f, 0.33f, 0.19f, 0.95f);
                case SurfaceType.Glass: return new Color(0.72f, 0.86f, 0.95f, 0.9f);
                case SurfaceType.Concrete: return new Color(0.62f, 0.62f, 0.62f, 0.95f);
                case SurfaceType.Metal: return new Color(0.64f, 0.66f, 0.72f, 0.95f);
                default: return new Color(0.75f, 0.75f, 0.75f, 0.9f);
            }
        }

        private AudioClip GetClip(bool isBreak)
        {
            var key = surfaceType + (isBreak ? "_Break" : "_Impact");
            if (ClipCache.TryGetValue(key, out var clip))
            {
                return clip;
            }

            var generated = CreateProceduralClip(key, surfaceType, isBreak);
            ClipCache[key] = generated;
            return generated;
        }

        private static AudioClip CreateProceduralClip(string name, SurfaceType type, bool isBreak)
        {
            const int sampleRate = 22050;
            var length = isBreak ? 0.32f : 0.14f;
            var samples = Mathf.CeilToInt(sampleRate * length);
            var data = new float[samples];

            var freq = 350f;
            var ring = 0.2f;

            switch (type)
            {
                case SurfaceType.Wood: freq = 240f; ring = 0.14f; break;
                case SurfaceType.Glass: freq = 1020f; ring = 0.34f; break;
                case SurfaceType.Concrete: freq = 170f; ring = 0.09f; break;
                case SurfaceType.Metal: freq = 620f; ring = 0.42f; break;
            }

            var phase = 0f;
            var low = 0f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var env = Mathf.Exp(-t * (isBreak ? 8f : 14f));
                var noise = (Random.value * 2f - 1f) * env;
                phase += (2f * Mathf.PI * freq) / sampleRate;
                var tonal = Mathf.Sin(phase) * ring * env;
                low = Mathf.Lerp(low, noise + tonal, 0.28f);
                data[i] = Mathf.Clamp(low, -1f, 1f) * 0.7f;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
