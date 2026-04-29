using UnityEngine;

/// <summary>
/// 플레이어 애니메이션 전담 컴포넌트
/// PlayerMovement2D의 상태를 '읽기만' 해서 Animator 파라미터를 갱신
/// 같은 GameObject에 부착 권장
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerAnimator2D : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] PlayerMovement2D _movement;


    private static readonly int HashSpeed     = Animator.StringToHash("Speed");
    
    /*
    private static readonly int HashVelocityY = Animator.StringToHash("VelocityY");
    private static readonly int HashGrounded  = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump      = Animator.StringToHash("Jump");
    private static readonly int HashLand      = Animator.StringToHash("Land");
    

    private void OnEnable()
    {
        // 단발성 이벤트 구독
        _movement.OnJumped += HandleJumped;
        _movement.OnLanded += HandleLanded;
    }

    private void OnDisable()
    {
        _movement.OnJumped -= HandleJumped;
        _movement.OnLanded -= HandleLanded;
    }

    private void HandleJumped() => _anim.SetTrigger(HashJump);
    private void HandleLanded() => _anim.SetTrigger(HashLand);
    
    */
    
    private void Update()
    {
        // 지속 상태는 매 프레임 동기화
        _anim.SetFloat(HashSpeed, Mathf.Abs(_movement.VelocityX));
        //_anim.SetFloat(HashVelocityY, _movement.VelocityY);
        //_anim.SetBool(HashGrounded, _movement.IsGrounded);
    }


}
