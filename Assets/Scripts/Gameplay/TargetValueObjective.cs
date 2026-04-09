using BreakTheRoom.Core;
using UnityEngine;

namespace BreakTheRoom.Gameplay
{
    public class TargetValueObjective : MonoBehaviour
    {
        [SerializeField] private int targetScore = 2500;

        public bool IsCompleted { get; private set; }

        private void OnEnable()
        {
            var manager = ChaosGameManager.Instance;
            if (manager != null)
            {
                manager.ScoreChanged += HandleScoreChanged;
            }
        }

        private void OnDisable()
        {
            var manager = ChaosGameManager.Instance;
            if (manager != null)
            {
                manager.ScoreChanged -= HandleScoreChanged;
            }
        }

        private void HandleScoreChanged(int score)
        {
            if (IsCompleted || score < targetScore)
            {
                return;
            }

            IsCompleted = true;
            Debug.Log($"Objective complete. Target score {targetScore} reached.");
        }
    }
}
