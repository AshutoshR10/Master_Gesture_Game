namespace BrickBreaker
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
            //Time.timeScale = 1f;
            //if (isPaused)
            //{
            //    isPaused = false;
            //    gameManager.ResumeRecording();
            //}
            ResumeGame();
            gameManager.PlayAgain();
            //gameManager.StopRecording();
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            StartCoroutine(ReplayCurrentSceneAsyncBrick());
            //// Use PlayAgain instead of reloading the scene
            //if (gameManager != null)
            //{
            //    ResumeGame();
            //    gameManager.PlayAgain();
            //}
            //else
            //{
            //    Debug.LogError("GameManager not found when trying to replay game");
            //}
        }
        private IEnumerator ReplayCurrentSceneAsyncBrick()
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


            Key_Code.Instance?.FinfRefBrick();
        }
        public void QuitGame()
        {
            //Application.Quit();
#if UNITY_ANDROID

            Application.Quit();
#elif UNITY_IOS
        
        Application.Unload(); 
#else
        
        Application.Quit();
#endif

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
