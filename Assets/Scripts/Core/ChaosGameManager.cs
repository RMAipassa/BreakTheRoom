using System;
using UnityEngine;

namespace BreakTheRoom.Core
{
    public class ChaosGameManager : MonoBehaviour
    {
        public enum RunState
        {
            Waiting,
            Playing,
            Finished
        }

        public static ChaosGameManager Instance { get; private set; }

        [SerializeField] private float runDurationSeconds = 180f;
        [SerializeField] private bool autoStartOnPlay = true;

        public event Action<int> ScoreChanged;
        public event Action<float> TimeChanged;
        public event Action<RunState> StateChanged;

        public RunState State { get; private set; } = RunState.Waiting;
        public int Score { get; private set; }
        public float TimeRemaining { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            TimeRemaining = runDurationSeconds;
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartRun();
            }
        }

        private void Update()
        {
            if (State != RunState.Playing)
            {
                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            TimeChanged?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0f)
            {
                EndRun();
            }
        }

        public void StartRun()
        {
            Score = 0;
            TimeRemaining = runDurationSeconds;
            SetState(RunState.Playing);
            ScoreChanged?.Invoke(Score);
            TimeChanged?.Invoke(TimeRemaining);
        }

        public void EndRun()
        {
            SetState(RunState.Finished);
        }

        public void AddChaos(int amount)
        {
            if (State != RunState.Playing || amount <= 0)
            {
                return;
            }

            Score += amount;
            ScoreChanged?.Invoke(Score);
        }

        private void SetState(RunState newState)
        {
            State = newState;
            StateChanged?.Invoke(State);
        }
    }
}
