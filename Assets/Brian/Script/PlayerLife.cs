using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class PlayerLife : MonoBehaviour
{
    [Header("Life")]
    [SerializeField] int maxLife = 100;
    [SerializeField] int startLife = 100;

    [Header("Visuals (optional)")]
    [SerializeField] GameObject hitEffectPrefab;
    [SerializeField] GameObject healEffectPrefab;

    [Header("Life Segments (5 objects)")]
    [Tooltip("Assign 5 GameObjects representing life segments (1..5).")]
    [SerializeField] GameObject[] lifeSegments = new GameObject[5];

    PhotonView pv;
    Animator animator;

    public int currentLife;

    public bool isDebug = false;

    // event invoked when life changes: (newLife, oldLife)
    public event System.Action<int,int> OnLifeChanged;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();
        currentLife = Mathf.Clamp(startLife, 0, maxLife);
        UpdateLifeSegments();
    }

    void Update()
    {
        // debug/testing only: local input to damage/heal
        if (pv != null && pv.IsMine && isDebug)
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                RequestChangeLife(-20); // take 10 damage
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                RequestChangeLife(20); // heal 10
            }
        }
    }

    // Public API — call this to request a life change (damage/heal).
    // This will be sent as an RPC and applied on all clients.
    public void RequestChangeLife(int amount)
    {
        if (pv == null) pv = GetComponent<PhotonView>();
        // use AllBuffered so late joiners get current state when you rework persistence; change to All if unwanted
        pv.RPC(nameof(RPC_ChangeLife), RpcTarget.All, amount);
    }

    // apply change on all clients
    [PunRPC]
    void RPC_ChangeLife(int amount, PhotonMessageInfo info)
    {
        int old = currentLife;
        currentLife = Mathf.Clamp(currentLife + amount, 0, maxLife);

        // notify listeners
        OnLifeChanged?.Invoke(currentLife, old);

        // trigger visual/audio feedback locally on this object
        if (amount < 0)
            PlayHitEffect(-amount);
        else if (amount > 0)
            PlayHealEffect(amount);

        UpdateLifeSegments();

        if (currentLife <= 0 && old > 0)
            HandleDeath(info.Sender);
    }

    void PlayHitEffect(int damage)
    {
        if (animator != null) animator.SetTrigger("hit");
        // additional local-only effects (sound, screen shake) can be placed here
    }

    void PlayHealEffect(int amount)
    {
        if (animator != null) animator.SetTrigger("heal");
        if (healEffectPrefab != null) Instantiate(healEffectPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
    }

    void UpdateLifeSegments()
    {
        if (lifeSegments == null || lifeSegments.Length == 0) return;

        float pct = (maxLife > 0) ? (currentLife / (float)maxLife) : 0f;

        int activeCount = 0;
        if (pct > 0.8f) activeCount = 5;
        else if (pct > 0.6f) activeCount = 4;
        else if (pct > 0.4f) activeCount = 3;
        else if (pct > 0.2f) activeCount = 2;
        else if (pct > 0f) activeCount = 1;
        else activeCount = 0;

        // ensure we don't exceed array length
        activeCount = Mathf.Clamp(activeCount, 0, lifeSegments.Length);

        for (int i = 0; i < lifeSegments.Length; i++)
        {
            var go = lifeSegments[i];
            if (go == null) continue;
            // segments expected ordered 0..4 => seg1..seg5
            go.SetActive(i < activeCount);
        }
    }

    void HandleDeath(Photon.Realtime.Player killer)
    {
        // local reaction to death (run once per object when life hits zero)
        if (animator != null) animator.SetTrigger("dead");

        // example: if this is local player's object, you might want to notify GameManager, respawn, etc.
        if (pv != null && pv.IsMine)
        {
            // TODO: insert local-player death handling (respawn, disable input, etc.)
            Debug.Log($"You died. Killer: {(killer != null ? killer.NickName : "unknown")}");
        }
        else
        {
            Debug.Log($"{gameObject.name} died.");
        }
    }

    // utility getters
    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;
}
