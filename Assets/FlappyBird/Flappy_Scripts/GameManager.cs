namespace Flappy
{
    using global::TMPro;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Text;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine.VFX;

    [DefaultExecutionOrder(-1)]
    public class GameManager : MasterGameManager
    {
        [SerializeField] public GameObject healthyUnhealthyPanel;
        [SerializeField] private TextMeshProUGUI huText;
        public static GameManager Instance { get; private set; }
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Player player;
        [SerializeField] private Spawner spawner;
        [SerializeField] private Text scoreText;  // Classic Score UI
        [SerializeField] private Text survivalScoreText;  // Survival Score UI
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text highSurvivalScoreText;
        [SerializeField] public GameObject playButton;
        [SerializeField] public GameObject gameOver;
        [SerializeField] private Text pauseMessage; // UI text to show during pause

        // Rest screen UI elements
        [SerializeField] private GameObject restPanel;
        [SerializeField] private TextMeshProUGUI restMessageText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button nextLevelButton;

        [SerializeField] private List<int> flappyGameScenes = new List<int> { 7, 8, 9 };

        public int score { get; private set; } = 0; // Classic score
        private float survivalTime = 0f; // Time survived
        private float startTime; // Tracks when the game starts
                                 // High scores
        private int highScore = 0;
        private float highSurvivalTime = 0f;
        private bool levelCompleteTriggered = false;
        public bool isGamePaused = true;  // Track whether the game is currently paused
        private bool initialGameStarted = false;
        private bool gameOverTriggered = false;
        //private int currentLevel;
        private string current_Level;

        //[SerializeField] private List<string> flappyGameScenes = new List<string> { "FlappyBirdlvl1", "FlappyBirdlvl2", "FlappyBirdlvl3" };

        public static string patientID = "Unknown";
        private TcpClient client;
        private NetworkStream stream;
        private bool isConnected = false;
        public bool first = true;
        private static bool first_UI = true;

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                //StartCoroutine(GetPatientID());
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindGestureUIsByTag();


            LoadGameGestures(lastGestureKey);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
            {
                Instance = null;
            }

        }

        [System.Serializable]
        private class PatientIDResponse
        {
            public string patient_id;
        }

        IEnumerator GetPatientID()
        {
            Debug.Log("Requesting Patient ID from Python...");
            UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:5000/get_patient_id");

            yield return request.SendWebRequest();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                patientID = JsonUtility.FromJson<PatientIDResponse>(jsonResponse).patient_id;
                PlayerPrefs.SetString("PatientID", patientID);
                PlayerPrefs.Save();
                Debug.Log("Patient ID received: " + patientID);
            }
            else
            {
                Debug.LogError("Failed to fetch Patient ID: " + request.error);
            }
        }

        void OnApplicationQuit()
        {
            // This will be called when the application is about to close
            Debug.Log("Application quitting - stopping recording");

            // ✅ END SESSION AND SEND DATA TO API (Exit)
            if (GameActionTracker.Instance != null && !isGamePaused)
            {
                int finalScore = (current_Level == "FlappyBirdlvl1") ? (int)survivalTime : score;
                GameActionTracker.Instance.EndSession(finalScore, "exit");
            }

            StopRecording();

            // Add a small delay to ensure message is sent before closing
            try
            {
                // Send a special QUIT command to notify Python that Unity is closing
                if (isConnected)
                    SendCommand("STOP");

                // Give it a moment to send the message
                System.Threading.Thread.Sleep(100);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during quit: {e.Message}");
            }

            // Close connections
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
        }
        void ConnectToPython()
        {
            try
            {
                client = new TcpClient("localhost", 9999);
                stream = client.GetStream();
                isConnected = true;
                Debug.Log("Connected to Python script");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to connect to Python: {e.Message}");
            }
        }

        public void StartRecording()
        {
            // Call this method when level starts
            if (isConnected)
                SendCommand("START");
            Debug.Log("Sent start");
        }

        public void StopRecording()
        {
            // Call this method when level ends
            if (isConnected)
                SendCommand("STOP");
            Debug.Log("Sent stop");
        }
        public void PauseRecording()
        {
            // Call this method when level starts
            if (isConnected)
                SendCommand("PAUSE");
            Debug.Log("Sent pause");
        }
        public void ResumeRecording()
        {
            // Call this method when level starts
            if (isConnected)
                SendCommand("RESUME");
            Debug.Log("Sent resume");
        }
        void SendCommand(string command)
        {
            if (stream != null)
            {
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
                Debug.Log($"Sent command: {command}");
            }
        }

        private void Start()
        {
            Pause();
            //ConnectToPython();
            // Assign button listeners for rest panel options
            playAgainButton.onClick.AddListener(PlayAgain);
            nextLevelButton.onClick.AddListener(LoadNextLevel);

            // Get current level
            //currentLevel = SceneManager.GetActiveScene().buildIndex;
            current_Level = SceneManager.GetActiveScene().name;

            if (playButton.GetComponent<Button>() != null)
            {
                playButton.GetComponent<Button>().onClick.AddListener(PlayButtonClick);
            }

            if (current_Level == "FlappyBirdlvl1")
            {
                survivalScoreText.gameObject.SetActive(true);
                highSurvivalScoreText.gameObject.SetActive(true);
                scoreText.gameObject.SetActive(false);
                highScoreText.gameObject.SetActive(false);
            }
            else
            {
                survivalScoreText.gameObject.SetActive(false);
                highSurvivalScoreText.gameObject.SetActive(false);
                scoreText.gameObject.SetActive(true);
                highScoreText.gameObject.SetActive(true);
            }
            //StartCoroutine(LevelTimerCoroutine());
            // Show the Healthy/Unhealthy panel and set initial text
            healthyUnhealthyPanel.SetActive(true);
            if (first_UI)
            {
                huText.text = "Raise your Healthy Hand";
                first_UI = false;
            }
            else
                huText.text = "Raise your Unhealthy Hand";
            LoadHighScores();


        }

        public void PlayButtonClick()
        {
            if (playButton.activeSelf)
            {
                // Reset the game over flag when play button is clicked
                gameOverTriggered = false;

                if (!initialGameStarted)
                {
                    initialGameStarted = true;
                    StartCoroutine(LevelTimerCoroutine());
                }
                ClearExistingPipes();
                Play();
            }
        }

        private void ClearExistingPipes()
        {
            // Find all pipes in the scene and destroy them
            Pipes[] existingPipes = Object.FindObjectsByType<Pipes>(FindObjectsSortMode.None);
            foreach (Pipes pipe in existingPipes)
            {
                Destroy(pipe.gameObject);
            }
            Debug.Log($"Cleared {existingPipes.Length} existing pipes");
        }
        // private void Update()
        // {
        //     // Check for Spacebar press to activate the play button when it's visible
        //     if (Input.GetKeyDown(KeyCode.Space) && playButton.activeSelf)
        //     {
        //         PlayButtonClick();
        //         if (first)
        //         {
        //             StartRecording();
        //             first = false;
        //         }
        //     }

        //     // Update survival time if in Level 1
        //     if (!isGamePaused && currentLevel == 0)
        //     {
        //         survivalTime = Time.time - startTime;
        //         survivalScoreText.text = "Time: " + survivalTime.ToString("F1") + "s"; // Display with 1 decimal
        //     }
        // }
        private void Update()
        {
            //     if (isGamePaused)
            //     {
            //         return;
            // }
            // On Spacebar press
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (healthyUnhealthyPanel.activeSelf)
                {
                    healthyUnhealthyPanel.SetActive(false); // Hide panel

                }
                if (playButton.activeSelf)
                {
                    //StartCoroutine(DelayedStart());
                    //PlayButtonClick(); // Call after delay

                    if (first)
                    {
                        PlayButtonClick();
                        StartRecording();

                    }
                    else
                    {
                        StartCoroutine(DelayedStart());
                    }
                    //PlayButtonClick(); // Start the game
                    //if (first)
                    //{
                    //    StartRecording();
                    //    first = false;
                    //}
                }
            }

            // Survival Time update
            if (!isGamePaused && current_Level == "FlappyBirdlvl1")
            {
                survivalTime = Time.time - startTime;
                survivalScoreText.text = "Time: " + survivalTime.ToString("F1") + "s";
            }
        }

        public void GameStart()
        {
            if (healthyUnhealthyPanel.activeSelf)
            {
                healthyUnhealthyPanel.SetActive(false); // Hide panel

            }
            if (playButton.activeSelf)
            {
                //StartCoroutine(DelayedStart());
                //PlayButtonClick(); // Call after delay

                if (first)
                {
                    PlayButtonClick();
                    StartRecording();

                }
                else
                {
                    StartCoroutine(DelayedStart());
                }
                //PlayButtonClick(); // Start the game
                //if (first)
                //{
                //    StartRecording();
                //    first = false;
                //}
            }
        }

        public IEnumerator DelayedStart()
        {
            Debug.Log($"[{Time.time}] DelayedStart coroutine started. GameObject active: {gameObject.activeInHierarchy}");

            // Use a shorter delay
            float delayTime = 0.4f;
            Debug.Log($"[{Time.time}] Waiting {delayTime} seconds...");
            yield return new WaitForSecondsRealtime(delayTime);

            Debug.Log($"[{Time.time}] Delay completed. GameObject still active: {gameObject.activeInHierarchy}");

            // Check if the button is still active
            if (playButton != null && playButton.activeSelf)
            {
                Debug.Log($"[{Time.time}] Calling PlayButtonClick...");
                PlayButtonClick();
                Debug.Log($"[{Time.time}] PlayButtonClick called");
            }
            else
            {
                Debug.Log($"[{Time.time}] PlayButton is no longer active or null. Active: {playButton != null && playButton.activeSelf}");
            }
        }



        public void Pause()
        {
            Time.timeScale = 0f;
            player.enabled = false;
            isGamePaused = true;
            //player.ResetPlayer();

            if (pauseMessage != null) pauseMessage.enabled = true;
        }

        // public void Play()
        // {
        //     //initialGameStarted = true;
        //     gameOverTriggered = false;

        //     score = 0;
        //     survivalTime = 0;
        //     startTime = Time.time; // Reset start time

        //     scoreText.text = "Score: " + score;
        //     survivalScoreText.text = "Time: 0.0s";

        //     playButton.SetActive(false);
        //     gameOver.SetActive(false);
        //     restPanel.SetActive(false);

        //     Time.timeScale = 1f;
        //     player.enabled = true;
        //     isGamePaused = false;

        //     if (pauseMessage != null) pauseMessage.enabled = false;
        // }
        public void Play()
        {
            gameOverTriggered = false;

            score = 0;
            survivalTime = 0;
            startTime = Time.time;

            scoreText.text = "Score: " + score;
            survivalScoreText.text = "Time: 0.0s";

            playButton.SetActive(false);
            gameOver.SetActive(false);
            restPanel.SetActive(false);

            // Reset player position and velocity
            ResetPlayerPosition();
            if (spawner != null)
            {
                // Disable and re-enable to restart the spawning cycle
                spawner.gameObject.SetActive(false);
                spawner.gameObject.SetActive(true);
            }

            Time.timeScale = 1f;
            player.enabled = true;
            isGamePaused = false;

            if (pauseMessage != null) pauseMessage.enabled = false;

            // ✅ START TRACKING SESSION
            if (GameActionTracker.Instance != null)
            {
                GameActionTracker.Instance.StartSession("FLAPPY");
            }
        }

        // Add this new method after Play()
        private void ResetPlayerPosition()
        {
            if (player != null)
            {
                // Reset velocity
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                // Reset direction in Player script
                player.ResetDirection();

                // Set position to respawn point
                if (respawnPoint != null)
                {
                    player.transform.position = respawnPoint.position;
                    Debug.Log($"Player respawned at: {respawnPoint.position}");
                }
                else
                {
                    // Fallback if no respawn point is set
                    player.transform.position = new Vector3(-5f, 0f, 0f);
                    Debug.LogWarning("No respawn point set! Using default position.");
                }
            }
            else
            {
                Debug.LogError("Player reference is missing in GameManager!");
            }
        }


        public void GameOver()
        {
            if (!levelCompleteTriggered)
            {
                gameOverTriggered = true;
                playButton.SetActive(true);
                gameOver.SetActive(true);
                /*if (currentLevel >= 1)
                {
                    SendGameData(patientID, currentLevel + 1, score);
                }
                else
                {
                    SendGameData(patientID, currentLevel + 1, survivalTime);
                }*/

                // Update high scores before pausing
                UpdateHighScores();

                // ✅ END SESSION AND SEND DATA TO API
                if (GameActionTracker.Instance != null)
                {
                    int finalScore = (current_Level == "FlappyBirdlvl1") ? (int)survivalTime : score;
                    GameActionTracker.Instance.EndSession(finalScore, "lose");
                }

                Pause();
                //StartCoroutine(RestartGameAfterDelay());
            }

        }
        private IEnumerator RestartGameAfterDelay()
        {
            yield return new WaitForSeconds(1f); // Small delay before restart
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reload level
        }

        public void IncreaseScore()
        {
            /* if (currentLevel >= 1)  // Only increase score in levels after Level 1
             {
                 score++;
                 scoreText.text = "Score: " + score;
             }*/

            if (current_Level != "FlappyBirdlvl1")
            {
                score++;
                scoreText.text = "Score: " + score;
            }
        }

        private IEnumerator LevelTimerCoroutine()
        {
            Debug.Log("Started");
            yield return new WaitForSeconds(120f); // Wait 60 seconds before ending the level
            if (!levelCompleteTriggered)
            {
                StopRecording();
                LevelComplete();
            }
        }

        private void LevelComplete()
        {
            StopRecording();
            levelCompleteTriggered = true;
            gameOverTriggered = true;
/*
            if (currentLevel >= 1)
            {
                SendGameData(patientID, currentLevel + 1, score);
            }
            else
            {
                SendGameData(patientID, currentLevel + 1, survivalTime);
            }*/
            Pause();

            restPanel.SetActive(true);
            restMessageText.text = "Great job! Select an option to continue.";
            playButton.SetActive(false);
            gameOver.SetActive(false);

            if (pauseMessage != null) pauseMessage.enabled = false;

            UpdateHighScores();

            // ✅ END SESSION AND SEND DATA TO API
            if (GameActionTracker.Instance != null)
            {
                int finalScore = (current_Level == "FlappyBirdlvl1") ? (int)survivalTime : score;
                GameActionTracker.Instance.EndSession(finalScore, "completed");
            }
        }

        public void PlayAgain()
        {
            // ✅ END SESSION AND SEND DATA TO API (Retry)
            if (GameActionTracker.Instance != null)
            {
                int finalScore = (current_Level == "FlappyBirdlvl1") ? (int)survivalTime : score;
                GameActionTracker.Instance.EndSession(finalScore, "retry");
            }

            first_UI = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextLevel()
        {
            // ✅ SAFETY CHECK: End session before loading next level (if not already ended)
            if (GameActionTracker.Instance != null)
            {
                int finalScore = (current_Level == "FlappyBirdlvl1") ? (int)survivalTime : score;
                GameActionTracker.Instance.EndSession(finalScore, "completed");
            }

            first_UI = false;
            //currentLevel++;
            Time.timeScale = 1f;
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Find the current scene in our dino scenes list
            int currentIndexInList = flappyGameScenes.IndexOf(currentSceneIndex);

            if (currentIndexInList >= 0 && currentIndexInList < flappyGameScenes.Count - 1)
            {
                // Load next scene in the dino game list
                int nextSceneIndex = flappyGameScenes[currentIndexInList + 1];
                StartCoroutine(LoadSceneAsyncRoutineFlappy(nextSceneIndex));
            }
        }

        private IEnumerator LoadSceneAsyncRoutineFlappy(int sceneIndex)
        {
            yield return new WaitForSeconds(0.1f);
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

            // Optional: Prevent scene activation until you're ready
            asyncLoad.allowSceneActivation = false;

            // While the scene is still loading, report progress or do other tasks
            while (!asyncLoad.isDone)
            {
                // Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");

                // When loading is done (progress >= 0.9f), allow activation
                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }

            // Scene is loaded, references will be reset via your existing sceneLoaded handler
        }

        private void UpdateHighScores()
        {
            if (current_Level == "FlappyBirdlvl1")
            {
                // Update high score for survival time in Level 1
                if (survivalTime > highSurvivalTime)
                {
                    highSurvivalTime = survivalTime;
                    PlayerPrefs.SetFloat("HighSurvivalTime", highSurvivalTime);

                    // Update the UI text immediately
                    if (highSurvivalScoreText != null)
                    {
                        highSurvivalScoreText.text = "HI: " + highSurvivalTime.ToString("F1") + "s";
                    }
                }
            }
            else
            {
                // Update high score for points in Levels 2+
                if (score > highScore)
                {
                    highScore = score;
                    PlayerPrefs.SetInt("HighScore", highScore);

                    // Update the UI text immediately
                    if (highScoreText != null)
                    {
                        highScoreText.text = "HI: " + highScore;
                    }
                }
            }

            PlayerPrefs.Save();
        }

        private void LoadHighScores()
        {
            highScore = PlayerPrefs.GetInt("HighScore", 0);
            highSurvivalTime = PlayerPrefs.GetFloat("HighSurvivalTime", 0f);


            if (highSurvivalScoreText != null)
            {
                highSurvivalScoreText.text = "HI: " + highSurvivalTime.ToString("F1") + "s";
            }
            if (highScoreText != null)
            {
                highScoreText.text = "HI: " + highScore;
            }
        }

        public int GetHighScore() => highScore;
        public float GetHighSurvivalTime() => highSurvivalTime;

        public void SendGameData(string patientID, int level, float score)
        {
            StartCoroutine(SendData(patientID, level, score));
        }

        IEnumerator SendData(string patientID, int level, float score)
        {
            // Create JSON payload
            string jsonData = $"{{\"patient_id\": \"{patientID}\", \"level\":{level},\"score\": {score}}}";

            // Convert to byte array
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // Set up POST request
            using (UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5000/receive_data", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Send request
                yield return request.SendWebRequest();

                // Handle response
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Data sent successfully: " + request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("Error sending data: " + request.error);
                }
            }
        }
    }
}
