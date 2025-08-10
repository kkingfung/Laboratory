using UnityEngine;

public interface IGameService
{
    void StartGame();
}

public class GameService : IGameService
{
    public void StartGame()
    {
        Debug.Log("Game Started!");
    }
}
