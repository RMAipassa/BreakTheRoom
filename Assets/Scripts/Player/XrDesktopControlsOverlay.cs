using UnityEngine;

namespace BreakTheRoom.Player
{
    public class XrDesktopControlsOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        private GUIStyle _style;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 14,
                    wordWrap = true,
                    richText = true,
                    padding = new RectOffset(10, 10, 8, 8)
                };
            }

            var text =
                "<b>Desktop XR Controls</b>\n" +
                "MMB: lock cursor | Esc: unlock\n" +
                "WASD: move | Shift: sprint | Mouse: look\n" +
                "RMB: swing equipped tool\n" +
                "F: equip nearest tool | G: drop tool\n" +
                "VR: press Right Trigger (R2) near tool to equip\n" +
                "VR: press Left Trigger (L2) to drop equipped tool\n" +
                "Hold T: tool tune mode (J/L,U/O,I/K + arrows,Q/E)\n" +
                "Use XR Device Simulator window for hand/controller bindings\n" +
                "` (backquote): toggle this panel";

            GUI.Box(new Rect(12f, 12f, 460f, 130f), text, _style);
        }
    }
}
