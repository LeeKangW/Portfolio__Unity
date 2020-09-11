using UnityEngine;
using charac_anim = enum_Character.Anims;
using charac_state = enum_Character.States;

public class Player : Character
{
    /** 상호작용 시, 상호작용되어진 오브젝트가 캐릭터에 자식으로 붙는 위치 */

    [SerializeField]
    private Transform interActionPoint = null;

    /** 캐릭터 발끝 확인 ( Physics2D.OverlapCircle 용 ) */

    [SerializeField]
    private Transform feetTrans = null;

    /** Gizmo 용 */

    [Range(0.1f, 5.0f)]
    [SerializeField]
    private float fCircleRadius = 1.0f;

    /**
     * 사용자 키 입력에 따른 출력
     * Left -> -1
     * Right -> +1
     * not Pressed -> 0
     * */

    [Header("Inputs")]
    [SerializeField]
    private float fMoveInput = 0.0f;

    /**
     * 캐릭터가 땅에 있는지 확인
     * true -> 땅에 있음
     * false -> 공중에 있음
     */

    [Header("Physics")]
    [SerializeField]
    private bool bIsGrounded = true;

    #region LayerMask

    /** 움직일 수 있는 오브젝트의 mask */

    [Header("LayerMask")]
    public LayerMask boxMask;

    /** Physics2D.OverlapCircle's layermask */
    private LayerMask layerMask = 0;

    #endregion LayerMask

    #region JumpSystem Field

    /** if User press spacebar, it is true. */
    private bool bPressSpace = false;
    /** if jump's anim started, it is true. */
    private bool bStartJumpAnim = false;
    /** Check Character is in the air */
    private bool bIsFalling = false;

    #endregion JumpSystem Field

    #region InterActionSystem Field

    /** 상호작용된 오브젝트의 원래 부모 값을 가짐( 상호작용을 끝낼 때, 오브젝트를 원래 부모에게 돌려주기 위해 사용 )*/
    private Transform originParent = null;
    /** 상호작용된 오브젝트를 저장 */
    private Transform interActingObject = null;

    /** 상호작용 중인지 체크
     * True : 상호작용 오브젝트에 접촉
     * False : 상호작용 오브젝트에서 떨어짐
     */
    private bool bCollisionInterActing = false;

    /** Collision에 닿고 난 후 상호작용을 했는지 확인 용도 
     * True -> 상호작용을 시도함.
     * False -> 상호작용을 하지 않고, Collision Exit을 부름
     */
    private bool bIsActiveInterActionButton = false;

    #endregion InterActionSystem Field

    protected override void Awake()
    {
        base.Awake();
        layerMask = (1 << LayerMask.NameToLayer("Ground")) + (1 << LayerMask.NameToLayer("InteractObject"));
    }

    /// <summary>
    /// 만약 state가 CanNotPlayed 이면,
    /// 플레이어가 캐릭터를 조종하지 못하게 막음.
    /// </summary>
    private void Update()
    {
        if (state.Equals(charac_state.CanNotPlayed))
            return;

        if (!state.Equals(charac_state.InterActing))
            JumpSystem();

        #region movement system ( Input )

        fMoveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
            bPressSpace = true;
        else if (Input.GetKeyUp(KeyCode.Space))
            bPressSpace = false;

        #endregion movement system ( Input )

        #region interaction system ( Input )

        if (Input.GetMouseButton(0))
        {
            // About Collision
            bIsActiveInterActionButton = true;
            ActivateInterActing();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // About Collision
            IsCancelInterActing();
        }

        #endregion interaction system ( Input )
    }

    /// <summary>
    /// 만약 state가 CanNotPlayed 이면,
    /// 플레이어가 캐릭터를 조종하지 못하게 막음.
    /// </summary>
    private void FixedUpdate()
    {
        if (state.Equals(charac_state.CanNotPlayed))
            return;

        MovementSystem();

        /** 캐릭터가 땅에 있는지 체크 */
        bIsGrounded = Physics2D.OverlapCircle(feetTrans.position, fCircleRadius, layerMask);
    }

    #region Movement System Functions

