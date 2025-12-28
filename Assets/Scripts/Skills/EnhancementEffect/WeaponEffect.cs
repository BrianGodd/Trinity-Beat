using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


[RequireComponent(typeof(PhotonView))]
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
    PhotonView pv;

    [SerializeField]
    protected EnhancementEffect enhancementEffect = EnhancementEffect.None;

    public CatEntity catEntity = null;


    public int effectLifeCycle = 0;
    public int AOELifeCycle = 0;
    public int AimLifeCycle= 0;
    public int RNGLifeCycle= 0;

    void Start()
    {
        pv = GetComponent<PhotonView>();
        if(!pv.IsMine)return;
        SkillManager.Instance.weaponEffect = this;
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
        effectLifeCycle += offset;
        AOELifeCycle += offset;
    }

    public virtual void SetEnhancementEffect(EnhancementEffect newEnhancementEffect)
    {
        enhancementEffect = newEnhancementEffect;
        effectLifeCycle = 3;
    }

    public virtual void Attack(Vector3 dir, Vector3 delta, float length){}

    public float GetCurrentCatDistane()
    {
        return catEntity? Vector3.Distance(catEntity.gameObject.transform.position, transform.position):0;
    }

}
