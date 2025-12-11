using Mono.Cecil.Cil;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridScript : MonoBehaviour
{
    [SerializeField]
    private int height;
    [SerializeField]
    private int width;

    [SerializeField] private TileScript tilePrefab;
    [SerializeField] private Transform camera;
    [SerializeField] private GameObject playerGrid;

    [SerializeField] bool player2 = false;
    private int[,] gridMap;

    public List<GameObject> playerShips;


    [SerializeField] private int shipTiles = 0;

    private GameObject shipPreview;
    public bool IsPlacing;

    private void Start()
    {
        IsPlacing = false;
        shipPreview = new GameObject();
        CreateGrid();
    }

    private Vector3 ConvertToWorldPos(int x, int y)
    {
        float tileSize = tilePrefab.GetComponent<BoxCollider2D>().bounds.size.x;
        return new Vector3 ((x * tileSize) - transform.position.x, y * tileSize - transform.position.y, 0);
    }

    public Vector2Int ConvertToCoords(Vector2 pos)
    {
        float tileSize = tilePrefab.GetComponent<BoxCollider2D>().bounds.size.x;

        Vector2Int coords = new Vector2Int(0, 0);
        coords.x = (int)Math.Round(pos.x);
        coords.y = (int)Math.Round(pos.y);

        return coords;
    }

    private void CreateGrid()
    {
        gridMap = new int[height, width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var spawnedTIle = Instantiate(tilePrefab, new Vector3(x, y) + playerGrid.transform.position, Quaternion.identity);
                spawnedTIle.transform.parent = playerGrid.transform;
                if (player2)
                {
                    spawnedTIle.name = $"2Tile{x}{y}";
                }
                else
                {
                    spawnedTIle.name = $"1Tile{x}{y}";
                }

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTIle.Init_Rpc(isOffset);
            }
        }

        //camera.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f,-10);
    }
    
    // ---------------------------------------------PLACEMENT SECTION--------------------------------------------------------------------------------------

    // CREATES SHIP PREVIEW
    private void StartPlacement()
    {
        //Start spawning preview
        //Enable placement
        if (IsPlacing)
        {
            return;
        }
        shipPreview = Instantiate(playerShips.First(), GetMousePosition(), Quaternion.identity);
        shipPreview.GetComponent<SpriteRenderer>().color = new Color(1f ,1f ,1f, 0.4f);
        IsPlacing = true;
    }

    private void MovePreview(GameObject shipPreview, Vector3 mousePos)
    {
        Vector2Int cords = ConvertToCoords(mousePos);
        if (shipPreview.transform.eulerAngles.z == 0)
        {
            shipPreview.transform.position = new Vector3(cords.x + ((shipPreview.GetComponent<ShipScript>().Length - 1) * tilePrefab.transform.localScale.x / 2), cords.y, 0);

        }
        else
        {
            shipPreview.transform.position = new Vector3(cords.x, cords.y + ((shipPreview.GetComponent<ShipScript>().Length - 1) * tilePrefab.transform.localScale.x / 2), 0);
        }
    }

    private bool CheckIfCanPlaceShip(GameObject ship, Vector2Int cords)
    {
        Debug.Log($"placing at: {cords.x} {cords.y}");

        if ((cords.x >= gridMap.GetLength(0) + playerGrid.transform.position.x || cords.x < 0 + playerGrid.transform.position.x) || (cords.y >= gridMap.GetLength(1) || cords.y < 0))
        {
            Debug.Log("Out of grid position");
            return false;
        }
        for (int i = 0; i < ship.GetComponent<ShipScript>().Length; i++)
        {


            if (shipPreview.transform.eulerAngles.z == 0)
            {
                if (cords.x - playerGrid.transform.position.x + ship.GetComponent<ShipScript>().Length > gridMap.GetLength(1))
                {
                    Debug.Log("Cant place there");
                    return false;
                }
                if (gridMap[cords.x - (int)playerGrid.transform.position.x + i, cords.y] != 0)
                {
                    Debug.Log("There is a ship there");
                    return false;
                }
            }
            else
            {
                if (cords.y + ship.GetComponent<ShipScript>().Length > gridMap.GetLength(0))
                {
                    Debug.Log("Cant place there");
                    return false;
                }
                if (gridMap[cords.x - (int)playerGrid.transform.position.x, cords.y + i] != 0)
                {
                    Debug.Log("There is a ship there");
                    return false;
                }
            }
        }
        return true;
    }

    private void PlaceShip(Vector3 pos)
    {
        Vector2Int cords = ConvertToCoords(pos);
        if (!CheckIfCanPlaceShip(shipPreview,cords))
            return;


        GameObject spawnedShip;
        ShipScript shipScript = shipPreview.GetComponent<ShipScript>();
        float tileSize = tilePrefab.transform.localScale.x;

        if (shipPreview.transform.eulerAngles.z == 0)
        {
            for (int i = 0; i < shipPreview.GetComponent<ShipScript>().Length; i++)
            {
                gridMap[cords.x - (int)playerGrid.transform.position.x + i, cords.y] = 1;
            }
            spawnedShip = Instantiate(playerShips.FirstOrDefault(), shipPreview.transform.position, shipPreview.transform.rotation);
        }
        else 
        {
            for (int i = 0; i < shipPreview.GetComponent<ShipScript>().Length; i++)
            {
                gridMap[cords.x - (int)playerGrid.transform.position.x, cords.y + i] = 1;
            }
            spawnedShip = Instantiate(playerShips.FirstOrDefault(), shipPreview.transform.position, shipPreview.transform.rotation);
        }

        shipTiles += shipScript.Length;
        Debug.Log($"Ship placed at: {cords.x} {cords.y}");
        Destroy(shipPreview);
        playerShips.Remove(playerShips.First());
        IsPlacing = false;
    }

    private void RotateShip()
    {
        if (shipPreview.transform.eulerAngles.z == 0)
        {
            shipPreview.transform.eulerAngles = new Vector3(0, 0, 90);
            return;
        }
        shipPreview.transform.eulerAngles = new Vector3(0, 0, 0);
    }
    //-----------------------------------------------------------------------------------------------------------------------------------------------------

    //NEED TO BE DETECTED FOR EACH PLAYER
    private Vector3 GetMousePosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        return mousePos;
    }

    private void Update()
    {

        //Debugging
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameManager.SwitchGameState();
        }

        //Controls
        switch (GameManager.CurrGameState)
        {
            case GameState.Preparation:
                //Can be removed
                if (Input.GetKeyDown(KeyCode.Space) && playerShips.Count > 0)
                {
                    StartPlacement();
                }
                //Controlled by each player
                if (IsPlacing)
                {
                    MovePreview(shipPreview, GetMousePosition());
                    if (Input.GetMouseButtonDown(0))
                    {
                        PlaceShip(GetMousePosition());
                    }
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        RotateShip();
                    }
                }
                break;
            case GameState.Battle:
                //also check if it is the player's turn
                if (Input.GetMouseButtonDown(0))
                {
                    ShootAtPosition(GetMousePosition());
                }
                break;
            case GameState.Over:
                break;
            default:
                return;
        }

    }

    private bool CheckIfCanShoot(Vector2Int cords)
    {
        Debug.Log($"shooting at: {cords.x} {cords.y}");

        if ((cords.x >= gridMap.GetLength(0) + playerGrid.transform.position.x || cords.x < 0 + playerGrid.transform.position.x) || (cords.y >= gridMap.GetLength(1) || cords.y < 0))
        {
            Debug.Log("Out of grid position");
            return false;
        }
        return true;
    }

    private void ShootAtPosition(Vector3 pos)
    {
        Vector2Int coords = ConvertToCoords(pos);
        if (!CheckIfCanShoot(coords))
        {
            return;
        }
        //check if pos x y is empty on the grid
        TileScript hitTile;
        if (gridMap[coords.x - (int)playerGrid.transform.position.x, coords.y] == 1)
        {
            //we hit something
            Debug.Log($"Hit ship! At: {coords.x} {coords.y}");
            gridMap[coords.x - (int)playerGrid.transform.position.x, coords.y] = 2;

            //set tile there to hitColor
            if (player2)
            {
                hitTile = GameObject.Find($"2Tile{coords.x - (int)playerGrid.transform.position.x}{coords.y}").GetComponent<TileScript>();
            }
            else
            {
                hitTile = GameObject.Find($"1Tile{coords.x - (int)playerGrid.transform.position.x}{coords.y}").GetComponent<TileScript>();
            }
            hitTile.OnHit_Rpc(true);
            
            shipTiles--;
            if (shipTiles == 0)
            {
                GameManager.SwitchGameState();
            }
        }
        if (gridMap[coords.x - (int)playerGrid.transform.position.x, coords.y] == 0)
        {
            //no luck, pass turn
            Debug.Log($"Missed! At: {coords.x} {coords.y}");
            // set tile to missColor
            if (player2)
            {
                hitTile = GameObject.Find($"2Tile{coords.x - (int)playerGrid.transform.position.x}{coords.y}").GetComponent<TileScript>();
            }
            else
            {
                hitTile = GameObject.Find($"1Tile{coords.x - (int)playerGrid.transform.position.x}{coords.y}").GetComponent<TileScript>();
            }
            hitTile.OnHit_Rpc(false);
        }
        else
        {
            //bad sfx or some particle idk, then pass turn
        }
    }
}
