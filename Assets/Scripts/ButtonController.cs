using UnityEngine;
using System.Collections.Generic;

public class ButtonController : MonoBehaviour
{
    public List<BridgeTile> controlledTiles = new List<BridgeTile>();
    
    public void Activate()
    {
        foreach (var tile in controlledTiles)
        {
            if (tile != null)
            {
                tile.Toggle();
            }
        }

        if (GlobalAudio.Instance != null)
        {
            GlobalAudio.Instance.PlayBridgeToggle();
        }
    }
}
