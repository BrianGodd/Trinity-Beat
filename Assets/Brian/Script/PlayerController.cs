using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveForce = 30f;
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float groundDrag = 3f;

    Rigidbody rb;
    PhotonView pv;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();
        // optional: for remote objects you might want to freeze rotation so network interpolation is stable
        rb.freezeRotation = true;
    }

    void Update()
    {
        // nothing here - input handled in FixedUpdate for physics
    }

    void FixedUpdate()
    {
        // allow control if there's no PhotonView (single player) or this client owns the PhotonView
        if (pv != null && !pv.IsMine) return;

        Vector3 input = GetWASDInput();
        Move(input);
        ApplyDrag();
        ClampVelocity();
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
        // move relative to the player's orientation
        Vector3 force = transform.TransformDirection(direction) * moveForce;
        rb.AddForce(force, ForceMode.Force);
    }

    void ApplyDrag()
    {
        // simple ground drag to help stopping
        Vector3 vel = rb.velocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
        Vector3 drag = -horizontalVel * groundDrag * Time.fixedDeltaTime;
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
