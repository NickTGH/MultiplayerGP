using UnityEngine;

public class TileScript : MonoBehaviour
{
    [SerializeField] private Color baseColor, offsetColor;
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private GameObject highlight;


    // by default = 0, if there is a ship = 1, if the ship has been hit = 2
    [SerializeField] private int value;

    public void Init(bool isOffset)
    {
        renderer.color = isOffset? offsetColor : baseColor;
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
