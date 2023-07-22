using UnityEngine;

namespace ModdingAPI;

internal class ModVersionUI : MonoBehaviour {
    private readonly GUIStyle _style = new(GUIStyle.none);

    public string drawString;

    private void Start() {
        _style.normal.textColor = Color.white;
        _style.alignment = TextAnchor.UpperLeft;
        _style.padding = new RectOffset(5, 5, 5, 5);
    }

    public void OnGUI() {
        if (drawString == null) return;

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), drawString, _style);
    }
}