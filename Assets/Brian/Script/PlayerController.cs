using UnityEngine;
using Photon.Pun;
using Cinemachine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveForce = 30f;
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float groundDrag = 3f;

    // jump
    [SerializeField] float jumpForce = 6f;
    [SerializeField] float groundCheckDistance = 0.2f;
    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody rb;
    PhotonView pv;
    Animator animator;
    CinemachineVirtualCamera vcam;

    // current move input (set by PlayerAction when BeatActionType.Move starts)
    Vector3 moveInput = Vector3.zero;

    // set when SetMoveInput receives a jump (worldDirection.y == 1)
    bool jumpRequested = false;

    public GameObject deathEffectPrefab;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        if (pv.IsMine)
        {
            vcam = FindObjectOfType<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = transform;
            }
        }
    }

    void FixedUpdate()
    {
        if (!pv.IsMine) return;

        // use moveInput set by PlayerAction instead of WASD
        if (moveInput.sqrMagnitude > 0f)
        {
            animator.SetBool("walking", true);
        }
        else
        {
            animator.SetBool("walking", false);
        }

        // handle jump request once
        if (jumpRequested)
        {
            if (IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                if (animator != null) animator.SetTrigger("jump");
            }
            jumpRequested = false;
        }

        Move(moveInput);
        if (IsGrounded()) ApplyDrag();
        ClampVelocity();
        RotateTowardsMovement();
    }

    // called by PlayerAction to provide movement direction (world-space)
    public void SetMoveInput(Vector3 worldDirection)
    {
        // interpret worldDirection.y == 1 as jump input
        if (worldDirection.y > 0.5f)
            jumpRequested = true;

        // only use horizontal components for continuous movement
        moveInput = new Vector3(worldDirection.x, 0f, worldDirection.z);
    }

    void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Vector3 force = direction * moveForce;
        rb.AddForce(force, ForceMode.Force);
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundMask);
    }

    void RotateTowardsMovement()
    {
        Vector3 vel = rb.velocity;
        vel.y = 0f;

        if (rb.velocity.sqrMagnitude < 0.0001f) 
        {
            rb.angularVelocity = Vector3.zero;
            rb.velocity = new Vector3(0f, 0f, 0f);
            return;
        }

        if (rb.velocity.y > -0.1f && rb.velocity.y < 0.1f && vel.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(vel.normalized);
        rb.MoveRotation(targetRot);
    }

    void ApplyDrag()
    {
        Vector3 vel = rb.velocity;
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);

        Vector3 drag = -horizontal * groundDrag * Time.fixedDeltaTime;
        rb.AddForce(drag, ForceMode.VelocityChange);
    }

    void ClampVelocity()
    {
        Vector3 v = rb.velocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);

        if (horizontal.magnitude > maxSpeed)
        {
            horizontal = horizontal.normalized * maxSpeed;
            rb.velocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }
    }

    public void PlayerDeath(GameObject killer)
    {
        vcam.Follow = killer.transform;
        GetComponent<PlayerAction>().enabled = false;
        GetComponent<PlayerLife>().enabled = false;
        GetComponent<PlayerController>().enabled = false;
        GameObject.Find("GameManager").GetComponent<BattleMaster>().ShowDeadUI();
    }
}
