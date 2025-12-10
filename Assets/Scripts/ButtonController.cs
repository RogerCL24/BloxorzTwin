using UnityEngine;
using System.Collections.Generic;

public class ButtonController : MonoBehaviour
{
    public enum Mode { Toggle, OpenOnly, CloseOnly }

    public Mode mode = Mode.Toggle;
    public List<BridgeTile> controlledTiles = new List<BridgeTile>();
    
    public void Activate()
    {
        foreach (var tile in controlledTiles)
        {
            if (tile == null) continue;

            switch (mode)
            {
                case Mode.Toggle:
                    tile.Toggle();
                    break;
                case Mode.OpenOnly:
                    tile.SetState(true);
                    break;
                case Mode.CloseOnly:
                    tile.SetState(false);
                    break;
            }
        }

        if (GlobalAudio.Instance != null)
        {
            GlobalAudio.Instance.PlayBridgeToggle();
        }
    }
}
