using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MainMenuView : MonoBehaviour
{
    private ISceneController sceneController;

    [SerializeField] private Button startButton;

    public void Construct(ISceneController sceneController)
    {
        this.sceneController = sceneController;
    }

    public void OnPlayButton()
    {
        sceneController.LoadSceneAsync("Gameplay").Forget();
    }

    void Start()
    {
        startButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadSceneAsync("Gameplay").Forget();
        });
    }
}
