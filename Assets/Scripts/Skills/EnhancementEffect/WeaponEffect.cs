using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponEffect : MonoBehaviour
{
    public enum EnhancementEffect
    {
        None,
        Hot,
        Ice,
        Wet,
        Tox
    }
    [SerializeField]
    private EnhancementEffect enhancementEffect = EnhancementEffect.None;
    [SerializeField]
    private Transform startPoint;
    [SerializeField]
    private GameObject attackObject;
    public int effectLifeCycle = 0;
    public int AOELifeCycle = 0;
    public int AimLifeCycle= 0;
    public int RNGLifeCycle= 0;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 d = transform.forward;
            d.y = 0;
            d.Normalize();
            Attack(d, Vector3.zero, 5f);
        }
        if(effectLifeCycle == 0)
        {
            enhancementEffect = EnhancementEffect.None;
        }
    }
    public void AddLifeCycle(int offset)
    {
        AimLifeCycle += offset;
        RNGLifeCycle += offset;
    }

    public void SetEnhancementEffect(EnhancementEffect newEnhancementEffect)
    {
        enhancementEffect = newEnhancementEffect;
        effectLifeCycle = 3;
    }

    public void Attack(Vector3 dir, Vector3 delta, float length)
    {
        delta *= (AimLifeCycle > 0?0.5f:1f) * (RNGLifeCycle > 0?2f:1f);
        dir += delta;
        dir.Normalize();
        GameObject newAttackObject = Instantiate(attackObject, startPoint.position, startPoint.rotation);
        if(AOELifeCycle > 0)
        {
            newAttackObject.transform.localScale *= 2f;
        }
        newAttackObject.GetComponent<EnergyBall>()?.Init(dir, length, enhancementEffect);
    }

}
