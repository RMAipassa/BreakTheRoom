using BreakTheRoom.Core;
using UnityEngine;

namespace BreakTheRoom.Gameplay
{
    public class ChaosHudOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private Vector2 screenOffset = new Vector2(12f, 12f);
        [SerializeField] private bool useWristHudAlways = true;
        [SerializeField] private Vector3 wristLocalPosition = new Vector3(0.07f, 0.04f, 0.08f);
        [SerializeField] private Vector3 wristLocalEuler = new Vector3(72f, -90f, 0f);

        private GUIStyle _box;
        private GUIStyle _text;
        private Transform _wristAnchor;
        private TextMesh _wristText;
        private MeshRenderer _wristRenderer;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                visible = !visible;
            }

            if (useWristHudAlways)
            {
                EnsureWristHud();
                UpdateWristHud();
            }
        }

        private void OnGUI()
        {
            if (useWristHudAlways)
            {
                return;
            }

            if (!visible)
            {
                return;
            }

            var manager = ChaosGameManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (_box == null)
            {
                _box = new GUIStyle(GUI.skin.box);
                _text = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    richText = true,
                    alignment = TextAnchor.UpperLeft
                };
                _text.normal.textColor = Color.white;
            }

            var minutes = Mathf.FloorToInt(manager.TimeRemaining / 60f);
            var seconds = Mathf.FloorToInt(manager.TimeRemaining % 60f);
            var content =
                "<b>Chaos Run</b>\n"
                + $"Score: {manager.Score}\n"
                + $"Time: {minutes:00}:{seconds:00}\n"
                + $"State: {manager.State}";

            var rect = new Rect(screenOffset.x, screenOffset.y, 240f, 110f);
            GUI.Box(rect, GUIContent.none, _box);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 12f), content, _text);
        }

        private void EnsureWristHud()
        {
            if (_wristAnchor == null)
            {
                _wristAnchor = FindLeftControllerTransform();
                if (_wristAnchor == null)
                {
                    return;
                }
            }

            if (_wristText != null)
            {
                return;
            }

            var go = new GameObject("WristChaosHud");
            go.transform.SetParent(_wristAnchor, false);
            go.transform.localPosition = wristLocalPosition;
            go.transform.localRotation = Quaternion.Euler(wristLocalEuler);

            _wristText = go.AddComponent<TextMesh>();
            _wristText.fontSize = 56;
            _wristText.characterSize = 0.012f;
            _wristText.anchor = TextAnchor.UpperLeft;
            _wristText.color = new Color(0.92f, 0.12f, 0.12f);

            _wristRenderer = _wristText.GetComponent<MeshRenderer>();
        }

        private void UpdateWristHud()
        {
            if (_wristText == null)
            {
                return;
            }

            var manager = ChaosGameManager.Instance;
            if (manager == null)
            {
                _wristText.text = "Chaos HUD\nNo manager";
                return;
            }

            if (_wristRenderer != null)
            {
                _wristRenderer.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            var minutes = Mathf.FloorToInt(manager.TimeRemaining / 60f);
            var seconds = Mathf.FloorToInt(manager.TimeRemaining % 60f);
            _wristText.text =
                "Chaos Run\n"
                + $"Score: {manager.Score}\n"
                + $"Time: {minutes:00}:{seconds:00}\n"
                + $"State: {manager.State}";
        }

        private Transform FindLeftControllerTransform()
        {
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform best = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                var n = t.name.ToLowerInvariant();
                if (!n.Contains("left"))
                {
                    continue;
                }

                var score = 0;
                if (n == "left controller") score += 100;
                if (n.Contains("left controller")) score += 50;
                if (n.Contains("interactionattach")) score += 40;
                if (n.Contains("attach")) score += 15;
                if (n.Contains("stabilized")) score -= 25;
                if (n.Contains("teleport")) score -= 25;

                if (score > bestScore)
                {
                    best = t;
                    bestScore = score;
                }
            }

            return best;
        }
    }
}
