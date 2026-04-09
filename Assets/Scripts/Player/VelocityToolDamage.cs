using BreakTheRoom.Destruction;
using UnityEngine;

namespace BreakTheRoom.Player
{
    [RequireComponent(typeof(Collider))]
    public class VelocityToolDamage : MonoBehaviour
    {
        [SerializeField] private Rigidbody toolRigidbody;
        [SerializeField] private float minHitSpeed = 0.8f;
        [SerializeField] private float damagePerSpeed = 20f;
        [SerializeField] private float impulseMultiplier = 0.75f;
        [SerializeField] private float hitCooldown = 0.05f;

        private float _nextHitTime;

        private void Reset()
        {
            toolRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time < _nextHitTime)
            {
                return;
            }

            var speed = toolRigidbody != null
                ? toolRigidbody.linearVelocity.magnitude
                : collision.relativeVelocity.magnitude;

            if (speed < minHitSpeed)
            {
                return;
            }

            var target = collision.collider.GetComponentInParent<BreakablePiece>();
            if (target == null)
            {
                return;
            }

            var damage = (speed - minHitSpeed) * damagePerSpeed;
            var impulse = collision.impulse * impulseMultiplier;
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            target.ApplyDamage(damage, point, impulse);
            _nextHitTime = Time.time + hitCooldown;
        }
    }
}
