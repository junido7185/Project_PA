using TMPro;
using UnityEngine;

[ExecuteAlways]
public class PrototypeWorldLabel : MonoBehaviour
{
    public string label = "Guide";
    public Color color = Color.white;
    public float fontSize = 2.4f;
    public TextAlignmentOptions alignment = TextAlignmentOptions.Center;

    TextMeshPro _text;

    void Awake() => EnsureText();
    void OnValidate() => EnsureText();

    void LateUpdate()
    {
        EnsureText();

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 toCamera = transform.position - cam.transform.position;
        if (toCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    public void Set(string text, Color textColor, float size)
    {
        label = text;
        color = textColor;
        fontSize = size;
        EnsureText();
    }

    void EnsureText()
    {
        if (_text == null)
            _text = GetComponent<TextMeshPro>() ?? gameObject.AddComponent<TextMeshPro>();

        _text.text = label;
        _text.color = color;
        _text.fontSize = fontSize;
        _text.alignment = alignment;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.raycastTarget = false;
    }
}
