using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class TntEntity : MonoBehaviour
{
    [SerializeField]
    private float effectRadius = 10f;
    PhotonView pv;
    Transform player;
    public Renderer rend;
    public Material originalMat;
    public Material whiteMat;

    public int blinkCount = 5;
    public float startInterval = 1.2f;
    public float intervalMultiplier = 0.6f;
    public ParticleSystem particleSystem;

    
    // Start is called before the first frame update
    void Start()
    {
        // in Spawned
        Transform player = FindMyPlayerTransform(); 
        StartCoroutine(BlinkRoutine());
        pv = GetComponent<PhotonView>();
    }

    IEnumerator BlinkRoutine()
    {
        float interval = startInterval;

        for (int i = 0; i < blinkCount; i++)
        {
            rend.material = whiteMat;
            yield return new WaitForSeconds(interval);

            rend.material = originalMat;
            yield return new WaitForSeconds(interval);

            interval *= intervalMultiplier;
        }

        TriggerEffect();
        if (!pv.IsMine) yield break;

        yield return new WaitForSeconds(1f);
        PhotonNetwork.Destroy(gameObject);
    }

    void TriggerEffect()
    {
        if (!pv.IsMine) return;
        pv.RPC(
            nameof(RpcTriggerEffect),
            RpcTarget.All
        );
    }


    Transform FindMyPlayerTransform()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();

        for (int i = 0; i < players.Length; i++)
        {
            PhotonView pv = players[i].GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return players[i].transform;
            }
        }

        return null;
    }

    [PunRPC]
    void RpcTriggerEffect()
    {
        ApplyEffectLocal();
    }

    void ApplyEffectLocal()
    {
        particleSystem?.Play();
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist > effectRadius) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float force =( effectRadius - dist) * 0.5f;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.AddForce(dir * force, ForceMode.Impulse);
    }

}
