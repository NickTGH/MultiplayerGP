using Unity.Netcode;
using UnityEngine;

public class TileScript : NetworkBehaviour
{
    [SerializeField] private Color baseColor, offsetColor, hitColor, missColor;
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private GameObject highlight;

    [Rpc(SendTo.ClientsAndHost)]
    public void Init_Rpc(bool isOffset)
    {
        renderer.color = isOffset? offsetColor : baseColor;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void OnHit_Rpc(bool isShip)
    {
        Debug.Log("TURNING REDD");
        renderer.color = isShip ? hitColor : missColor;
    }

    private void OnMouseEnter()
    {
        highlight.SetActive(true);
    }

    private void OnMouseExit()
    {
        highlight.SetActive(false);
    }
}
