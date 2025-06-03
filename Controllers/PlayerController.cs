// # System
using System.Collections;
using System.Collections.Generic;

// # UnityEninge
using UnityEngine;
using UnityEngine.Rendering;

// # Photon
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed                   = default;
    [SerializeField] private float groundDrag                  = default;
    [SerializeField] private float jumpForce                   = default;
    [SerializeField] private float jumpCooldown                = default;
    [SerializeField] private float airMultiplier               = default;
    [Space(10)]
    [SerializeField] private float smoothPosSpeed              = default;
    [SerializeField] private float smoothRotSpeed              = default;
    [SerializeField] private float teleprotDistance            = default;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = default;

    [Header("GroundCheck")]
    [SerializeField] private Transform groundCheckPosition   = null;
    [SerializeField] public float playerHeight               = default;
    [SerializeField] public LayerMask whatIsGround           = default;
    
    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = default;

    [Header("CheckPoint")]
    [SerializeField] private float respawnObjectCheckDistance = default;
    [SerializeField] private LayerMask respawnObjectLayer     = default;

    [Header("VFX")]
    [SerializeField] private GameObject checkPointVFX = null;

    [Header("Camera")]
    [SerializeField] private Transform cameraTarget = null;
    [SerializeField] private Transform orientation = null;

    //private 변수
    private float horisontalInput   = default;
    private float verticalInput     = default;
    private float animH             = default;
    private float animV             = default;

    private bool isFall = default;
    private bool grounded = default;
    private bool exitingSlope = default;
    private bool readyToJump = default;

    private Rigidbody rb      = null;
    private Animator anim     = null;
    private Camera mainCamera = null;

    private Vector3 moveDir     = default;
    private Vector3 forward     = default;

    private RaycastHit slopeHit = default;

    private Vector3 networkPos = default;
    private Quaternion networkRot = default;

    //프로퍼티
    public Transform CameraTarget => cameraTarget;

    //컴포넌트 초기화
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    //초기화
    private void Start()
    {
        GameManager.Instance.SetPlayerTransform(transform);

        if (!photonView.IsMine)
        {
            Destroy(rb);
        }

        Volume volume = GetComponentInChildren<Volume>();
        Light light = GetComponentInChildren<Light>();

        volume.enabled = photonView.IsMine;
        light.enabled = photonView.IsMine;

        mainCamera = Camera.main;
        mainCamera.GetComponent<CameraController>().target = cameraTarget;

        rb.freezeRotation = true;
        readyToJump = true;

        orientation = mainCamera.transform;
    }
    private void Update()
    {
        if (!GameManager.Instance.IsGameStart || GameManager.Instance.IsGameOver || GameManager.Instance.IsGameClear) return;

        if (photonView.IsMine)
        {
            if (horisontalInput == 0 && verticalInput == 0)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }

            grounded = Physics.Raycast(groundCheckPosition.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
            transform.forward = forward;

            PlayerInput();
            SpeedControl();

            if (grounded)
            {
                rb.drag = groundDrag;
            }
            else
            {
                rb.drag = 0;
            }

            if (Input.GetKey(jumpKey) && readyToJump && grounded)
            {
                Jump();
                StartCoroutine(Co_ResetJump());
                readyToJump = false;
            }

            SetAnim();
        }

        CheckRespawnObject();
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            if(GameManager.Instance.IsGameOver || GameManager.Instance.IsGameClear) return;

            MovePlayer();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPos, smoothPosSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRot, smoothRotSpeed * Time.fixedDeltaTime);

            if (Vector3.Distance(transform.position, networkPos) > teleprotDistance)
            {
                transform.position = networkPos;
            }
        }
    }

    //플레이어 입력
    private void PlayerInput()
    {
        horisontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        animH = Input.GetAxis("Horizontal");
        animV = Input.GetAxis("Vertical");
    }

    //플레이어 애니매이션 셋팅
    private void SetAnim()
    {
        if(grounded)
        {

            isFall = false;
        }
        else
        {
            isFall = true;
        }

        anim.SetBool("isFall", isFall);
        anim.SetFloat("h", animH);
        anim.SetFloat("v", animV);
    }

    //플레이어 이동
    private void MovePlayer()
    {
        forward = orientation.forward;
        forward.y = 0;
        moveDir = forward * verticalInput + orientation.right * horisontalInput;

        if (CheckSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
            rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        if (grounded)
        {
            rb.AddForce(moveDir.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDir.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
        rb.useGravity = !CheckSlope();
    }

    //플레이어의 속도를 관리
    private void SpeedControl()
    {
        if (CheckSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    //플레이어 점프
    private void Jump()
    {
        photonView.RPC("RPC_Jump", RpcTarget.All);

        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    //점프 상태를 초기화
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    [PunRPC]
    private void RPC_Jump()
    {
        SoundManager.instance.PlaySFX("Jump");
    }

    //경사면 위에 있는지 검사
    private bool CheckSlope()
    {
        if (Physics.Raycast(groundCheckPosition.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    //경사에서 움직일 방향을 계산
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
    }

    //체크포인트 오브젝트를 감지
    private void CheckRespawnObject()
    {
        Collider[] respawnObject;

        respawnObject = Physics.OverlapSphere(transform.position, respawnObjectCheckDistance, respawnObjectLayer);
        
        if(respawnObject.Length > 0)
        {
            ChangeRespawnPosition(respawnObject[0].transform, respawnObject[0]);
            
        }
    }

    private IEnumerator Co_ResetJump()
    {
        yield return new WaitForSeconds(jumpCooldown);
        ResetJump();
    }

    //리스폰 위치를 갱신
    private void ChangeRespawnPosition(Transform newSpawnPosition, Collider checkPoint)
    {
        EffectManager.Instance.PlayEffect(newSpawnPosition.position, "CheckPoint");
        GameManager.Instance.RespawnPostiton = newSpawnPosition;
        checkPoint.gameObject.SetActive(false);
    }

    //변수를 전송
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            networkPos = (Vector3)stream.ReceiveNext();
            networkRot = (Quaternion)stream.ReceiveNext();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (photonView.IsMine)
        {
            //물에 닿으면 게임오버
            if (other.CompareTag("Water"))
            {
                GameManager.Instance.GameOver();
            }

            //게임 클리어 문에 닿으면 게임 클리어
            if (other.CompareTag("GameClearDoor"))
            {
                GameManager.Instance.GameClear();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, respawnObjectCheckDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(groundCheckPosition.position, Vector3.down * (playerHeight * 0.5f + 0.2f));
    }
}