using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class OutlineHighlight : MonoBehaviour
{
    [SerializeField] SpriteRenderer _renderer;

    [Header("페이드")]
    [Tooltip("값이 클수록 빠르게 켜지고 꺼짐")]
    [SerializeField] private float fadeSpeed = 8f;

    private static readonly int IntensityID = Shader.PropertyToID("_OutlineIntensity");

    
    private MaterialPropertyBlock _block;

    private float _currentIntensity = 0;
    private float _targetIntensity = 0;


    private void Awake()
    {
        _block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (Mathf.Approximately(_currentIntensity, _targetIntensity)) return;

        _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, fadeSpeed * Time.deltaTime);
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(IntensityID, _currentIntensity);
        _renderer.SetPropertyBlock(_block);
    }


    public void SetHighlight(bool isOn)
    {
        _targetIntensity = isOn ? 1f : 0f;
    }
}