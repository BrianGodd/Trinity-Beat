using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;
    // Start is called before the first frame update
    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
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
                ActionCat();
                break;
            case "fog":
                ActionFog();
                break;
            case "dog":
                ActionDog();
                break;
            case "owl":
                ActionOwl();
                break;
            case "tnt":
                ActionTnt();
                break;
            default:
                return false;
        }
        return true;
    }
    public void ActionRun()
    {
        
    }

    public void ActionRng()
    {
        
    }

    public void ActionFly()
    {
        
    }

    public void ActionFix()
    {
        
    }

    public void ActionHot()
    {
        
    }

    public void ActionCut()
    {
        
    }

    public void ActionAim()
    {
        
    }

    public void ActionAoe()
    {
        
    }

    public void ActionIce()
    {
        
    }

    public void ActionWet()
    {
        
    }

    public void ActionTox()
    {
        
    }

    public void ActionCat()
    {
        
    }

    public void ActionFog()
    {
        
    }

    public void ActionDog()
    {
        
    }

    public void ActionOwl()
    {
        
    }

    public void ActionTnt()
    {
        
    }
}
