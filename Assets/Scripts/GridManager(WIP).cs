using Mono.Cecil.Cil;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }


    [SerializeField]
    private const int height = 10;

    [SerializeField] PlayerType localPlayerType;
    [SerializeField] private TileScript tilePrefab;
    [SerializeField] private Vector2 playerGridPos;
    [SerializeField] private int shipTiles = 0;

    [SerializeField]
    private int[,] player1GridMap;
    [SerializeField]
    private int[,] player2GridMap;


    public List<ShipType> playerShips = new List<ShipType>() {ShipType.Short,ShipType.Meduim, ShipType.Meduim, ShipType.Long, ShipType.ExtraLong };

    public NetworkVariable<PlayerType> currentPlayerTurn = new NetworkVariable<PlayerType>(PlayerType.None,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs {
        public float x;
        public float y;
        public int[,] gridMap;
        public ShipType st;
    }
    public EventHandler<OnGameStartedEventArgs> OnGameStarted;
    public class OnGameStartedEventArgs : EventArgs
    {
        public int height;
        public int[,] gridMap;
        public Vector2 playerGridPos;
        public PlayerType localPlayerType;
    }
    public EventHandler<OnPlacementStartedEventArgs> OnPlacementStarted;
    public class OnPlacementStartedEventArgs : EventArgs
    {
        public Vector2 cords;
        public ShipType st;
        public int remainingShipCount;
    }
    public EventHandler OnRotationKeyPressed;
    public EventHandler OnBattleStarted;

    private int rotationAngle;
    public bool IsPlacing;

    private NetworkVariable<int> readyPlayers = new NetworkVariable<int>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GridManager");
            Destroy(Instance);
        }
        Instance = this;
    }
    private void Start()
    {
        rotationAngle = 0;
        IsPlacing = false;
    }

    public Vector2 GetMousePosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2(mousePos.x, mousePos.y);
    }
    public Vector2Int ConvertToCoords(Vector2 pos)
    {
        Vector2Int coords = new Vector2Int(0, 0);
        coords.x = (int)Math.Round(pos.x);
        coords.y = (int)Math.Round(pos.y);

        return coords;
    }

    public void ClickedOnGridPosition(float x, float y)
    {
        if (localPlayerType != currentPlayerTurn.Value && GameManager.CurrGameState == GameState.Battle)
        {
            return;
        }
        if (playerShips.Count < 1 && GameManager.CurrGameState == GameState.Preparation)
        {
            Debug.Log("No more ships!");
            return;
        }

        int offset = localPlayerType == PlayerType.Player1 ? -8 : 8;
        int[,] gridMap = localPlayerType == PlayerType.Player1 ? player1GridMap : player2GridMap;

        Vector2Int coords = ConvertToCoords(new Vector2(x, y));
        ShipType st = playerShips.FirstOrDefault();
        int shipLength;
        switch (st)
        {
            case ShipType.Short:
                shipLength = 2;
                break;
            case ShipType.Meduim:
                shipLength = 3;
                break;
            case ShipType.Long:
                shipLength = 4;
                break;
            case ShipType.ExtraLong:
                shipLength = 5;
                break;
            default:
            case ShipType.None:
                shipLength = -1;
                break;
        }
        if (GameManager.CurrGameState == GameState.Preparation)
        {
            
            if (!CheckIfCanPlaceShip(shipLength, coords,gridMap, offset))
            {
                return;
            }
            Debug.Log("Clicked on grid");

        }
        if (GameManager.CurrGameState == GameState.Battle)
        {
            if (!CheckIfCanShoot(coords,-offset, localPlayerType))
            {
                return;
            }
            StartShootingAtPostion_Rpc(coords, -offset, localPlayerType);
        }


        if (GameManager.CurrGameState == GameState.Preparation)
        {
            StartShipPlacement(coords, shipLength, localPlayerType, offset);
            OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
            {
                x = x,
                y = y,
                gridMap = gridMap,
                st = st
            });
            OnPlacementStarted?.Invoke(this, new OnPlacementStartedEventArgs
            {
                cords = new Vector2(x, y),
                st = playerShips.FirstOrDefault(),
                remainingShipCount = playerShips.Count()
            });
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn: " + NetworkManager.Singleton.LocalClientId);
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Player1;
            playerGridPos = new Vector2 (-8, 0);
        }
        else
        {
            localPlayerType = PlayerType.Player2;
            playerGridPos = new Vector2(8, 0);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        readyPlayers.OnValueChanged += (int oldReadyPlayers, int newReadyPlayers) =>
        {
            if (readyPlayers.Value > 1)
            {
                //Start battle mode
                GameManager.Instance.SetGameState(GameState.Battle);
                Debug.Log("Battle started!");
                if (IsServer)
                {
                    currentPlayerTurn.Value = PlayerType.Player1;
                }
            }
        };
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2) 
        {
            //Start gane
            Debug.Log("Start game!");
            player1GridMap = new int[height, height];
            player2GridMap = new int[height, height];
            OnGameStartedRpc();
            StartPlacement();
        }
    }
   
    [Rpc(SendTo.Me)]
    private void OnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, new OnGameStartedEventArgs
        {
            height = height,
            gridMap = player1GridMap,
            playerGridPos = playerGridPos,
            localPlayerType = localPlayerType
        });
    }

    // ---------------------------------------------PLACEMENT SECTION--------------------------------------------------------------------------------------

    // CREATES SHIP PREVIEW
    public void StartPlacement()
    {
        //Start spawning preview
        //Enable placement
        if (IsPlacing)
        {
            return;
        }
        IsPlacing = true;
        OnPlacementStarted?.Invoke(this, new OnPlacementStartedEventArgs
        {
            cords = GetMousePosition(),
            st = playerShips.FirstOrDefault()
        });
    }

    private bool CheckIfCanPlaceShip(int shipLength, Vector2Int cords, int[,] gridMap, int Xoffset)
    {
        if ((cords.x >= gridMap.GetLength(0) + Xoffset || cords.x < 0 + Xoffset) || (cords.y >= gridMap.GetLength(1) || cords.y < 0))
        {
            Debug.Log("Out of grid position");
            return false;
        }
        for (int i = 0; i < shipLength; i++)
        {
            if (rotationAngle == 0)
            {
                if (cords.x - Xoffset + shipLength > gridMap.GetLength(1))
                {
                    Debug.Log("Cant place there");
                    return false;
                }
                if (gridMap[cords.x - (int)Xoffset + i, cords.y] != 0)
                {
                    Debug.Log("There is a ship there");
                    return false;
                }
            }
            else
            {
                if (cords.y + shipLength > gridMap.GetLength(0))
                {
                    Debug.Log("Cant place there");
                    return false;
                }
                if (gridMap[cords.x - (int)Xoffset, cords.y + i] != 0)
                {
                    Debug.Log("There is a ship there");
                    return false;
                }
            }
        }
        return true;
    }
    private void StartShipPlacement(Vector2Int cords, int shipLength, PlayerType playerType, int offset)
    {
        playerShips.Remove(playerShips.FirstOrDefault());
        shipTiles += shipLength;
        PlaceShip_Rpc(cords, shipLength, playerType, offset, playerShips.Count);
    }

    [Rpc(SendTo.Server)]
    private void PlaceShip_Rpc(Vector2Int cords,int shipLength, PlayerType playerType, int offset, int count)
    {
        //Debug.Log("Playertype: " + playerType.ToString());
        float tileSize = tilePrefab.transform.localScale.x;
        int[,] gridMap;
        if (playerType == PlayerType.Player1)
        {
            gridMap = player1GridMap;
        }
        else
        {
            gridMap = player2GridMap;
        }

        if (rotationAngle == 0)
        {
            for (int i = 0; i < shipLength; i++)
            {
                gridMap[cords.x - (int)offset + i, cords.y] = 1;
            }
        }
        else
        {
            for (int i = 0; i < shipLength; i++)
            {
                gridMap[cords.x - (int)offset, cords.y + i] = 1;
            }
        }

        if (count == 0)
        {
            ReadyPlayer_Rpc();
        }
    }

    public void RotateShip()
    {
        if (rotationAngle == 0)
        {
            rotationAngle = 90;
        }
        else
        {
            rotationAngle = 0;
        }
        OnRotationKeyPressed.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    private void ReadyPlayer_Rpc()
    {
        readyPlayers.Value += 1;
        Debug.Log(readyPlayers.Value);
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------


    //private void Update()
    //{

    //    //Debugging
    //    if (Input.GetKeyDown(KeyCode.Tab))
    //    {
    //        GameManager.SwitchGameState();
    //    }

    //    //Controls
    //    switch (GameManager.CurrGameState)
    //    {
    //        case GameState.Preparation:
    //            //Can be removed
    //            if (Input.GetKeyDown(KeyCode.Space) && playerShips.Count > 0)
    //            {
    //                StartPlacement();
    //            }
    //            //Controlled by each player
    //            if (IsPlacing)
    //            {
    //                //MovePreview(shipPreview, GetMousePosition());
    //                if (Input.GetMouseButtonDown(0))
    //                {
    //                    //PlaceShip(GetMousePosition());
    //                }
    //                if (Input.GetKeyDown(KeyCode.R))
    //                {
    //                    RotateShip();
    //                }
    //            }
    //            break;
    //        case GameState.Battle:
    //            //also check if it is the player's turn
    //            if (Input.GetMouseButtonDown(0))
    //            {
    //                //ShootAtPosition(GetMousePosition());
    //            }
    //            break;
    //        case GameState.Over:
    //            break;
    //        default:
    //            return;
    //    }

    //}

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GridManager.Instance.RotateShip();
        }
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = GetMousePosition();
            GridManager.Instance.ClickedOnGridPosition(mousePos.x, mousePos.y);
        }
    }
    private bool CheckIfCanShoot(Vector2Int cords,int Xoffset, PlayerType playerType)
    {
        Debug.Log($"shooting at: {cords.x} {cords.y}");
        int[,] gridMap = localPlayerType == PlayerType.Player1 ? player2GridMap : player1GridMap;

        if ((cords.x >= gridMap.GetLength(0) + Xoffset || cords.x < 0 + Xoffset) || (cords.y >= gridMap.GetLength(1) || cords.y < 0))
        {
            Debug.Log("Out of grid position");
            return false;
        }
        return true;
    }

    [Rpc(SendTo.Server)]
    private void StartShootingAtPostion_Rpc(Vector2 pos, int Xoffset, PlayerType playerType)
    {
        Vector2Int coords = ConvertToCoords(pos);
        TileScript hitTile;

        int[,] gridMap;
        if (playerType == PlayerType.Player1)
        {
            gridMap = player2GridMap;
        }
        else
        {
            gridMap = player1GridMap;
        }

        Debug.Log("player1gridMap " + player1GridMap[0, 0]);
        Debug.Log("currGridMap " + gridMap[0, 0]);

        if (gridMap[coords.x - (int)Xoffset, coords.y] == 1)
        {
            //we hit something
            Debug.Log($"Hit ship! At: {coords.x} {coords.y}");
            ShootAtPosition_Rpc(pos,Xoffset,playerType);

            //set tile there to hitColor
            if (playerType == PlayerType.Player1)
            {
                hitTile = GameObject.Find($"2Tile{coords.x - Xoffset}{coords.y}").GetComponent<TileScript>();
            }
            else
            {
                hitTile = GameObject.Find($"1Tile{coords.x - Xoffset}{coords.y}").GetComponent<TileScript>();
            }
            hitTile.OnHit_Rpc(true);

            shipTiles--;
            if (shipTiles == 0)
            {
                GameManager.SwitchGameState();
            }
        }
        else if (gridMap[coords.x - Xoffset, coords.y] == 0)
        {
            //no luck, pass turn
            Debug.Log($"Translated coords: {coords.x - Xoffset} {coords.y}");
            Debug.Log($"Needed position {gridMap[0, 0]}");
            Debug.Log($"Missed! At: {coords.x} {coords.y}, offset = {Xoffset}");
            SwitchPlayerTurn_Rpc();
            // set tile to missColor
            if (playerType == PlayerType.Player1)
            {
                hitTile = GameObject.Find($"2Tile{coords.x - Xoffset}{coords.y}").GetComponent<TileScript>();
            }
            else
            {
                hitTile = GameObject.Find($"1Tile{coords.x - Xoffset}{coords.y}").GetComponent<TileScript>();
            }
            hitTile.OnHit_Rpc(false);
        }
        else
        {
            //bad sfx or some particle idk, then pass turn
        }
    }

    [Rpc(SendTo.Server)]
    private void ShootAtPosition_Rpc(Vector2 pos,int Xoffset, PlayerType playerType)
    {
        Vector2Int coords = ConvertToCoords(pos);
        int[,] gridMap;
        if (playerType == PlayerType.Player1)
        {
            gridMap = player2GridMap;
        }
        else
        {
            gridMap = player1GridMap;
        }
        if (gridMap[coords.x - (int)Xoffset, coords.y] == 1)
        {
            gridMap[coords.x - Xoffset, coords.y] = 2;
        }
        else
        {

        }
    }

    [Rpc(SendTo.Server)]
    private void SwitchPlayerTurn_Rpc()
    {
        if (currentPlayerTurn.Value == PlayerType.Player1)
        {
            currentPlayerTurn.Value = PlayerType.Player2;
        }
        else
        {
            currentPlayerTurn.Value = PlayerType.Player1;
        }
    }
}
