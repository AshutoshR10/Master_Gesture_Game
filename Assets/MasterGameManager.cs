using UnityEngine;
using UnityEngine.SceneManagement;

public class MasterGameManager : MonoBehaviour
{
    public void LoadGameScene(string keyCode)
    {
        switch (keyCode)
        {
            case "DINO":
                SceneManager.LoadScene("DinoLvl1");
                break;
            case "SPACE":
                SceneManager.LoadScene("SpaceInvaderslvl1");
                break;
        }
    }
}
