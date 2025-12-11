using Unity.Netcode;
using UnityEngine;

public class ShipScript : NetworkBehaviour
{
    public int Length;


    public void SetPreviewColor()
    {
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.4f);
    }
}
