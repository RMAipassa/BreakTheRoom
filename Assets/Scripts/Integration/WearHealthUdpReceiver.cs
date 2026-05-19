using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace BreakTheRoom.Integration
{
    [Serializable]
    public class WearHealthPacket
    {
        public int heartRateBpm;
        public int steps;
        public float caloriesKcal;
        public long lastUpdatedEpochMillis;
    }

    public class WearHealthUdpReceiver : MonoBehaviour
    {
        private const string DiscoveryRequestMessage = "BTR_DISCOVER_V1";
        private const string DiscoveryResponsePrefix = "BTR_HERE_V1:";

        [SerializeField] private int listenPort = 7777;
        [SerializeField] private bool autoStartOnEnable = true;
        [SerializeField] private bool verboseLogs;

        public WearHealthPacket LatestPacket { get; private set; }
        public bool HasPacket => LatestPacket != null;

        public event Action<WearHealthPacket> PacketReceived;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private volatile bool _isRunning;
        private readonly object _packetLock = new object();
        private string _pendingJson;

        private void OnEnable()
        {
            if (autoStartOnEnable)
            {
                StartReceiver();
            }
        }

        private void Update()
        {
            string json = null;

            lock (_packetLock)
            {
                if (!string.IsNullOrEmpty(_pendingJson))
                {
                    json = _pendingJson;
                    _pendingJson = null;
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var packet = JsonUtility.FromJson<WearHealthPacket>(json);
            if (packet == null)
            {
                return;
            }

            LatestPacket = packet;
            PacketReceived?.Invoke(packet);
        }

        private void OnDisable()
        {
            StopReceiver();
        }

        public void StartReceiver()
        {
            if (_isRunning)
            {
                return;
            }

            try
            {
                _udpClient = new UdpClient(listenPort);
                _isRunning = true;
                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();

                if (verboseLogs)
                {
                    Debug.Log($"[WearHealthUdpReceiver] Listening on UDP {listenPort}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WearHealthUdpReceiver] Failed to start: {ex.Message}");
                _isRunning = false;
            }
        }

        public void StopReceiver()
        {
            _isRunning = false;

            try
            {
                _udpClient?.Close();
            }
            catch
            {
            }

            _udpClient = null;

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(200);
            }

            _receiveThread = null;
        }

        private void ReceiveLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);

            while (_isRunning)
            {
                try
                {
                    var bytes = _udpClient.Receive(ref endpoint);
                    var json = Encoding.UTF8.GetString(bytes);

                    if (json == DiscoveryRequestMessage)
                    {
                        var response = $"{DiscoveryResponsePrefix}{listenPort}";
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        _udpClient.Send(responseBytes, responseBytes.Length, endpoint);

                        if (verboseLogs)
                        {
                            Debug.Log($"[WearHealthUdpReceiver] Discovery response sent to {endpoint.Address}");
                        }

                        continue;
                    }

                    lock (_packetLock)
                    {
                        _pendingJson = json;
                    }
                }
                catch (SocketException)
                {
                    if (_isRunning)
                    {
                        Thread.Sleep(50);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WearHealthUdpReceiver] Receive error: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }
    }
}
