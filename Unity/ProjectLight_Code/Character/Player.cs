using System.Collections;
using UnityEngine;
using charac_anim = ENUM_Character.Anims;
using charac_PBAnim = ENUM_Character.PushBackAnims;
using charac_state = ENUM_Character.States;
using charac_interpos = ENUM_Character.InterActionPos;

public class Player : CharacterParents
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

    /**
    * 콜리전 상호작용 시(밀고 당기기), 속도 조절
    * @Tips : 일반 속도보다 낮출 예정   
    * */

    [Header("InterActions")]
    [SerializeField] private float InterActionSpeed = 2.4f;

    /** Collision으로 상호작용된 오브젝트의 원래 부모 값을 가짐( 상호작용을 끝낼 때, 오브젝트를 원래 부모에게 돌려주기 위해 사용 )*/
    private Transform originCollisionParent = null;
    /** Collision으로 상호작용된 오브젝트를 저장 */
    private Transform interActingCollisionObject = null;
    /** Trigger로 상호작용된 오브젝트의 스크립트를 저장 */
    private InterActionObjectsWithTriggerParent interActionObjectScript = null;

    private WaitUntil interActingWaitUntill = null;

    /** 코루틴 실행 중, StopCoroutine이 호출 될 경우, 작업이 멈추게 되는 것을 방지하기 위해 사용 */
    private bool startCoroutine = false;

    /** 상호작용 애니메이션 실행 중, 준비자세 들어갈 때, 이동을 방지 */
    private bool bwaitInterActingAnim = false;

    private Coroutine interActingCoroutine = null;

    /** 상호작용 중인지 체크
     * True : 상호작용 오브젝트에 접촉
     * False : 상호작용 오브젝트에서 떨어짐
     */
    private bool bInterActing = false;

    /** Collision or Trigger에 닿고 난 후 상호작용 키를 눌렀는지 확인 용도
     * True -> 상호작용을 시도함.
     * False -> 상호작용을 하지 않고, Collision Exit을 부름
     */
    private bool bIsActiveInterActionButton = false;

    private charac_interpos interActionPos = charac_interpos.Default;

    #endregion InterActionSystem Field

    protected override void Awake()
    {
        base.Awake();
        layerMask = (1 << LayerMask.NameToLayer("Ground")) + (1 << LayerMask.NameToLayer("InteractObject"));

        interActingWaitUntill = new WaitUntil(() => interActionObjectScript && bIsActiveInterActionButton && !bIsFalling);
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
            if (interActionObjectScript || interActingCollisionObject)
            {
                if (interActingCollisionObject)
                {
                    /** About InterAction Anims */
                    if (!bIsActiveInterActionButton)
                    {
                        anim.SetTrigger(charac_PBAnim.PressPushBack.ToString());
                    }
                    anim.SetBool(charac_PBAnim.PressInterActionKey.ToString(), bIsActiveInterActionButton);
                }
                bIsActiveInterActionButton = true;
            }
            ActivateCollisionInterActing();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // About Collision
            IsCancelInterActing();

            /** About InterAction Anims */
            anim.SetBool(charac_PBAnim.PressInterActionKey.ToString(), bIsActiveInterActionButton);
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
        switch (fMoveInput)
        {
            case -1.0f:
                SetCharacterSpeedAndLocalScale(ref fMoveInput);
                if (interActionPos == charac_interpos.Left)
                {
                    anim.SetInteger(charac_anim.ValueInput.ToString(), -1 * (int)fMoveInput);
                }
                else
                {
                    anim.SetInteger(charac_anim.ValueInput.ToString(), (int)fMoveInput);
                }
                break;

            case 0.0f:
                anim.SetInteger(charac_anim.ValueInput.ToString(), (int)fMoveInput);
                break;

            case 1.0f:
                SetCharacterSpeedAndLocalScale(ref fMoveInput);
                if (interActionPos == charac_interpos.Left)
                {
                    anim.SetInteger(charac_anim.ValueInput.ToString(), -1 * (int)fMoveInput);
                }
                else
                {
                    anim.SetInteger(charac_anim.ValueInput.ToString(), (int)fMoveInput);
                }
                break;
        }
    }

    /// <summary>
    /// MovementSystem 함수에서 case문 안에 사용
    /// </summary>
    /// <param name="localScale"></param>
    private void SetCharacterSpeedAndLocalScale(ref float localScale)
    {
        if (bwaitInterActingAnim) return;

        if (bIsActiveInterActionButton) // 상호작용 키 클릭 시
        {
            if (interActingCollisionObject) // 상호작용 오브젝트가 있을 때
            {
                rigid.velocity = new Vector2(fMoveInput * InterActionSpeed, rigid.velocity.y);
                trans.localScale = new Vector3(trans.localScale.x, trans.localScale.y, trans.localScale.z);
            }
            else // 상호작용 오브젝트가 없을 때
            {
                rigid.velocity = new Vector2(fMoveInput * speed, rigid.velocity.y);
                trans.localScale = new Vector3(localScale * Mathf.Abs(trans.localScale.x), trans.localScale.y, trans.localScale.z);
            }
        }
        else // 상호작용 키 사용하지 않을 때
        {
            rigid.velocity = new Vector2(fMoveInput * speed, rigid.velocity.y);
            trans.localScale = new Vector3(localScale * Mathf.Abs(trans.localScale.x), trans.localScale.y, trans.localScale.z);
        }
    }

    /// <summary>
    /// 플레이어의 캐릭터 움직임 권한을 정함.
    /// </summary>
    /// <param name="rhs"> 권한 설정 </param>
    public void SetUserState(charac_state rhs)
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

    #region Trigger InterActing System Functions

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("InteractiveObject") && !bIsActiveInterActionButton)
        {
            // 상호작용(트리거용) 오브젝트 안에 있는 함수 실행.
            interActingCoroutine = StartCoroutine(ActivateTriggerInterActing());
            interActionObjectScript = collision.gameObject.GetComponent<InterActionObjectsWithTriggerParent>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("InteractiveObject"))
        {
            if (!startCoroutine)
            {
                if (interActingCoroutine != null)
                    StopCoroutine(interActingCoroutine);

                interActionObjectScript = null;
                interActingCoroutine = null;
            }
        }
    }

    /// <summary>
    /// Trigger로 작동하는 오브젝트 내부 로직을 실행
    /// </summary>
    private IEnumerator ActivateTriggerInterActing()
    {
        yield return interActingWaitUntill;

        startCoroutine = true;

        /** 상호작용 오브젝트 내에 구현된 ActivateSystem 함수 실행 */
        interActionObjectScript.ActivateSystemWithTrigger();

        /** 코루틴 종료로 다시 초기화 */
        interActionObjectScript = null;
        startCoroutine = false;
    }

    #endregion Trigger InterActing System Functions

    #region Collision InterActing System Functions

    private charac_interpos IsCharacterStandOnCollider(Transform trans)
    {
        Vector2 charVec = this.gameObject.transform.position;
        Vector2 ColliderVec = trans.position;

        Vector2 vector2 = charVec - ColliderVec;

        float degree = Mathf.Atan2(vector2.y, vector2.x) * Mathf.Rad2Deg;

        /** degree 가 양수면 위에 서있고, 음수면 동일선상에 있음. */
        if (degree < 0.0f)
        {
            if (degree > -90.0f)
            {
                return charac_interpos.Left;
            }
            if (degree < -90.0f)
            {
                return charac_interpos.Right;
            }
        }
        return charac_interpos.CantInterAction;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("InteractiveObject") && !bIsActiveInterActionButton)
        {
            interActionPos = IsCharacterStandOnCollider(collision.gameObject.transform);
            /** 캐릭터가 콜라이더 위에 있으면 상호작용 불가 */
            if (interActionPos != charac_interpos.CantInterAction)
            {
                bInterActing = true;
                interActingCollisionObject = collision.gameObject.transform;

                InterActionSpeed =  collision.gameObject.GetComponent<InterActionObjectsWithCollision>().MoveSpeed;
                GetComponent<Animator>().SetFloat("InterActionSpeed", collision.gameObject.GetComponent<InterActionObjectsWithCollision>().AnimSpeed);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("InteractiveObject"))
        {
            bInterActing = false;
            if (!bIsActiveInterActionButton)
                interActingCollisionObject = null;
        }
    }

    /// <summary>
    /// 콜리전 상호작용 시, 상호작용 된 객체를 캐릭터 자식에 붙임.
    /// </summary>
    private void ActivateCollisionInterActing()
    {
        if (bInterActing && !bIsFalling && interActingCollisionObject)
        {
            /** 상호작용 상태로 변경 */
            SetUserState(charac_state.InterActing);

            /** 상호작용 오브젝트의 부모가 없을 때를 대비한 예외 처리 */
            if (originCollisionParent == null)
            {
                originCollisionParent = interActingCollisionObject.parent;
            }

            interActingCollisionObject.SetParent(interActionPoint);
        }
    }

    /// <summary>
    /// 상호작용을 중지 시, 상호작용된 객체를 캐릭터 자식에서 제외시킴.
    /// </summary>
    private void IsCancelInterActing()
    {
        if (interActingCollisionObject)
        {
            /** 플레이 상태로 변경 */
            SetUserState(charac_state.CanPlayed);

            /** 상호작용 오브젝트의 부모가 없을 때를 대비한 예외 처리 */
            if (originCollisionParent)
            {
                if (originCollisionParent == interActionPoint) // 만약 캐릭터에 세팅되어 있는 오브젝트와 같다면...
                {
                    interActingCollisionObject.SetParent(null);
                }
                else
                {
                    interActingCollisionObject.SetParent(originCollisionParent);
                }
            }
            else
            {
                interActingCollisionObject.SetParent(null);
            }
            interActingCollisionObject = null;
            originCollisionParent = null;
        }
        bIsActiveInterActionButton = false;
    }

    /// <summary>
    /// 상호작용 ( 밀기,당기기 ) 애님 시, 준비자세가 끝나고
    /// 실제 애니메이션이 나올 때까지 대기시켜주는 코드.
    /// 
    /// PushBack_Ready 애니메이션 안에 적용되어 있음.
    /// </summary>
    /// <param name="isWait"></param>
    public void AE_SetWaitInterActionAnim(int isWait)
    {
        if (isWait == 1)
        {
            bwaitInterActingAnim = true;
        }
        else
        {
            bwaitInterActingAnim = false;
        }
    }
    #endregion Collision InterActing System Functions

    #endregion InterAction Functions

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(feetTrans.position, fCircleRadius);
    }
}