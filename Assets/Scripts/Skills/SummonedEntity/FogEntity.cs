using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogEntity : SummonedEntity
{
    public Renderer renderer;
    public Material originalMaterial;
    public Material visibleMaterial;

    public void Start()
    {
        SkillManager.Instance.AddIntoEntitiesList(this);
        if(lifeCycle == 0)
        {
            SetLifeCycle(10);
        }
        renderer.sharedMaterial = originalMaterial;
    }


    public void EnableVisibiity(bool visible)
    {
        renderer.sharedMaterial = visible?visibleMaterial : originalMaterial;
    }
}
