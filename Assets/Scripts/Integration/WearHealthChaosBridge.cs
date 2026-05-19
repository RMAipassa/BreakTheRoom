using BreakTheRoom.Core;
using UnityEngine;

namespace BreakTheRoom.Integration
{
    public class WearHealthChaosBridge : MonoBehaviour
    {
        [SerializeField] private WearHealthUdpReceiver receiver;
        [SerializeField] private int stepsPerChaosPoint = 12;
        [SerializeField] private bool useHeartRateBonus = true;
        [SerializeField] private int highHeartRateThreshold = 140;

        private int _lastStepTotal = -1;

        private void Awake()
        {
            if (receiver == null)
            {
                receiver = GetComponent<WearHealthUdpReceiver>();
            }
        }

        private void OnEnable()
        {
            if (receiver != null)
            {
                receiver.PacketReceived += OnPacketReceived;
            }
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                receiver.PacketReceived -= OnPacketReceived;
            }
        }

        private void OnPacketReceived(WearHealthPacket packet)
        {
            var manager = ChaosGameManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (_lastStepTotal < 0)
            {
                _lastStepTotal = packet.steps;
                return;
            }

            var deltaSteps = Mathf.Max(0, packet.steps - _lastStepTotal);
            _lastStepTotal = packet.steps;

            if (deltaSteps <= 0)
            {
                return;
            }

            var chaos = deltaSteps / Mathf.Max(1, stepsPerChaosPoint);
            if (chaos <= 0)
            {
                return;
            }

            if (useHeartRateBonus && packet.heartRateBpm >= highHeartRateThreshold)
            {
                chaos += 1;
            }

            manager.AddChaos(chaos);
        }
    }
}
