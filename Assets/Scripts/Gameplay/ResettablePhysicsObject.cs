using UnityEngine;

namespace BreakTheRoom.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class ResettablePhysicsObject : MonoBehaviour
    {
        [SerializeField] private float resetBelowY = -10f;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Rigidbody _rb;

        private void Awake()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _rb = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            if (transform.position.y < resetBelowY)
            {
                ResetObject();
            }
        }

        [ContextMenu("Reset Object")]
        public void ResetObject()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = _spawnPosition;
            _rb.rotation = _spawnRotation;
        }
    }
}
