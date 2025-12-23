using System.Collections;
using UnityEngine;


public class SplitCubeFlash : MonoBehaviour
{
    [Tooltip("Highlight color used for the flash.")]
    public Color flashColor = Color.yellow;
    [Tooltip("Duration of the flash in seconds.")]
    public float duration = 0.25f;

    private Renderer[] rends;

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>();
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float half = duration * 0.5f;
        var mats = new Material[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            mats[i] = rends[i].material;
            if (mats[i].HasProperty("_EmissionColor"))
            {
                mats[i].EnableKeyword("_EMISSION");
                mats[i].SetColor("_EmissionColor", flashColor);
            }
            else if (mats[i].HasProperty("_Color"))
            {
                mats[i].SetColor("_Color", flashColor);
            }
        }

        yield return new WaitForSeconds(half);

        for (int i = 0; i < rends.Length; i++)
        {
            var m = mats[i];
            if (m == null) continue;
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", Color.black);
            }
            else if (m.HasProperty("_Color"))
            {
                m.SetColor("_Color", Color.white);
            }
        }

        yield return null;
    }
}
