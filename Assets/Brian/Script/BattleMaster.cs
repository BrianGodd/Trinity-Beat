using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMaster : MonoBehaviour
{
    public RectTransform UIRect;
    public GameObject deathEffectPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDeadUI()
    {
        GameObject cd = Instantiate(deathEffectPrefab);
        cd.transform.parent = UIRect;
        cd.transform.localPosition = Vector3.zero;   
    }
}
