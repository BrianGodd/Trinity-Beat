using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyBall : MonoBehaviour
{
    private Vector3 direction;
    private float length;
    public float existTime = 0.5f;
    public Transform visualEffect;
    private Vector3 originalPosition;
    private WeaponEffect.EnhancementEffect enhancementEffect;

    // test parameter
    public float timer = 0f;
    void Start()
    {
        originalPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
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
    }
    void OnTriggerEnter(Collider other)
    {
        // 處理被攻擊
        Debug.Log("Attack by " + enhancementEffect);
    }

    // void OnTriggerExit(Collider other)
    // {
    //     // 處理視野
    //     Debug.Log("like GetComponent<InvisibleArea>().triggerCount -= 1;");
    // }
}
