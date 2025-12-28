using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


[System.Serializable]
public class EntityPrefabEntry
{
    public string key;
    public GameObject prefab;
}

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    private Transform player;
    [SerializeField]
    private List<EntityPrefabEntry> entityprefabEntries;

    [SerializeField]
    private Dictionary<string, GameObject> prefabDict;


    [SerializeField]
    private List<SummonedEntity> summonedEntities;

    [SerializeField]
    public WeaponEffect weaponEffect;

    [SerializeField]
    public PlayerLife playerLife;


    // test parameter
    public float timer = 0f;


    // Start is called before the first frame update
    void Start()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        prefabDict = new Dictionary<string, GameObject>();
        foreach (var entry in entityprefabEntries)
        {
            if (!prefabDict.ContainsKey(entry.key))
                prefabDict.Add(entry.key, entry.prefab);
        }
        
        // TODO: get user transform?
        player = FindMyPlayerTransform();
        
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

    // Update is called once per frame
    void Update()
    {
        // timer += Time.deltaTime;
        // if(timer > 5f)
        // {
        //     AfterCycle();
        //     timer -= 5f;
        // }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkillDetection("tnt");
        }
    }

    public void AddIntoEntitiesList(SummonedEntity summonedEntity)
    {
        summonedEntities.Add(summonedEntity);
    }
    public void RemoveFromEntitiesList(SummonedEntity summonedEntity)
    {
        summonedEntities.Remove(summonedEntity);
    }
    public void AfterCycle()
    {
        for (int i = summonedEntities.Count - 1; i >= 0; i--)
        {
            summonedEntities[i]?.AddLifeCycle(-1);
        }
        weaponEffect?.AddLifeCycle(-1);
    }
    public bool SkillDetection(string input)
    {
        input = input.ToLower();
        switch (input)
        {
            case "run":
                ActionRun();
                break;
            case "rng":
                ActionRng();
                break;
            case "fly":
                ActionFly();
                break;
            case "fix":
                ActionFix();
                break;
            case "hot":
                ActionHot();
                break;
            // case "cut":
            //     ActionCut();
            //     break;
            case "aim":
                ActionAim();
                break;
            case "aoe":
                ActionAoe();
                break;
            case "ice":
                ActionIce();
                break;
            // case "wet":
            //     ActionWet();
            //     break;
            case "tox":
                ActionTox();
                break;
            case "cat":
            case "fog":
            case "dog":
            case "owl":
            case "tnt":
                ActionSummon(input);
                break;
            default:
                return false;
        }
        return true;
    }
    private void ActionRun()
    {
        // increase speed in PlayerController
    }

    private void ActionRng()
    {
        weaponEffect.RNGLifeCycle = 3;
    }

    private void ActionFly()
    {
        // increase jumpForce in PlayerController
        
    }

    private void ActionFix()
    {
        // heal one hp
        playerLife.RequestChangeLife(20);
    }

    private void ActionHot()
    {
        weaponEffect?.SetEnhancementEffect(WeaponEffect.EnhancementEffect.Hot);
    }

    private void ActionCut()
    {
        
    }

    private void ActionAim()
    {
        weaponEffect.AimLifeCycle = 3;
    }

    private void ActionAoe()
    {
        weaponEffect.AOELifeCycle = 3;
    }

    private void ActionIce()
    {
        weaponEffect?.SetEnhancementEffect(WeaponEffect.EnhancementEffect.Ice);
    }

    private void ActionWet()
    {
        weaponEffect?.SetEnhancementEffect(WeaponEffect.EnhancementEffect.Wet);
    }

    private void ActionTox()
    {
        weaponEffect?.SetEnhancementEffect(WeaponEffect.EnhancementEffect.Tox);
        
    }

    private void ActionSummon(string name)
    {
        if (!prefabDict.TryGetValue(name, out var prefab))
        return;

        PhotonNetwork.Instantiate(
            prefab.name,
            player.position + player.forward * 0.5f + ((name == "owl")?3f:0f )* Vector3.up,
            player.rotation
        );
    }

}
