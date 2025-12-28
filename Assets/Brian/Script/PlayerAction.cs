using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerAction : MonoBehaviour
{
    [Header("Spawn Prefab")]
    public GameObject magic;

    [Header("UI")]
    public GameObject UISurface;
    public TextMeshProUGUI actionText;
    TypingSync typingSync;

    public ComboCaster comboCaster;
    public BeatClock beatClock;
    public SkillManager skillManager;
    PhotonView pv;
    PlayerController playerController;
    PlayerLife playerLife;

    Rigidbody rb;
    Animator animator;

    public ComboRecorder.ComboData LastCombo { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        comboCaster = FindObjectOfType<ComboCaster>();
        beatClock = FindObjectOfType<BeatClock>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        typingSync = GetComponent<TypingSync>();
        playerLife = GetComponent<PlayerLife>();
        playerController = GetComponent<PlayerController>();
        skillManager = FindObjectOfType<SkillManager>();

        if (comboCaster != null && comboCaster.comboRecorder != null)
        {
            comboCaster.comboRecorder.OnComboReady += OnComboReady;
        }
    }

    void OnDisable()
    {
        if (comboCaster != null && comboCaster.comboRecorder != null)
        {
            comboCaster.comboRecorder.OnComboReady -= OnComboReady;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        if (!pv.IsMine) return;

        UISurface.transform.rotation = Quaternion.identity;
    }

    // handler invoked when the ComboRecorder fires a combo ready event
    void OnComboReady(ComboRecorder.ComboData combo)
    {
        // only react for the local player
        if (pv != null && !pv.IsMine) return;

        LastCombo = combo;

        if (!skillManager.SkillDetection(combo.WordString))
        {
            StartCoroutine(PerformActionCoroutine(combo.hits));
        }
        else
        {
            StartCoroutine(LateInitTypingSync());
        }
    }
    IEnumerator LateInitTypingSync()
    {
        float beatDur = (beatClock != null) ? (float)beatClock.BeatDurationSec : 0.5f;

        beatDur *= 3;
        yield return new WaitForSeconds(beatDur);

        typingSync.InitWord();
    }
    IEnumerator PerformActionCoroutine(ComboRecorder.Hit[] hits)
    {
        float beatDur = (beatClock != null) ? (float)beatClock.BeatDurationSec : 0.5f;

        // process each hit in the combo sequentially, keeping the action for one beat
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];

            if (hit.hasInput)
            {
                Debug.Log($"Performing action: Type={hit.type}, Dir={hit.dir}, Glyph='{hit.glyph}'");

                // only perform actions for the local player
                if (pv != null && !pv.IsMine) yield break;

                // apply the action immediately
                ApplyActionStart(hit);

                // hold the action for the beat duration
                yield return new WaitForSeconds(beatDur);

                // stop / revert the action
                ApplyActionStop(hit);
            }
            else
            {
                // if miss, optionally wait a beat so timing stays consistent
                yield return new WaitForSeconds(beatDur);
            }
        }
        typingSync.InitWord();
        playerLife.SetParryDirection(Vector3.zero);
    }

    void ApplyActionStart(ComboRecorder.Hit hit)
    {
        // parse incoming type/dir into enums used by InputBindingMap
        BeatActionType actionType = ParseActionType(hit.type);
        BeatDirection9 dir9 = ParseDirection9(hit.dir);

        ExecuteActionStart(actionType, dir9, hit);
    }

    void ApplyActionStop(ComboRecorder.Hit hit)
    {
        BeatActionType actionType = ParseActionType(hit.type);
        BeatDirection9 dir9 = ParseDirection9(hit.dir);

        ExecuteActionStop(actionType, dir9, hit);
    }

    // execute start behavior for action types
    void ExecuteActionStart(BeatActionType action, BeatDirection9 dir9, ComboRecorder.Hit hit)
    {
        Vector3 dirVec = DirectionFromBeatDirection9(dir9);

        Debug.Log($"Executing action : {dir9} towards {dirVec}");

        switch (action)
        {
            case BeatActionType.Move:
                // now delegate movement to PlayerController (so movement only comes from beat actions)
                if (pv != null && !pv.IsMine) break;
                if (playerController != null)
                {
                    // pass world-space direction to controller
                    playerController.SetMoveInput(dirVec);
                }
                if (animator != null)
                    animator.SetBool("walking", true);
                playerLife.SetParryDirection(Vector3.zero);
                break;

            case BeatActionType.Attack:
                // attack animation + small forward lunge
                if (animator != null)
                    animator.SetTrigger("attack");
                // network spawn magic prefab in front of player
                if (pv != null && pv.IsMine && magic != null)
                {
                    Vector3 spawnPos = transform.position + dirVec.normalized * 15.0f + Vector3.up * 1.0f;
                    Quaternion spawnRot = Quaternion.LookRotation(dirVec, Vector3.up);

                    // pass owner viewID as instantiation data so all clients receive it
                    object[] instData = new object[] { pv != null ? pv.ViewID : -1 };
                    GameObject skillObj = PhotonNetwork.Instantiate(magic.name, spawnPos, spawnRot, 0, instData);

                    // optional: set locally if needed immediately
                    if (skillObj != null)
                    {
                        Skill skillComp = skillObj.GetComponent<Skill>();
                        if (skillComp != null)
                            skillComp.ownerViewID = (pv != null) ? pv.ViewID : -1;
                    }

                    // destroy after some time
                    StartCoroutine(DestroyAfter(skillObj));
                }
                playerLife.SetParryDirection(Vector3.zero);
                break;

            case BeatActionType.Parry:
                // parry animation / temporary state
                if (animator != null)
                    animator.SetTrigger("parry");
                // optionally set a parry flag / enable parry collider (not implemented here)
                playerLife.SetParryDirection(dirVec);
                break;
        }
    }

    IEnumerator DestroyAfter(GameObject obj)
    {
        yield return new WaitForSeconds(1f);
        if (obj != null)
            PhotonNetwork.Destroy(obj);
    }

    // revert or stop effects started above
    void ExecuteActionStop(BeatActionType action, BeatDirection9 dir9, ComboRecorder.Hit hit)
    {
        switch (action)
        {
            case BeatActionType.Move:
                // stop movement input from PlayerAction
                if (pv != null && !pv.IsMine) break;
                if (playerController != null)
                {
                    playerController.SetMoveInput(Vector3.zero);
                }
                if (animator != null)
                    animator.SetBool("walking", false);
                // dampen horizontal velocity (controller will also do this when stopping)
                if (rb != null)
                {
                    Vector3 v = rb.velocity;
                    rb.velocity = new Vector3(v.x * 0.3f, v.y, v.z * 0.3f);
                }
                break;

            case BeatActionType.Attack:
                // nothing special to revert by default; could reset animator states if needed
                break;

            case BeatActionType.Parry:
                // end parry state
                break;
        }
    }

    // parse generic hit.type into BeatActionType
    BeatActionType ParseActionType(object typeObj)
    {
        if (typeObj == null) return BeatActionType.Move;

        if (typeObj is BeatActionType at) return at;

        string s = typeObj.ToString().ToLower();
        if (s.Contains("attack") || s.Contains("atk")) return BeatActionType.Attack;
        if (s.Contains("parry")) return BeatActionType.Parry;
        if (s.Contains("move")) return BeatActionType.Move;

        // try enum parse / numeric
        if (System.Enum.TryParse(typeof(BeatActionType), typeObj.ToString(), true, out var parsed))
            return (BeatActionType)parsed;

        if (int.TryParse(s, out int idx) && System.Enum.IsDefined(typeof(BeatActionType), idx))
            return (BeatActionType)idx;

        return BeatActionType.Move;
    }

    // parse generic hit.dir into BeatDirection9
    BeatDirection9 ParseDirection9(object dirObj)
    {
        if (dirObj == null) return BeatDirection9.Center;

        if (dirObj is BeatDirection9 b9) return b9;

        if (dirObj is int i && System.Enum.IsDefined(typeof(BeatDirection9), i))
            return (BeatDirection9)i;

        string s = dirObj.ToString().ToLower();
        if (s == "center" || s == "c") return BeatDirection9.Center;
        if (s == "n" || s == "north" || s == "up" || s == "forward") return BeatDirection9.N;
        if (s == "ne" || s == "northeast") return BeatDirection9.NE;
        if (s == "e" || s == "east" || s == "right") return BeatDirection9.E;
        if (s == "se" || s == "southeast") return BeatDirection9.SE;
        if (s == "s" || s == "south" || s == "down" || s == "back") return BeatDirection9.S;
        if (s == "sw" || s == "southwest") return BeatDirection9.SW;
        if (s == "w" || s == "west" || s == "left") return BeatDirection9.W;
        if (s == "nw" || s == "northwest") return BeatDirection9.NW;

        if (System.Enum.TryParse(typeof(BeatDirection9), dirObj.ToString(), true, out var parsed))
            return (BeatDirection9)parsed;

        if (int.TryParse(s, out int idx) && System.Enum.IsDefined(typeof(BeatDirection9), idx))
            return (BeatDirection9)idx;

        return BeatDirection9.Center;
    }

    // convert BeatDirection9 to world direction (relative to player transform)
    Vector3 DirectionFromBeatDirection9(BeatDirection9 dir9)
    {
        Vector3 f = new Vector3(0, 0, 1);
        Vector3 r = new Vector3(1, 0, 0);
        Vector3 u = new Vector3(0, 1, 0);

        switch (dir9)
        {
            case BeatDirection9.Center: return u;
            case BeatDirection9.N: return f;
            case BeatDirection9.NE: return (f + r).normalized;
            case BeatDirection9.E: return r;
            case BeatDirection9.SE: return (-f + r).normalized;
            case BeatDirection9.S: return -f;
            case BeatDirection9.SW: return (-f - r).normalized;
            case BeatDirection9.W: return -r;
            case BeatDirection9.NW: return (f - r).normalized;
            default: return f;
        }
    }
}