    /// <summary>
    /// movement system
    /// </summary>
    private void MovementSystem()
    {
        rigid.velocity = new Vector2(fMoveInput * speed, rigid.velocity.y);

        switch (fMoveInput)
        {
            case -1.0f:
                if (!bIsActiveInterActionButton && originParent == null) // 상호작용 동작 시, 뒤로 돌기 막음.
                {
                    trans.localScale = new Vector3(-Mathf.Abs(trans.localScale.x), trans.localScale.y, trans.localScale.z);
                    anim.SetBool(charac_anim.IsRunning.ToString(), true);
                }
                else // 상호작용 애니메이션 실행.
                {

                }
                break;

            case 0.0f:
                if (!bIsActiveInterActionButton && originParent == null) // 상호작용 동작 시, 뒤로 돌기 막음.
                {
                    anim.SetBool(charac_anim.IsRunning.ToString(), false);
                }
                else // 상호작용 애니메이션 실행.
                {

                }
                break;

            case 1.0f:
                if (!bIsActiveInterActionButton && originParent == null) // 상호작용 동작 시, 뒤로 돌기 막음.
                {
                    trans.localScale = new Vector3(Mathf.Abs(trans.localScale.x), trans.localScale.y, trans.localScale.z);
                    anim.SetBool(charac_anim.IsRunning.ToString(), true);
                }
                else // 상호작용 애니메이션 실행.
                {

                }
                break;
        }
    }

    /// <summary>
    /// 플레이어의 캐릭터 움직임 권한을 정함.
    /// </summary>
    /// <param name="rhs"> 권한 설정 </param>
    public void UserCanPlayed(ref charac_state rhs)
    {
        state = rhs;
    }

    #endregion Movement System Functions

    #region Jump System Functions

    /// <summary>
    /// Animation_event applied in any jumping animation
    /// </summary>
    public void AE_jump_Force()
    {
        rigid.velocity = Vector2.up * jumpSpeed;
        Invoke("SetStartJumpAnim", 0.5f);
    }

    private void SetStartJumpAnim()
    {
        bStartJumpAnim = false;
    }

    /// <summary>
    /// Animation_event applied in any landing animation
    /// </summary>
    public void AE_Jump_End()
    {
        bIsFalling = false;
    }

    /// <summary>
    /// Jump System
    /// </summary>
    private void JumpSystem()
    {
        if (bIsGrounded && bPressSpace && !bStartJumpAnim && !bIsFalling)
        {
            // AE system
            bStartJumpAnim = true;
            bIsFalling = true;
            // ==========

            if (fMoveInput.Equals(0.0f))
                anim.SetTrigger(charac_anim.PressJump.ToString());
            else
                anim.SetTrigger(charac_anim.PressRunningJump.ToString());

            anim.SetBool(charac_anim.IsJumping.ToString(), true);
        }

        if (bIsGrounded && !bStartJumpAnim)
        {
            anim.SetBool(charac_anim.IsJumping.ToString(), false);
        }
        else
        {
            anim.SetBool(charac_anim.IsJumping.ToString(), true);
        }
    }

    #endregion Jump System Functions

    #region InterAction Functions

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("InteractiveObject") && !bIsActiveInterActionButton)
        {
            bCollisionInterActing = true;
            interActingObject = collision.gameObject.transform;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("InteractiveObject"))
        {
            bCollisionInterActing = false;
            if(!bIsActiveInterActionButton)
                interActingObject = null;
        }
    }

    /// <summary>
    /// 상호작용 시, 상호작용 된 객체를 캐릭터 자식에 붙임.
    /// </summary>
    private void ActivateInterActing()
    {
        if (bCollisionInterActing && interActingObject)
        {
            originParent = interActingObject.parent.transform;
            interActingObject.parent = interActionPoint;
        }
    }
    /// <summary>
    /// 상호작용을 중지 시, 상호작용된 객체를 캐릭터 자식에서 제외시킴.
    /// </summary>
    private void IsCancelInterActing()
    {
        if (originParent)
        {
            interActingObject.parent = originParent;

            originParent = null;
            interActingObject = null;
        }
        bIsActiveInterActionButton = false;
    }

    #endregion InterAction Functions

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(feetTrans.position, fCircleRadius);
    }
}