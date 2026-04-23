using UnityEngine;

namespace BreakTheRoom.Integration
{
    public class WearHealthHud : MonoBehaviour
    {
        [SerializeField] private WearHealthUdpReceiver receiver;
        [SerializeField] private Vector2 offset = new Vector2(16f, 16f);
        [SerializeField] private float width = 280f;
        [SerializeField] private int fontSize = 18;
        [SerializeField] private bool showWhenNoData = true;

        private GUIStyle _boxStyle;
        private GUIStyle _lineStyle;

        private void Awake()
        {
            if (receiver == null)
            {
                receiver = FindObjectOfType<WearHealthUdpReceiver>();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (receiver == null)
            {
                if (showWhenNoData)
                {
                    DrawPanel("Wear bridge niet gevonden");
                }
                return;
            }

            if (!receiver.HasPacket)
            {
                if (showWhenNoData)
                {
                    DrawPanel("Wachten op watch data...");
                }
                return;
            }

            var packet = receiver.LatestPacket;
            var heartRate = packet.heartRateBpm >= 0 ? packet.heartRateBpm.ToString() : "-";
            var text =
                "Wear OS Live\n" +
                $"Hartslag: {heartRate} bpm\n" +
                $"Stappen: {packet.steps}\n" +
                $"Kcal: {packet.caloriesKcal:0.0}";

            DrawPanel(text);
        }

        private void DrawPanel(string text)
        {
            const float panelHeight = 120f;
            var rect = new Rect(offset.x, offset.y, width, panelHeight);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            var labelRect = new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height - 20f);
            GUI.Label(labelRect, text, _lineStyle);
        }

        private void EnsureStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.textColor = Color.white;
            }

            if (_lineStyle == null)
            {
                _lineStyle = new GUIStyle(GUI.skin.label);
                _lineStyle.fontSize = fontSize;
                _lineStyle.normal.textColor = Color.white;
            }
        }
    }
}
