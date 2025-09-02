namespace Space
{
    using TMPro;
    using UnityEngine;
    using System.Collections;
    using UnityEngine.SceneManagement;


#if UNITY_IOS
using System.Runtime.InteropServices;
using JetBrains.Annotations;

#endif

    using UnityEngine.Android;
    //using global::TMPro;

#if UNITY_IOS
public class NativeAPI
{
    [DllImport("__Internal")]
    public static extern void sendMessageToMobileApp(string message);
}
#endif

    public class NativeKeycodeScript : MonoBehaviour
    {
        /* public AndroidJavaObject activity;
         public AndroidJavaObject communicationBridge;
         public AndroidJavaClass unityPlayer;*/


        public static NativeKeycodeScript instance;
        private Player player;
        private GameManager gameManager;
        private PauseMenu pauseMenu;

        public TMP_Text debugText;


        /*private void Start()
        {
    #if UNITY_ANDROID
            unityPlayer = new("com.unity3d.player.UnityPlayer");
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            communicationBridge = new AndroidJavaObject("com.Zigurous.SpaceInvaders.SetDataFromUnity");
    #endif
        }*/

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //player = FindFirstObjectByType<Player>(); // reacquire the new player
            //FindAndCacheReferences();
            StartCoroutine(WaitAndFindRef(scene.name));
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Awake()
        {
            player = FindFirstObjectByType(typeof(Player)) as Player;
            if (instance == null)
            {
                instance = this;
                //DontDestroyOnLoad(gameObject);

            }
            else
            {
                //Destroy(gameObject);
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void ShowDebug(string message)
        {
            //Debug.Log(message); // still log to console
            if (debugText != null)
                debugText.text = message;
        }
        private IEnumerator WaitAndFindRef(string sceneName)
        {
            yield return null; // wait 1 frame
            FindRef();
            //AddDebug($"[KeyBinding] Cached references for scene: {sceneName}");
        }
        private void FindRef()
        {
            player = FindFirstObjectByType<Player>();
            gameManager = FindFirstObjectByType<GameManager>();
            pauseMenu = FindFirstObjectByType<PauseMenu>();
        }
        public void NativeKeyCodes(string Keycode)
        {
            switch (Keycode)
            {
                case "2": // left Movement
                    player?.MoveLeft();
                    //ShowDebug("Key 2 pressed  Player Move Left");
                    break;

                case "3": //right Movement
                    player?.MoveRight();
                    //ShowDebug("Key 3 pressed  Player Move Right");
                    break;

                case "4":
                    //GameManager.Instance.gameOverUI.SetActive(false); //spacebar close first gesture
                    if (GameManager.Instance != null)
                    {
                        //GameManager.Instance.gameOverUI.SetActive(false);
                        GameManager.Instance.GameStart();
                    }
                    //ShowDebug("Key 4 pressed  Closed Game Over UI");
                    break;
                case "5":
                    if (GameManager.Instance.startPanel.activeSelf)
                    {
                        return;
                    }
                    else
                    {
                        GameManager.Instance.raiseHandPanel.SetActive(false); //Raise your healthy hand to begin! gesture to close panel 
                        GameManager.Instance.StartLevel();
                    }
                   
                    //ShowDebug("Key 5 pressed  Started Level (Raise Hand Closed)");
                    break;
                case "6":
                    pauseMenu.ReplayGame();
                    break;
                case "7":
                    Application.Quit();
                    break;
                case "8":
                    pauseMenu.ResumeGame();
                    break;
                case "9":
                    GameManager.Instance.NextLevel();
                    break;
                case "10":
                    pauseMenu.PauseGame();
                    break;
            }

        }

    }
}
