namespace BrickBreaker
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class Key_Code : MonoBehaviour
    {
        public static Key_Code Instance;
        private GameManager GameManager;

        public PauseMenu PauseMenu;
        private Paddle paddle;


        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //player = FindFirstObjectByType<Player>(); // reacquire the new player
            //FindAndCacheReferences();
            StartCoroutine(DeferredCacheBrick(scene.name));

            FinfRefBrick();
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
            FinfRefBrick();
        }
        void Start()
        {
            GameManager = GameManager.Instance;
        }

        private IEnumerator DeferredCacheBrick(string sceneName)
        {
            yield return null;
            FinfRefBrick();
        }

        public void FinfRefBrick()
        {
            GameManager = gameObject.GetComponent<GameManager>();
            paddle = gameObject.GetComponent<Paddle>();
            PauseMenu = gameObject.GetComponent<PauseMenu>();

            PauseMenu = FindFirstObjectByType<PauseMenu>();
        }

        public void KeyCodeNative(string Keycode)
        {
            switch (Keycode)
            {
                case "21"://GameStart
                    //GameManager.Instance.GameStartKey();
                    break;
                case "22":
                    if (!GameManager.Instance.gameStarted)
                    {
                        GameManager.Instance.GameStartKey();
                    }
                    else
                    {
                        // ✅ PAUSE FIX: Block paddle movement gesture during pause
                        if (!PauseMenu.isPaused)
                        {
                            Paddle.Instance.LeftMovement();
                        }
                    }
                    break;
                case "23":
                    if (!GameManager.Instance.gameStarted)
                    {
                        GameManager.Instance.GameStartKey();
                    }
                    else
                    {
                        // ✅ PAUSE FIX: Block paddle movement gesture during pause
                        if (!PauseMenu.isPaused)
                        {
                            Paddle.Instance.RightMovement();
                        }
                    }

                    break;
                case "24":
                    PauseMenu.PauseGame();
                    break;
                case "25":
                    PauseMenu.ResumeGame();
                    break;
                case "26":
                    PauseMenu.ReplayGame();
                    break;
                case "27":
                    PauseMenu.QuitGame();
                    break;
                case "28":
                    GameManager.LoadNextLevel();
                    break;
            }
        }

    }
}
