namespace Dino
{
    using TMPro;
    using UnityEngine;
    using System.Collections;

#if UNITY_IOS
using System.Runtime.InteropServices;
using JetBrains.Annotations;

#endif

    using UnityEngine.Android;
    using UnityEngine.SceneManagement;
    //using global::TMPro;

    /*#if UNITY_IOS
    public class NativeAPI
    {
        [DllImport("__Internal")]
        public static extern void sendMessageToMobileApp(string message);
    }
    #endif
    */
    public class KeyBinding : MonoBehaviour
    {
        public PauseMenu pauseMenu;
        /*public AndroidJavaObject activity;
        public AndroidJavaObject communicationBridge;
        public AndroidJavaClass unityPlayer;*/


        public static KeyBinding Instance;

        private Player player;
        private GameManager gameManager;


        // Reference to the debug text
        [SerializeField] private TMP_Text debugText;
        private string debugLog = "";

        /*private void Start()
        {
    #if UNITY_ANDROID
            unityPlayer = new("com.unity3d.player.UnityPlayer");
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            communicationBridge = new AndroidJavaObject("com.Zigurous.Dino-Game.SetDataFromUnity");
    #endif
        }*/

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //player = FindFirstObjectByType<Player>(); // reacquire the new player
            //FindAndCacheReferences();
            StartCoroutine(DeferredCache(scene.name));
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Awake()
        {


            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                //Destroy(gameObject);
            }

            /*player = FindFirstObjectByType<Player>();
            // Auto-find if not assigned
            if (debugText == null)
                debugText = GameObject.Find("DebugText")?.GetComponent<TMP_Text>();*/

            SceneManager.sceneLoaded += OnSceneLoaded;
            FindAndCacheReferences();
        }

        private void Start()
        {
            // Cache first scene references
            //FindAndCacheReferences();
        }
        private IEnumerator DeferredCache(string sceneName)
        {
            yield return null; // wait 1 frame
            FindAndCacheReferences();
            //AddDebug($"[KeyBinding] Cached references for scene: {sceneName}");
        }

        public void FindAndCacheReferences()
        {

            player = FindFirstObjectByType<Player>();
            gameManager = FindFirstObjectByType<GameManager>();
            pauseMenu = FindFirstObjectByType<PauseMenu>();

            //gameManager = GameManager.Instance;


            if (debugText == null)
                debugText = GameObject.Find("DebugText")?.GetComponent<TMP_Text>();

            //AddDebug($"References cached - Player: {player != null}, GameManager: {gameManager != null}");
        }

        public void OnKeyPressed(string keyCode)
        {

            /* if (keyCode == "1")
             {
                 player?.Jump();
             }*/


            //AddDebug($"Key pressed: {keyCode}");
            // 🔍 DEBUG: Log every gesture received
            Debug.Log($"[KeyBinding] 📱 Gesture received: '{keyCode}' - isPaused: {PauseMenu.isPaused}");

            // Ensure we have updated references
            if (player == null || gameManager == null)
            {
                FindAndCacheReferences();
            }

            switch (keyCode)
            {
                /* case "1": // Jump
                     if (GameManager.Instance.startPanel != null)
                     {
                         GameManager.Instance.startPanel.SetActive(false);
                         AddDebug("Start panel closed");
                     }
                     player?.Jump();
                     break;

                 default:
                     Debug.LogWarning($"Unknown key code: {keyCode}");
                     break;*/
                case "1":
                    /* if (GameManager.Instance != null)
                     {

                         GameManager.Instance.startPanel.SetActive(false);
                         //GameManager.Instance.StartGameFromKey();
                         GameManager.Instance.StartGameAndCloseFirstPanel();


                         if (GameManager.Instance.currentGameState == GameManager.GameState.Playing)
                         {
                             player?.Jump();
                         }
                     }*/
                    HandleJumpAndStart();
                    break;
                case "UNLOAD":
                    GameManager.Instance.UnloadApplication();
                    break;
                case "EXIT":
                    //Application.Quit();
                    Application.Unload();
                    break;
                case "RESTART":
                    pauseMenu.ReplayGame();
                    //AddDebug($"Key pressed: {keyCode}");
                    break;
                case "PAUSE":
                    pauseMenu.PauseGame();
                    break;
                case "RESUME":
                    pauseMenu.ResumeGame();
                    break;
                case "REPLAY":
                    //pauseMenu.ReplayGame();
                    GameManager.Instance.NewGame();
                    break;
                default:
                    Debug.LogWarning($"Unknown key code: {keyCode}");
                    break;
                    /* case "1": // Jump

                         player?.Jump();
                         break;
                     case "2":
                         if (GameManager.Instance.startPanel != null)
                         {
                             GameManager.Instance.startPanel.SetActive(false);
                             AddDebug("Start panel closed");
                         }
                         GameManager.Instance.StartGameAndCloseFirstPanel();
                         break;

                     default:
                         Debug.LogWarning($"Unknown key code: {keyCode}");
                         break;*/

            }
        }



        private void HandleJumpAndStart()
        {
            // 🔍 DEBUG: Log when this method is called
            Debug.Log($"[KeyBinding] HandleJumpAndStart called - isPaused: {PauseMenu.isPaused}");

            if (gameManager == null)
            {
                //AddDebug("GameManager not found!");
                return;
            }
            if (gameManager.gameOverText.isActiveAndEnabled)
            {
                Debug.Log("[KeyBinding] GameOver screen active - calling NewGame()");
                gameManager.NewGame();
            }
            // Handle start panel and game start
            if (gameManager.startPanel != null && gameManager.startPanel.activeSelf)
            {
                Debug.Log("[KeyBinding] Start panel active - calling StartGameFromKey()");
                gameManager.startPanel.SetActive(false);
                gameManager.StartGameFromKey();
                //  AddDebug("Game started from key press");
            }
            else
            {
                // ✅ PAUSE FIX: Block jump gesture input during pause (like Space game)
                if (PauseMenu.isPaused)
                {
                    Debug.Log("[KeyBinding] 🚫 BLOCKED: Jump gesture during pause!");
                    return;
                }

                // Handle jump only if game is in playing state
                if (gameManager.currentGameState == GameManager.GameState.Playing)
                {
                    if (player != null)
                    {
                        Debug.Log("[KeyBinding] Calling player.Jump()");
                        player.Jump();
                        //AddDebug("Jump executed");
                    }
                    else
                    {
                        //AddDebug("Player not found for jump!");
                    }
                }
                else
                {
                    Debug.Log($"[KeyBinding] Cannot jump - Game state: {gameManager.currentGameState}");
                    //AddDebug($"Cannot jump - Game state: {gameManager.currentGameState}");
                }
            }
        }

        public void AddDebug(string message)
        {
            debugLog = $"[{Time.time:F1}s] {message}\n{debugLog}";

            if (debugText != null)
            {
                debugText.text = debugLog;
            }

            Debug.Log(message);
        }
    }
}
