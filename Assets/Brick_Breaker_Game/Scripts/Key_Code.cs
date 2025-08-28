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

        private PauseMenu PauseMenu;
        private Paddle paddle;


        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //player = FindFirstObjectByType<Player>(); // reacquire the new player
            //FindAndCacheReferences();
            StartCoroutine(DeferredCacheBrick(scene.name));
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
        }

        public void KeyCodeNative(string Keycode)
        {
            switch (Keycode)
            {
                case "21"://GameStart
                    GameManager.Instance.GameStartKey();
                    break;
                case "22":
                    Paddle.Instance.LeftMovement();
                    break;
                case "23":
                    PauseMenu.PauseGame();
                    break;
                case "24":
                    PauseMenu.ResumeGame();
                    break;
                case "25":
                    PauseMenu.ReplayGame();
                    break;
                case "26":
                    PauseMenu.QuitGame();
                    break;
                case "27":
                    GameManager.LoadNextLevel();
                    break;
            }
        }

    }
}
