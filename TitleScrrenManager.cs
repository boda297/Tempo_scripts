using SG;
using Unity.Netcode;
using UnityEngine;

public class TitleScrrenManager : MonoBehaviour
{
    // Starts the game instance as both the server and a local client (host)
    public void StartNetworkAsHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartNewGame()
    {
        StartCoroutine(WorldGameManager.instance.LoadNewGame());
    }
}
