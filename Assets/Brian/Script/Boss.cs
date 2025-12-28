using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Boss : MonoBehaviour
{
    public GameObject thunderPrefab, hintPrefab;

    [Header("Spawn Area")]
    public float thunderRadius = 5f;
    [Tooltip("Inner radius — spawn will be outside this radius.")]
    public float thunderInnerRadius = 1f;
    [Tooltip("Y offset from boss position where thunder should spawn (fixed Y).")]
    public float thunderYOffset = 0f;
    [Tooltip("Number of segments used to draw the gizmo circle")]
    public int gizmoSegments = 48;
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.6f);
    public Color gizmoInnerColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    /*public void SpawnThunder(Vector3 pos)
    {
        if (thunderPrefab == null && !PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.Instantiate(thunderPrefab.name, pos, Quaternion.identity);
    }*/

    IEnumerator SpawnThunder(Vector3 pos)
    {
        if (thunderPrefab == null) yield break;
        if (!PhotonNetwork.IsMasterClient) yield break;

        Vector3 hintPos = new Vector3(pos.x, pos.y, pos.z);
        GameObject hint = PhotonNetwork.Instantiate(hintPrefab.name, hintPos, Quaternion.identity);

        yield return new WaitForSeconds(1.0f); // wait before thunder strike
        
        // despawn hint object
        PhotonNetwork.Destroy(hint);

        GameObject lightning = PhotonNetwork.Instantiate(thunderPrefab.name, pos, Quaternion.identity);

        StartCoroutine(DestroyAfter(lightning));

        yield return null;
    }

    IEnumerator DestroyAfter(GameObject obj)
    {
        yield return new WaitForSeconds(1f);
        if (obj != null)
            PhotonNetwork.Destroy(obj);
    }

    // sample a random point inside the annulus (XZ plane) and spawn thunder at fixed Y (boss.y + offset)
    public void SpawnThunderRandom()
    {
        Vector2 sample = SamplePointInAnnulus(thunderInnerRadius, thunderRadius);
        Vector3 spawnPos = transform.position + new Vector3(sample.x, thunderYOffset, sample.y);
        StartCoroutine(SpawnThunder(spawnPos));
    }

    // spawn multiple thunder strikes at random positions
    public void SpawnThunderRandomMultiple(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnThunderRandom();
    }

    // draw circle gizmo on XZ plane at boss position + Y offset
    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * thunderYOffset;
        DrawCircleGizmo(center, thunderRadius, gizmoSegments, gizmoColor);
        DrawCircleGizmo(center, thunderInnerRadius, gizmoSegments, gizmoInnerColor);
    }

    // returns a uniformly sampled point in annulus between rMin (exclusive/inner) and rMax (inclusive/outer)
    Vector2 SamplePointInAnnulus(float rMin, float rMax)
    {
        // ensure valid radii
        float inner = Mathf.Max(0f, Mathf.Min(rMin, rMax));
        float outer = Mathf.Max(rMin, rMax);

        // sample radius with sqrt trick for uniform area distribution
        float r = Mathf.Sqrt(Random.Range(inner * inner, outer * outer));
        float theta = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(theta) * r, Mathf.Sin(theta) * r);
    }

    void DrawCircleGizmo(Vector3 center, float radius, int segments, Color color)
    {
        if (segments < 8) segments = 8;
        Gizmos.color = color;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        float step = 360f / segments;
        for (int i = 1; i <= segments; i++)
        {
            float ang = step * i;
            Vector3 next = center + new Vector3(
                Mathf.Cos(Mathf.Deg2Rad * ang) * radius,
                0f,
                Mathf.Sin(Mathf.Deg2Rad * ang) * radius
            );
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
