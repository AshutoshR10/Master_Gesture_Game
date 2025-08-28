namespace Dino
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine;

    public class PauseMenu : MonoBehaviour
    {
        public static bool isPaused;

        public GameObject pauseMenu;
        private GameManager gameManager;
        void Start()
        {
            pauseMenu.SetActive(false);
            //gameManager = FindObjectOfType<GameManager>();
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found in the scene. Recording pause/resume will not work.");
            }
            CacheGameManager();
        }
        private void CacheGameManager()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found in the scene.");
            }
        }

        public void ReplayGame()
        {
            /*Time.timeScale = 1f;
            if (isPaused)
            {
                isPaused = false;
                gameManager.ResumeRecording();
            }
            //ResumeGame();
            gameManager.StopRecording();

            //SceneManager.sceneLoaded += OnSceneReloaded;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);*/
            Time.timeScale = 1f;
            isPaused = false;
            if (gameManager != null)
            {
                gameManager.StopRecording();
            }

            StartCoroutine(ReplayCurrentSceneAsync());
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        private IEnumerator ReplayCurrentSceneAsync()
        {

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);


            asyncLoad.allowSceneActivation = false;


            while (!asyncLoad.isDone)
            {

                if (asyncLoad.progress >= 0.9f)
                {

                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }


            yield return null;


            KeyBinding.Instance?.FindAndCacheReferences();
        }
        private void OnSceneReloaded(Scene scene, LoadSceneMode mode)
        {

            CacheGameManager();


            KeyBinding.Instance?.FindAndCacheReferences();

            SceneManager.sceneLoaded -= OnSceneReloaded;
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void PauseGame()
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;
        }

        public void ResumeGame()
        {
            pauseMenu.SetActive(false);
            gameManager.ResumeRecording();
            Time.timeScale = 1f;
            isPaused = false;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                {
                    ResumeGame();
                    //gameManager.ResumeRecording();
                }
                else
                {
                    gameManager.PauseRecording();
                    PauseGame();
                }
            }
        }
    }
}
