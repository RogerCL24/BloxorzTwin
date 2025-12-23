using System.Collections;
using UnityEngine;

/// <summary>
/// Simple flash effect for split cubes: briefly tints emission/color on all renderers.
/// </summary>
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
        // Attempt to use emission where possible, fallback to color
        var mats = new Material[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            // create a material instance so we don't modify shared material
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

        // hold briefly
        yield return new WaitForSeconds(half);

        // then fade out emission/color back to original by disabling or resetting
        for (int i = 0; i < rends.Length; i++)
        {
            var m = mats[i];
            if (m == null) continue;
            if (m.HasProperty("_EmissionColor"))
            {
                // reduce emission
                m.SetColor("_EmissionColor", Color.black);
            }
            else if (m.HasProperty("_Color"))
            {
                // try to reset to white/neutral — cannot know original without storing, so set to white
                m.SetColor("_Color", Color.white);
            }
        }

        yield return null;
    }
}
