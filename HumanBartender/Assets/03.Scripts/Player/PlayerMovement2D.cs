using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unity 6 - 2D 횡스크롤 플레이어 (이동만 담당)
/// 애니메이션은 PlayerAnimator2D가 이 스크립트의 상태를 읽어 처리
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("충돌")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float skinWidth = 0.02f;

    [SerializeField] BoxCollider2D _col;

    // 입력
    private float _moveInput;

    // 속도
    private float _velocityX;
    private float _velocityY;

    // 외부 영향 (엘리베이터, 이동 플랫폼, 컨베이어 벨트 등)
    // 이번 프레임에 외부 시스템이 더해줄 이동량. 매 프레임 누적되고, 적용 후 리셋된다.
    private Vector2 _externalDelta;

    // 상태
    private bool _isGrounded;
    private bool _isFacingRight = true;

    public float VelocityX => _velocityX;
    public float VelocityY => _velocityY;
    public bool  IsGrounded => _isGrounded;
    public bool  IsFacingRight => _isFacingRight;
    public bool  IsMoving => Mathf.Abs(_moveInput) > 0.01f;


    private void Awake()
    {
        
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>().x;
    public void HandleExternalDelta(Vector2 delta) => _externalDelta += delta;

    private void Update()
    {
        _velocityX = _moveInput * moveSpeed;

       
        MoveWithCollision();
        HandleFlip();
    }

    private void MoveWithCollision()
    {
        Vector2 origin = (Vector2)transform.position + _col.offset;
        Vector2 move = new Vector2(_velocityX, _velocityY) * Time.deltaTime + _externalDelta;
        Vector2 castSize = _col.size - Vector2.one * skinWidth * 2f;

        _externalDelta = Vector2.zero;

        if (move.x != 0f)
        {
            RaycastHit2D hit = Physics2D.BoxCast(origin, castSize, 0f,
                                Vector2.right * Mathf.Sign(move.x),
                                Mathf.Abs(move.x) + skinWidth, groundLayer);
            if (hit)
            {
                move.x = (hit.distance - skinWidth) * Mathf.Sign(move.x);
                _velocityX = 0f;
            }
        }

        origin.x += move.x;

        if (move.y != 0f)
        {
            RaycastHit2D hit = Physics2D.BoxCast(origin, castSize, 0f,
                                Vector2.up * Mathf.Sign(move.y),
                                Mathf.Abs(move.y) + skinWidth, groundLayer);
            if (hit)
            {
                move.y = (hit.distance - skinWidth) * Mathf.Sign(move.y);
                _velocityY = 0f;
            }
        }

        bool wasGrounded = _isGrounded;
        Vector2 footPos = (Vector2)transform.position + _col.offset
                          + Vector2.down * (_col.size.y / 2f);
        Vector2 footSize = new Vector2(_col.size.x * 0.9f, skinWidth * 2f);
        _isGrounded = Physics2D.OverlapBox(footPos, footSize, 0f, groundLayer);

        transform.Translate(move);
    }

    private void HandleFlip()
    {
        if (_moveInput > 0.01f && !_isFacingRight) Flip();
        else if (_moveInput < -0.01f && _isFacingRight) Flip();
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    private void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<BoxCollider2D>();
        Vector2 footPos = (Vector2)transform.position + _col.offset
                          + Vector2.down * (_col.size.y / 2f);
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(footPos, new Vector2(_col.size.x * 0.9f, skinWidth * 2f));
    }
}
