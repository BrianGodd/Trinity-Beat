using UnityEngine;

[DisallowMultipleComponent]
public class BushStealthZonePun : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var stealth = other.GetComponentInParent<PlayerBushStealthPunSwap>();
        if (stealth != null) stealth.EnterBush();
    }

    void OnTriggerExit(Collider other)
    {
        var stealth = other.GetComponentInParent<PlayerBushStealthPunSwap>();
        if (stealth != null) stealth.ExitBush();
    }
}
