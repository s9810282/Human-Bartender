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

    private float _currentIntensity;
    private float _targetIntensity;

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

    // --- 외부 호출용 함수 ---

    /// <summary>
    /// 아웃라인을 켭니다.
    /// </summary>
    public void EnableHighlight()
    {
        _targetIntensity = 1f;
    }

    /// <summary>
    /// 아웃라인을 끕니다.
    /// </summary>
    public void DisableHighlight()
    {
        _targetIntensity = 0f;
    }

    /// <summary>
    /// bool 값을 통해 아웃라인 상태를 켜거나 끕니다.
    /// </summary>
    /// <param name="isOn">true면 켜짐, false면 꺼짐</param>
    public void SetHighlight(bool isOn)
    {
        _targetIntensity = isOn ? 1f : 0f;
    }
}