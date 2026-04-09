using System;
using System.Collections.Generic;
using UnityEngine;

namespace BreakTheRoom.Destruction
{
    [RequireComponent(typeof(Collider))]
    public class BreakablePiece : MonoBehaviour
    {
        [Header("Durability")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float minImpulseToBreak = 0f;

        [Header("Fracture")]
        [SerializeField] private GameObject fracturePrefab;
        [SerializeField] private float chunkImpulseMultiplier = 1f;
        [SerializeField] private float radialBreakForce = 2.75f;
        [SerializeField] private float impactDirectionForce = 1.6f;
        [SerializeField] private float randomForceJitter = 0.3f;
        [SerializeField] private float randomTorqueImpulse = 0.35f;

        [Header("Impact Propagation")]
        [SerializeField] private bool propagateImpactDamage = true;
        [SerializeField] private float minDamageToPropagate = 10f;
        [SerializeField] private float propagationRadius = 1.2f;
        [SerializeField] private float propagatedDamageMultiplier = 0.35f;
        [SerializeField] private float propagatedImpulseMultiplier = 0.45f;

        [Header("Scoring")]
        [SerializeField] private int chaosValue = 10;

        public event Action<BreakablePiece> Broken;
        public event Action<BreakablePiece, float, Vector3, Vector3> Damaged;

        public bool IsBroken { get; private set; }
        public float Health { get; private set; }
        public int ChaosValue => chaosValue;

        private Rigidbody _rb;
        private Collider[] _colliders;
        private Renderer[] _renderers;

        private void Awake()
        {
            Health = maxHealth;
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 impulse, bool allowPropagation = true)
        {
            if (IsBroken || amount <= 0f)
            {
                return;
            }

            if (minImpulseToBreak > 0f && impulse.magnitude < minImpulseToBreak)
            {
                return;
            }

            Health = Mathf.Max(0f, Health - amount);
            Damaged?.Invoke(this, amount, hitPoint, impulse);

            if (allowPropagation)
            {
                PropagateImpact(amount, hitPoint, impulse);
            }

            if (Health <= 0f)
            {
                Break(hitPoint, impulse);
            }
        }

        public void Break(Vector3 hitPoint, Vector3 impulse)
        {
            if (IsBroken)
            {
                return;
            }

            IsBroken = true;

            SpawnFracture(hitPoint, impulse);
            SetVisualState(false);
            Broken?.Invoke(this);
            Destroy(gameObject);
        }

        private void SpawnFracture(Vector3 hitPoint, Vector3 impulse)
        {
            if (fracturePrefab == null)
            {
                return;
            }

            var chunksRoot = Instantiate(fracturePrefab, transform.position, transform.rotation);
            var sourceVelocity = _rb != null ? _rb.linearVelocity : Vector3.zero;
            var sourceAngular = _rb != null ? _rb.angularVelocity : Vector3.zero;

            foreach (var chunkRb in chunksRoot.GetComponentsInChildren<Rigidbody>())
            {
                chunkRb.linearVelocity = sourceVelocity;
                chunkRb.angularVelocity = sourceAngular;

                var toChunk = chunkRb.worldCenterOfMass - hitPoint;
                var distance = Mathf.Max(0.2f, toChunk.magnitude);
                var radial = toChunk.normalized * (radialBreakForce / distance);

                var impactDir = impulse.sqrMagnitude > 0.0001f
                    ? impulse.normalized * impactDirectionForce
                    : Vector3.zero;

                var jitter = 1f + UnityEngine.Random.Range(-randomForceJitter, randomForceJitter);
                var totalImpulse = (impulse * chunkImpulseMultiplier + radial + impactDir) * jitter;

                chunkRb.AddForce(totalImpulse, ForceMode.Impulse);
                chunkRb.AddForceAtPosition(totalImpulse * 0.35f, hitPoint, ForceMode.Impulse);

                if (randomTorqueImpulse > 0f)
                {
                    chunkRb.AddTorque(UnityEngine.Random.insideUnitSphere * randomTorqueImpulse, ForceMode.Impulse);
                }
            }
        }

        private void SetVisualState(bool enabledState)
        {
            foreach (var col in _colliders)
            {
                col.enabled = enabledState;
            }

            foreach (var rend in _renderers)
            {
                rend.enabled = enabledState;
            }
        }

        private void PropagateImpact(float amount, Vector3 hitPoint, Vector3 impulse)
        {
            if (!propagateImpactDamage || amount < minDamageToPropagate || propagationRadius <= 0f)
            {
                return;
            }

            var propagatedDamage = amount * propagatedDamageMultiplier;
            if (propagatedDamage <= 0.01f)
            {
                return;
            }

            var hits = Physics.OverlapSphere(hitPoint, propagationRadius, ~0, QueryTriggerInteraction.Ignore);
            var touchedBodies = new HashSet<Rigidbody>();

            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null)
                {
                    continue;
                }

                var other = col.GetComponentInParent<BreakablePiece>();
                if (other != null && other != this)
                {
                    var point = col.ClosestPoint(hitPoint);
                    var dir = (point - hitPoint).sqrMagnitude > 0.0001f ? (point - hitPoint).normalized : impulse.normalized;
                    var propagatedImpulse = (impulse * propagatedImpulseMultiplier) + (dir * propagatedImpulseMultiplier);
                    other.ApplyDamage(propagatedDamage, point, propagatedImpulse, false);
                }

                var rb = col.attachedRigidbody;
                if (rb == null || touchedBodies.Contains(rb))
                {
                    continue;
                }

                touchedBodies.Add(rb);
                var forceDir = (rb.worldCenterOfMass - hitPoint).normalized;
                rb.AddForce(forceDir * impulse.magnitude * propagatedImpulseMultiplier, ForceMode.Impulse);
            }
        }
    }
}
