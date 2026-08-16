public enum GameMode
{
    SinglePlayer,
    SinglePlayerWithAI,
    LocalMultiplayer,
}

public static class GameModeSelection
{
    public static GameMode Current { get; private set; } = GameMode.SinglePlayer;

    public static void Select(GameMode gameMode)
    {
        Current = gameMode;
    }
}
