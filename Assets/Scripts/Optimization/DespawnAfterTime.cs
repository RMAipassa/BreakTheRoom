using UnityEngine;

namespace BreakTheRoom.Optimization
{
    public class DespawnAfterTime : MonoBehaviour
    {
        [SerializeField] private float lifeTimeSeconds = 12f;

        private void OnEnable()
        {
            Destroy(gameObject, lifeTimeSeconds);
        }
    }
}
