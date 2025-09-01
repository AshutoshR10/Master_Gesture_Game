namespace Flappy
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class NativeKeyCode : MonoBehaviour
    {
        public static NativeKeyCode Instance;
        private Player player;
        private PauseMenu pauseMenu;
        private GameManager GameManager;
        private void Start()
        {
            // Find the player reference
            player = FindFirstObjectByType<Player>();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //player = FindFirstObjectByType<Player>(); // reacquire the new player
            //FindAndCacheReferences();
            StartCoroutine(DeferredCacheFlappy(scene.name));
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
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            FindReferance();
        }

        private IEnumerator DeferredCacheFlappy(string sceneName)
        {
            yield return null; // wait 1 frame
            FindReferance();
            //AddDebug($"[KeyBinding] Cached references for scene: {sceneName}");
        }

        public void FindReferance()
        {
            player = FindFirstObjectByType<Player>();
            GameManager = gameObject.GetComponent<GameManager>();
            pauseMenu = gameObject.GetComponent<PauseMenu>();
        }
        public void NativeKeyCodes(string Keycode)
        {
            switch (Keycode)
            {
                case "15": // Game start - equivalent to Spacebar functionality
                    HandleKey15();
                    break;
                case "16":
                    pauseMenu.PauseGame();
                    break;
                case "17":
                    pauseMenu.ResumeGame();
                    break;
                case "18":
                    pauseMenu.QuitGame();
                    break;
                case "19":
                    pauseMenu.ReplayGame();
                    break;
                case "20":
                    GameManager.LoadNextLevel();
                    break;



            }
        }

        private void HandleKey15()
        {

            if (GameManager.Instance.healthyUnhealthyPanel.activeSelf)
            {
                GameManager.Instance.healthyUnhealthyPanel.SetActive(false);
            }
            else
            {
                player.Jump();
            }

            if (GameManager.Instance.gameOver.activeSelf)
            {
                GameManager.Instance.Play();
            }

            // Second priority: If play button is active, start the game
            /*if (GameManager.Instance.playButton.activeSelf)
            {
                if (GameManager.Instance.first)
                {
                    // First time - start immediately
                    //GameManager.Instance.PlayButtonClick();
                    GameManager.Instance.StartRecording();
                    GameManager.Instance.first = false;
                    //ShowDebug("Key 15 pressed - Game Started (First Time)");
                }
                else
                {
                    // Subsequent times - use delayed start
                    //StartCoroutine(GameManager.Instance.DelayedStart());
                    GameManager.Instance.StartCoroutine(GameManager.Instance.DelayedStart());
                    //ShowDebug("Key 15 pressed - Game Started (Delayed)");
                }
                //return; // Exit after starting game
            }
            else if (player != null && !GameManager.Instance.isGamePaused && Time.timeScale > 0)
            {
                player.Jump();
                ShowDebug("Key 15 pressed - Player Jumped");
            }*/

            GameManager.Instance.GameStart();
            // Third priority: If game is already running, make the player jump
            /*if (player != null && !GameManager.Instance.isGamePaused && Time.timeScale > 0)
            {
                player.Jump();
                ShowDebug("Key 15 pressed - Player Jumped");
            }*/
        }

        private void ShowDebug(string message)
        {
            Debug.Log(message);
            // You could also display this in a UI debug text if you have one
        }
    }
}
