using UnityEngine;

public class HitFeedback : MonoBehaviour
{
    private Renderer _renderer;
    private Color _originalColor;

    public Color hitColor = Color.red;
    public float flashTime = 0.1f;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
    }

    public void OnHit()
    {
        if (_renderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(Flash());
        }
    }

    private System.Collections.IEnumerator Flash()
    {
        _renderer.material.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        _renderer.material.color = _originalColor;
    }
}