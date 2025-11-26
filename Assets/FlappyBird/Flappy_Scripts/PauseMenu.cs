namespace Flappy
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
            gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found in the scene. Recording pause/resume will not work.");
            }
        }
        public void ReplayGame()
        {
            // ✅ END SESSION AND SEND DATA TO API (Retry from pause menu)
            if (GameActionTracker.Instance != null && gameManager != null)
            {
                // Note: gameManager.score is public, but survivalTime is private
                // For Level 1 (survival mode), this will send current score (may be 0)
                // TODO: If needed, add public getter in GameManager for current score/time
                GameActionTracker.Instance.EndSession(gameManager.score, "retry");
            }

            Time.timeScale = 1f;
            if (isPaused)
            {
                isPaused = false;
                gameManager.ResumeRecording();
            }
            gameManager.StopRecording();
            StartCoroutine(ReplayCurrentSceneAsyncFlappy());
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator ReplayCurrentSceneAsyncFlappy()
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


            NativeKeyCode.Instance?.FindReferance();
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
            gameManager.ResumeRecording();
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;
        }

        void Update()
        {
            if (!gameManager.isGamePaused)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (isPaused)
                    {
                        ResumeGame();
                        // gameManager.ResumeRecording();
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
}
