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
    [SerializeField] private GameObject player1Grid;
    private int[,] gridMap;

    public List<GameObject> playerShips;

    private void Start()
    {
        CreateGrid();
    }

    private Vector3 ConvertToWorldPos(int x, int y)
    {
        float tileSize = tilePrefab.GetComponent<BoxCollider2D>().bounds.size.x;
        return new Vector3 ((x * tileSize) - transform.position.x, y * tileSize - transform.position.y, 0);
    }

    private Vector2Int ConvertToCoords(Vector3 pos)
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
                var spawnedTIle = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity);
                spawnedTIle.transform.parent = player1Grid.transform;
                spawnedTIle.name = $"Tile {x} {y}";

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTIle.Init(isOffset);
            }
        }

        camera.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f,-10);
    }

    private void StartPlacement()
    {
        //Start spawning preview
        //Enable placement
    }

    private bool CheckIfCanPlaceShip(GameObject ship, Vector2Int cords)
    {
        if ((cords.x >= gridMap.GetLength(0) || cords.x < 0) || (cords.y >= gridMap.GetLength(1) || cords.y < 0))
        {
            Debug.Log("Out of grid position");
            return false;
        }
        for (int i = 0; i < ship.GetComponent<ShipScript>().Length; i++)
        {
            if (ship.transform.eulerAngles.z == 0)
            {
                if (gridMap[cords.x + i, cords.y] != 0)
                {
                    Debug.Log("There is a ship there");
                    return false;
                }
            }
            else
            {
                if (gridMap[cords.x, cords.y + i] != 0)
                {
                    Debug.Log("There is a ship there");
                    return false;
                }
            }
        }
        return true;
    }

    private void PlaceShip(GameObject ship, Vector3 pos)
    {
        Vector2Int cords = ConvertToCoords(pos);
        if (!CheckIfCanPlaceShip(ship,cords))
            return;




        GameObject spawnedShip;
        ShipScript shipScript = ship.GetComponent<ShipScript>();

        if (ship.transform.eulerAngles.z == 0)
        {
            for (int i = 0; i < ship.GetComponent<ShipScript>().Length; i++)
            {
                gridMap[cords.x + i, cords.y] = 1;
            }
            spawnedShip = Instantiate(ship, new Vector3(cords.x + ship.transform.localScale.x / (shipScript.Length * 2), cords.y, 0), ship.transform.rotation);
        }
        else 
        {
            for (int i = 0; i < ship.GetComponent<ShipScript>().Length; i++)
            {
                gridMap[cords.x, cords.y + i] = 1;
            }
            spawnedShip = Instantiate(ship, new Vector3(cords.x, cords.y + ship.transform.localScale.x / (shipScript.Length * 2), 0), ship.transform.rotation);
        }
        Debug.Log($"Ship placed at: {cords.x} {cords.y}");
        playerShips.Remove(ship);
    }

    private void RotateShip(GameObject ship)
    {
        if (ship.transform.eulerAngles.z == 0)
        {
            ship.transform.eulerAngles = new Vector3(0, 0, 90);
            return;
        }
        ship.transform.eulerAngles = new Vector3(0, 0, 0);
    }

    private Vector3 GetMousePosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        return mousePos;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlaceShip(playerShips.FirstOrDefault(), GetMousePosition());
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateShip(playerShips.FirstOrDefault());
        }
    }
}
