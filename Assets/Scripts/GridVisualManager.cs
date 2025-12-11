using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class GridVisualManager : NetworkBehaviour
{
    [SerializeField] private List<GameObject> shipList;
    [SerializeField] private TileScript tilePrefab;
    [SerializeField] private Transform player1GridParent;
    [SerializeField] private Transform player2GridParent;
    [SerializeField] private GameObject shipPreview;

    private void Start()
    {
        GridManager.Instance.OnClickedOnGridPosition += GridManager_OnClickedOnGridPosition;
        GridManager.Instance.OnGameStarted += GridManager_OnGameStarted;
        GridManager.Instance.OnPlacementStarted += GridManager_OnPlacementStarted;
        GridManager.Instance.OnRotationKeyPressed += GridManager_OnRotationKeyPressed;
    }

    private void GridManager_OnRotationKeyPressed(object sender, EventArgs e)
    {
        if (shipPreview.transform.eulerAngles.z == 0)
        {
            shipPreview.transform.eulerAngles = new Vector3(0, 0, 90);
            return;
        }
        shipPreview.transform.eulerAngles = new Vector3(0, 0, 0);
    }

    private void GridManager_OnClickedOnGridPosition(object sender, GridManager.OnClickedOnGridPositionEventArgs e)
    {
        //spawning

        if (GameManager.CurrGameState == GameState.Preparation)
        {
            SpawnShip_Rpc(shipPreview.transform.position.x,shipPreview.transform.position.y, e.st, shipPreview.transform.rotation);
            Destroy(shipPreview);
        }
        if (GameManager.CurrGameState == GameState.Battle && shipPreview != null)
        {
            Destroy(shipPreview);
        }
    }

    private void SpawnShip_Rpc(float x, float y, ShipType st, Quaternion rotation)
    {
        var spawnedShip = Instantiate(shipList[(int)st], new Vector2(x,y) , rotation);
        //spawnedShip.GetComponent<NetworkObject>().SpawnWithOwnership((ulong)st,true);
        Debug.Log(spawnedShip.transform.position.x + "   " + spawnedShip.transform.position.y);
    }

    private void GridManager_OnGameStarted(object sender, GridManager.OnGameStartedEventArgs e)
    {
        GameManager.Instance.SetGameState(GameState.Preparation);
        CreateGrid_Rpc(e.gridMap, e.height, e.playerGridPos, e.localPlayerType);
    }

    [Rpc(SendTo.Server)]
    private void CreateGrid_Rpc(int[,] gridMap, int height, Vector2 playerGridPos, PlayerType localPlayerType)
    {
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var spawnedTIle = Instantiate(tilePrefab, new Vector2(x, y) + playerGridPos, Quaternion.identity);
                spawnedTIle.GetComponent<NetworkObject>().Spawn(true);
                if (localPlayerType == PlayerType.Player2)
                {
                    spawnedTIle.transform.parent = player2GridParent.transform;
                }
                else
                {
                    spawnedTIle.transform.parent = player1GridParent.transform;
                }
                NameTiles_Rpc(localPlayerType, x, y);

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTIle.Init_Rpc(isOffset);
            }
        }

        //camera.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f,-10);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void NameTiles_Rpc(PlayerType pt, int x, int y)
    {
        var spawnedTile = GameObject.Find("Tile(Clone)").GetComponent<NetworkObject>();
        if (pt == PlayerType.Player2)
        {
            spawnedTile.name = $"2Tile{x}{y}";
            spawnedTile.transform.parent = player2GridParent.transform;
        }
        else
        {
            spawnedTile.name = $"1Tile{x}{y}";
            spawnedTile.transform.parent = player1GridParent.transform;
        }
    }

    private void GridManager_OnPlacementStarted(object sender, GridManager.OnPlacementStartedEventArgs e)
    {
        if (GameManager.CurrGameState == GameState.Battle)
        {
            Destroy(shipPreview);
            return;
        }
        SpawnPreview(e.st, e.cords, e.remainingShipCount);
    }

    private void SpawnPreview(ShipType st, Vector2 pos, int shipsRemaining)
    {
        var spawnedPreview = Instantiate(shipList[(int)st], pos, Quaternion.identity);
        spawnedPreview.GetComponent<ShipScript>().SetPreviewColor();
        shipPreview = spawnedPreview;
    }

    
    private void MovePreview_Rpc(Vector2 pos)
    {
        Vector2Int cords = GridManager.Instance.ConvertToCoords(pos);
        if (shipPreview.transform.eulerAngles.z == 0)
        {
            shipPreview.transform.position = new Vector3(cords.x + ((shipPreview.GetComponent<ShipScript>().Length - 1) * tilePrefab.transform.localScale.x / 2), cords.y, 0);
        }
        else
        {
            shipPreview.transform.position = new Vector3(cords.x, cords.y + ((shipPreview.GetComponent<ShipScript>().Length - 1) * tilePrefab.transform.localScale.x / 2), 0);
        }
    }


    private void Update()
    {
        if(shipPreview == null && GameManager.CurrGameState != GameState.Preparation)
        {
            return;
        }
        MovePreview_Rpc(GridManager.Instance.GetMousePosition());
    }
}
