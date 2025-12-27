using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EntityPrefabEntry
{
    public string key;
    public GameObject prefab;
}

public class SkillManager : MonoBehaviour
{
    public enum EnhancementEffect
    {
        Hot,
        Ice,
        Wet,
        Tox
    }
    public static SkillManager Instance;
    [SerializeField]
    private List<EntityPrefabEntry> entityprefabEntries;

    [SerializeField]
    private Dictionary<string, GameObject> prefabDict;


    [SerializeField]
    private List<SummonedEntity> summonedEntities;

    




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

    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer > 0.5f)
        {
            EntitiesCycle();
            timer -= 0.5f;
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
    public void EntitiesCycle()
    {
        for (int i = summonedEntities.Count - 1; i >= 0; i--)
        {
            summonedEntities[i]?.AddLifeCycle(-1);
        }
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
            case "cut":
                ActionCut();
                break;
            case "aim":
                ActionAim();
                break;
            case "aoe":
                ActionAoe();
                break;
            case "ice":
                ActionIce();
                break;
            case "wet":
                ActionWet();
                break;
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
        
    }

    private void ActionRng()
    {
        
    }

    private void ActionFly()
    {
        
    }

    private void ActionFix()
    {
        
    }

    private void ActionHot()
    {
        
    }

    private void ActionCut()
    {
        
    }

    private void ActionAim()
    {
        
    }

    private void ActionAoe()
    {
        
    }

    private void ActionIce()
    {
        
    }

    private void ActionWet()
    {
        
    }

    private void ActionTox()
    {
        
    }

    private void ActionSummon(string name)
    {
        if (!prefabDict.TryGetValue(name, out var prefab))
        return;

        Instantiate(prefab);
    }

}
