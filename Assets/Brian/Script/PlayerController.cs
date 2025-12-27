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

    Rigidbody rb;
    PhotonView pv;
    Animator animator;
    CinemachineVirtualCamera vcam;

    public GameObject UISurface;

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

        Vector3 input = GetWASDInput();

        if(input.magnitude > 0f)
        {
            animator.SetBool("walking", true);
        }
        else
        {
            animator.SetBool("walking", false);
        }
        
        UISurface.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        Move(input);
        ApplyDrag();
        ClampVelocity();
        RotateTowardsMovement();
    }

    void LateUpdate()
    {
        if (!pv.IsMine) return;

        UISurface.transform.rotation = Quaternion.identity;
    }

    Vector3 GetWASDInput()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;

        Vector3 dir = new Vector3(x, 0f, z);
        return dir.normalized;
    }

    void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Vector3 force = direction * moveForce;
        rb.AddForce(force, ForceMode.Force);
    }

    void RotateTowardsMovement()
    {
        Vector3 vel = rb.velocity;
        vel.y = 0f;

        if (vel.sqrMagnitude < 0.1f) return;

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
}
