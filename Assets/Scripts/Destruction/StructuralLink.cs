using UnityEngine;

namespace BreakTheRoom.Destruction
{
    public class StructuralLink : MonoBehaviour
    {
        [SerializeField] private float breakForceThreshold = 2500f;
        [SerializeField] private float breakTorqueThreshold = 1500f;
        [SerializeField] private float supportDamageOnSnap = 50f;

        private Joint _joint;
        private BreakablePiece _owner;

        private void Awake()
        {
            _joint = GetComponent<Joint>();
            _owner = GetComponentInParent<BreakablePiece>();

            if (_joint == null)
            {
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (_joint == null)
            {
                return;
            }

            var force = _joint.currentForce.magnitude;
            var torque = _joint.currentTorque.magnitude;

            if (force >= breakForceThreshold || torque >= breakTorqueThreshold)
            {
                SnapLink();
            }
        }

        private void SnapLink()
        {
            var joint = _joint;
            if (joint == null)
            {
                enabled = false;
                return;
            }

            var snapImpulse = (joint.currentForce + joint.currentTorque) * Time.fixedDeltaTime;
            _joint = null;
            Destroy(joint);
            enabled = false;

            if (_owner != null)
            {
                _owner.ApplyDamage(supportDamageOnSnap, transform.position, snapImpulse);
            }
        }
    }
}
