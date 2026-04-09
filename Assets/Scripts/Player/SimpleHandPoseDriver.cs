using UnityEngine;

namespace BreakTheRoom.Player
{
    public class SimpleHandPoseDriver : MonoBehaviour
    {
        [SerializeField] private bool isLeftHand = true;
        [SerializeField] private float closeSpeed = 10f;

        private Transform[] _fingerRoots;
        private float _grip;

        private void Awake()
        {
            _fingerRoots = new[]
            {
                transform.Find("Index"),
                transform.Find("Middle"),
                transform.Find("Ring"),
                transform.Find("Pinky"),
                transform.Find("Thumb")
            };
        }

        private void Update()
        {
            var targetGrip = isLeftHand ? (Input.GetKey(KeyCode.Q) ? 1f : 0f) : (Input.GetKey(KeyCode.E) ? 1f : 0f);
            _grip = Mathf.MoveTowards(_grip, targetGrip, Time.deltaTime * closeSpeed);

            for (var i = 0; i < _fingerRoots.Length; i++)
            {
                var finger = _fingerRoots[i];
                if (finger == null)
                {
                    continue;
                }

                var curl = i == 4 ? 28f : 68f;
                finger.localRotation = Quaternion.Euler(-_grip * curl, 0f, 0f);
            }
        }
    }
}
