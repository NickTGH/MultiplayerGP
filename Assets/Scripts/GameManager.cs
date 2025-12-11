using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
            return;
        }
        Instance = this;
    }

    public static GameState CurrGameState;

    public static void SwitchGameState()
    {
        if (CurrGameState == GameState.Preparation)
        {
            Debug.Log("Swithced to BATTLE");
            CurrGameState = GameState.Battle;
        }
        else if (CurrGameState == GameState.Battle)
        {
            Debug.Log("Swithced to OVER");
            CurrGameState = GameState.Over;
        }
        else if (CurrGameState == GameState.Over)
        {
            Debug.Log("Swithced to PREPARATION");
            CurrGameState = GameState.Preparation;
        }
    }
    public void SetGameState(GameState gs)
    {
        CurrGameState = gs;
    }
}


public enum GameState
{
    None,
    Preparation,
    Battle,
    Over
}

public enum ShipType
{
    Short,
    Meduim,
    Long,
    ExtraLong,
    None
}

public enum PlayerType
{
    None,
    Player1,
    Player2
}
