using UnityEngine;

namespace BreakTheRoom.Destruction
{
    [RequireComponent(typeof(Collider))]
    public class ImpactDamageDealer : MonoBehaviour
    {
        [SerializeField] private float minImpactSpeed = 1.25f;
        [SerializeField] private float damagePerSpeed = 10f;
        [SerializeField] private float impulseMultiplier = 1f;
        [SerializeField] private bool damageSelfOnImpact = false;

        private BreakablePiece _selfBreakable;

        private void Awake()
        {
            _selfBreakable = GetComponentInParent<BreakablePiece>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            var speed = collision.relativeVelocity.magnitude;
            if (speed < minImpactSpeed)
            {
                return;
            }

            var damage = (speed - minImpactSpeed) * damagePerSpeed;
            var impulse = collision.impulse * impulseMultiplier;
            var hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            var otherBreakable = collision.collider.GetComponentInParent<BreakablePiece>();
            if (otherBreakable != null)
            {
                otherBreakable.ApplyDamage(damage, hitPoint, impulse);
            }

            if (damageSelfOnImpact && _selfBreakable != null)
            {
                _selfBreakable.ApplyDamage(damage, hitPoint, -impulse);
            }
        }
    }
}
