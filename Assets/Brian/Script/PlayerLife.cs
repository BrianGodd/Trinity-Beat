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

    // damage cooldown (seconds)
    [Header("Damage")]
    [Tooltip("Cooldown in seconds during which the player cannot take further damage after being hit.")]
    [SerializeField] float damageCooldown = 0.8f;

    PhotonView pv;
    Animator animator;

    public int currentLife;
    
    public float slowMotionScale = 0.2f;

    public bool isDebug = false;

    // timestamp of last time this player locally caused a damage RPC (used for cooldown)
    float lastDamageTime = -Mathf.Infinity;

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
                RequestChangeLife(-20); // take 20 damage
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                RequestChangeLife(20); // heal 20
            }
        }
    }

    // Public API — call this to request a life change (damage/heal).
    // This will be sent as an RPC and applied on all clients.
    public void RequestChangeLife(int amount)
    {
        if (pv == null) pv = GetComponent<PhotonView>();

        // Only apply local cooldown check for damage requests initiated by the local owner.
        if (amount < 0 && pv != null && pv.IsMine)
        {
            if (Time.time - lastDamageTime < damageCooldown)
            {
                // still in cooldown, ignore this damage
                return;
            }
            // mark last damage time so subsequent hits are blocked for cooldown duration
            lastDamageTime = Time.time;
        }

        // send RPC to apply change on all clients
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

        // trigger visual/audio feedback only on the local instance of the affected player
        if (pv != null && pv.IsMine)
        {
            if (amount < 0)
                PlayHitEffect(-amount);
            else if (amount > 0)
                PlayHealEffect(amount);
        }

        UpdateLifeSegments();

        if (currentLife <= 0 && old > 0)
            HandleDeath(info.Sender);
    }

    void PlayHitEffect(int damage)
    {
        if (animator != null) animator.SetTrigger("hit");
        // additional local-only effects (sound, screen shake) can be placed here
        StartCoroutine(SlowMotionEffect());
    }

    IEnumerator SlowMotionEffect()
    {
        var mainCam = GameObject.Find("MainCam");
        var virtualCam = GameObject.Find("VirtualCam");
        if (mainCam != null) mainCam.GetComponent<Animator>()?.Play("VolumeActive");
        if (virtualCam != null) virtualCam.GetComponent<Animator>()?.Play("MovingCamera");

        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
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
            GetComponent<PlayerController>()?.PlayerDeath(killer?.TagObject as GameObject);
        }
        else
        {
            Debug.Log($"{gameObject.name} died.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!pv.IsMine) return;

        if (other.CompareTag("Damage"))
        {
            // ignore damage if it's from our own spawned object (same Photon owner)
            PhotonView dmgPv = other.GetComponent<PhotonView>() ?? other.GetComponentInParent<PhotonView>();
            if (dmgPv != null && pv != null && pv.Owner != null && dmgPv.Owner != null && dmgPv.Owner == pv.Owner)
                return;

            RequestChangeLife(-20);
        }
    }

    // utility getters
    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;
}
