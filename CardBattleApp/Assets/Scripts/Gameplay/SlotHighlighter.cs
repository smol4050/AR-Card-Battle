using UnityEngine;

public class SlotHighlighter : MonoBehaviour
{
    private MeshRenderer _renderer;
    private Color _originalColor;
    public Color highlightColor = Color.yellow;
    public float pulseSpeed = 2f;
    private bool _isHighlighting = false;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    public void SetHighlight(bool active)
    {
        _isHighlighting = active;
        if (!active && _renderer != null) _renderer.material.color = _originalColor;
    }

    void Update()
    {
        if (_isHighlighting && _renderer != null)
        {
            float lerp = Mathf.PingPong(Time.time * pulseSpeed, 1);
            _renderer.material.color = Color.Lerp(_originalColor, highlightColor, lerp);
        }
    }
}