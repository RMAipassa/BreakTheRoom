using BreakTheRoom.Core;
using BreakTheRoom.Destruction;
using UnityEngine;

namespace BreakTheRoom.Gameplay
{
    [RequireComponent(typeof(BreakablePiece))]
    public class ScoreOnBreak : MonoBehaviour
    {
        [SerializeField] private int scoreOverride = -1;

        private BreakablePiece _breakable;

        private void Awake()
        {
            _breakable = GetComponent<BreakablePiece>();
            _breakable.Broken += OnBroken;
        }

        private void OnDestroy()
        {
            if (_breakable != null)
            {
                _breakable.Broken -= OnBroken;
            }
        }

        private void OnBroken(BreakablePiece target)
        {
            var manager = ChaosGameManager.Instance;
            if (manager == null)
            {
                return;
            }

            var scoreToAdd = scoreOverride >= 0 ? scoreOverride : target.ChaosValue;
            manager.AddChaos(scoreToAdd);
        }
    }
}
