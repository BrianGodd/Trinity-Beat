using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class EnergyBall : MonoBehaviour
{
    private Vector3 direction;
    private float length;
    public float existTime = 0.5f;
    public Transform visualEffect;
    private Vector3 originalPosition;
    private WeaponEffect.EnhancementEffect enhancementEffect;
    [SerializeField]
    private List<ParticleSystem> particleSystems;
    PhotonView pv;


    // test parameter
    public float timer = 0f;
    void Start()
    {
        pv = GetComponent<PhotonView>();
        originalPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(!pv.IsMine)return;
        transform.position += direction * Time.deltaTime * length / existTime;
        visualEffect.position = originalPosition + (transform.position - originalPosition) /2f;
        Vector3 scale = visualEffect.localScale;
        scale.z = (transform.position - originalPosition).magnitude;
        visualEffect.localScale = scale;
        // testing
        timer += Time.deltaTime;
        if(timer > existTime)
        {
            Destroy(visualEffect.gameObject);
            Destroy(gameObject);
        }
    }
    public void Init(Vector3 dir, float l, WeaponEffect.EnhancementEffect ee)
    {
        direction = dir;
        length = l;
        enhancementEffect = ee;
        Color color = new Color(1f, 1f, 1f);
        switch (ee)
        {
            case WeaponEffect.EnhancementEffect.Hot:
                color = new Color(1f, 0.4f, 0.5f);
                break;
            case WeaponEffect.EnhancementEffect.Ice:
                color = new Color(0.8f, 0.8f, 1f);
                break;
            case WeaponEffect.EnhancementEffect.Tox:
                color = new Color(0.65f, 0.3f, 0.7f);
                break;
            case WeaponEffect.EnhancementEffect.Wet:
                color = new Color(0.7f, 0.6f, 1f);
                break;
            default:
                break;
        }
        for (int i = 0; i < particleSystems.Count; i++)
        {
            var ps = particleSystems[i];
            if (ps == null)
                continue;

            var main = ps.main;
            main.startColor = color;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(pv.IsMine)return;
        PlayerLife playerLife = other.GetComponent<PlayerLife>();
        if(playerLife == null || !playerLife.gameObject.GetComponent<PhotonView>().IsMine)
        {
            return;
        }
        // 處理被攻擊
        Debug.Log("Attack by " + enhancementEffect);
        float damage = -20f;
        switch (enhancementEffect)
        {
            case WeaponEffect.EnhancementEffect.Hot:
                damage *= 2f;
                break;
            case WeaponEffect.EnhancementEffect.Ice:
                // other.GetComponent<Locomotion>().speed decrease
                break;
            case WeaponEffect.EnhancementEffect.Tox:
                // other.GetComponent<HPSystem>().isToxic = true;
                break;
            case WeaponEffect.EnhancementEffect.Wet:

                break;
            default:
                break;

        }
        playerLife.RequestChangeLife((int)damage);
    }

    // void OnTriggerExit(Collider other)
    // {
    //     // 處理視野
    //     Debug.Log("like GetComponent<InvisibleArea>().triggerCount -= 1;");
    // }
}
