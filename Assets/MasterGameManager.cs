using UnityEngine;
using UnityEngine.SceneManagement;

public class MasterGameManager : MonoBehaviour
{

    [ContextMenu("Test Load Scene")]
    public void TestLoadSceneDino()
    {
        LoadGameScene("DINO");
    }

    [ContextMenu("Test Load Space")]
    public void TestLoadSceneSpace()
    {
        LoadGameScene("SPACE");
    }

    public void LoadGameScene(string keyCode)
    {
        switch (keyCode)
        {
            case "DINO":
                SceneManager.LoadScene("DinoLvl1");
                break;
            case "SPACE":
                SceneManager.LoadScene("SpaceInvadersvl1");
                break;
        }
    }
}
