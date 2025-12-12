namespace Dino
{
    using System.Collections;
    using System.Runtime.CompilerServices;
    using TMPro;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using System.Net.Sockets;
    using System.Text;
    //using global::TMPro;
    using System.Collections.Generic;

    //public enum GameState
    //{
    //    WaitingToStart,
    //    Playing,
    //    GameOver,
    //    LevelComplete,b
    //    Paused
    //}
    [DefaultExecutionOrder(-100)]
    public class GameManager : MasterGameManager
    {
        public enum GameState
        {
            WaitingToStart,
            Playing,
            GameOver,
            LevelComplete,
            Paused
        }
        public static int count = 0;
        public static GameManager Instance { get; private set; }
        //private static bool isRetry = false;

        public float initialGameSpeed = 5f;
        public float gameSpeedIncrease = 0.1f;
        public float gameSpeed { get; private set; }
        // [SerializeField] private TextMeshProUGUI raiseHandPromptText;

        [SerializeField] private List<int> dinoGameScenes = new List<int> { 0, 1, 2 }; // Default: scenes 0, 1, 2


        // [SerializeField] private GameObject raiseHandPanel;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI hiscoreText;
        [SerializeField] private TextMeshProUGUI startText;
        [SerializeField] public TextMeshProUGUI gameOverText;
        [SerializeField] public Button retryButton;

        // Rest screen UI (panel, message text, and buttons)
        [SerializeField] private GameObject restPanel; // the panel that contains the rest UI
        [SerializeField] private TextMeshProUGUI restMessageText; // e.g., "Great job, rest for 2 mins before starting next level"
        [SerializeField] private Button playAgainButton; // button to replay the current level
        [SerializeField] private Button nextLevelButton; // button to go to the next level

        // Start screen UI (panel with start text)
        [SerializeField] public GameObject startPanel; // the panel that contains the start UI (e.g., "Press Space to Start")

        private Player player;
        private Spawner spawner;

        private float score;
        public float Score => score;

        private Coroutine levelDurationCoroutine;

        // This flag ensures we trigger the level completion only once.
        private bool levelCompleteTriggered = false;

        // This flag indicates whether the initial game start has occurred.
        private bool initialGameStarted = false;
        private bool isGameOver = false;

        // patientID used to send data to be stored in the db.
        public static string patientID = "Unknown";

        private TcpClient client;
        private NetworkStream stream;
        private bool isConnected = false;
        private bool gameOverCalled = false;

        private bool first = true;
        private Coroutine retryCoroutine;


        public GameState currentGameState { get; private set; } = GameState.WaitingToStart;
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reassign Player reference
            //player = FindFirstObjectByType<Player>();
            //startPanel = GameObject.Find("Start");

            // Reset critical flags and references

            FindGestureUIsByTag();
            initialGameStarted = false;
            isGameOver = false;
            levelCompleteTriggered = false;
            gameOverCalled = false;

            // Reacquire references
            player = FindFirstObjectByType<Player>();
            spawner = FindFirstObjectByType<Spawner>();

            player.ResetState();
            // Reset UI state
            InitializeUI();
            LoadGameGestures(lastGestureKey);

            Debug.Log($"Scene loaded: {scene.name}, GameState: {currentGameState}");
        }

        private void InitializeUI()
        {
            if (restPanel != null) restPanel.SetActive(false);
            if (startPanel != null) startPanel.SetActive(true);
            if (gameOverText != null) gameOverText.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);

            // Update start text based on count
            if (startText != null)
            {
                startText.text = count == 0 ?
                    "Raise your healthy hand to begin!" :
                    "Raise your unhealthy hand to begin!";
            }
        }

        private void Awake()
        {
            /*SceneManager.sceneLoaded += OnSceneLoaded;
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                //StartCoroutine(GetPatientID());
            }*/

            /*if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }*/

            Instance = this;
            //DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
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
            if (GameActionTracker.Instance != null && currentGameState == GameState.Playing)
            {
                GameActionTracker.Instance.EndSession((int)score, "exit");
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
            //Debug.Log("Sent start");
        }

        public void StopRecording()
        {
            // Call this method when level ends
            if (isConnected)
                SendCommand("STOP");
            //Debug.Log("Sent stop");
        }
        public void PauseRecording()
        {
            // Call this method when level starts
            if (isConnected)
                SendCommand("PAUSE");
            //Debug.Log("Sent pause");
        }
        public void ResumeRecording()
        {
            // Call this method when level starts
            if (isConnected)
                SendCommand("RESUME");
            //Debug.Log("Sent resume");
        }
        void SendCommand(string command)
        {
            if (stream != null && isConnected)
            {
                try
                {
                    byte[] data = Encoding.ASCII.GetBytes(command);
                    stream.Write(data, 0, data.Length);
                    //Debug.Log($"Sent command: {command}");
                }
                catch (System.Exception )
                {
                    //Debug.LogError($"Failed to send command {command}: {e.Message}");
                    isConnected = false;
                }
            }
        }

        private void Start()
        {
            //ConnectToPython();
            StopAllCoroutines();
            player = Object.FindAnyObjectByType<Player>();
            spawner = Object.FindAnyObjectByType<Spawner>();

            // Reset these values when scene loads/reloads
            initialGameStarted = false;
            isGameOver = false;
            levelCompleteTriggered = false;
            gameOverCalled = false;
            score = 0f;  // Reset score explicitly here
            scoreText.text = Mathf.FloorToInt(0).ToString("D5");
            enabled = true;
            currentGameState = GameState.WaitingToStart;

            // Ensure the rest panel is hidden and show the start panel when the scene loads.
            if (restPanel != null)
            {
                restPanel.SetActive(false);
            }
            if (startPanel != null)
            {
                startPanel.SetActive(true);
            }
            if (startText != null)
            {
                if (count == 0)
                {
                    // Debug.Log(firstGame);
                    startText.text = "Raise your healthy hand to begin!";
                    // PlayerPrefs.SetInt("HasPlayedBefore", 1); // Mark as played
                    count++;
                }
                else
                {
                    startText.text = "Raise your unhealthy hand to begin!";
                }
            }

            // Assign button listeners programmatically
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(PlayAgain);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(LoadNextLevel);
            }

            // Update the high score display at start
            UpdateHiscore();
            
        }

       

        public void NewGame()
        {
            //startPanel.SetActive(true);
            Time.timeScale = 1f;
            if (PauseMenu.isPaused)
            {
                PauseMenu.isPaused = false;
            }

            // ✅ FIX: Close pause panel if it was opened before game started
            PauseMenu pauseMenuComponent = FindFirstObjectByType<PauseMenu>();
            if (pauseMenuComponent != null && pauseMenuComponent.pauseMenu != null)
            {
                if (pauseMenuComponent.pauseMenu.activeSelf)
                {
                    pauseMenuComponent.pauseMenu.SetActive(false);
                    Debug.Log("[GameManager] Closed pause panel that was opened before game start");
                }
            }

            currentGameState = GameState.Playing;
            // Reset the gameOverCalled flag
            gameOverCalled = false;
            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }

            // Clear any remaining obstacles.
            Obstacle[] obstacles = Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
            foreach (var obstacle in obstacles)
            {
                Destroy(obstacle.gameObject);
            }
            Time.timeScale = 1f;
            score = 0f;
            isGameOver = false;
            enabled = true;
            scoreText.text = Mathf.FloorToInt(0).ToString("D5");
            gameSpeed = initialGameSpeed;

            // Reactivate game objects.
            if (player != null)
                player.gameObject.SetActive(true);
            if (spawner != null)
                spawner.gameObject.SetActive(true);

            // Hide game over UI (if visible).
            if (gameOverText != null)
                gameOverText.gameObject.SetActive(false);
            if (retryButton != null)
                retryButton.gameObject.SetActive(false);
            Input.ResetInputAxes();

            // ✅ START TRACKING SESSION
            if (GameActionTracker.Instance != null)
            {
                GameActionTracker.Instance.StartSession("DINO");
            }
        }


        public void GameOver()
        {
            // Prevent multiple calls to GameOver
            //if (gameOverCalled) return;
            if (gameOverCalled || !enabled) return;
            gameOverCalled = true;
            currentGameState = GameState.GameOver;
            Time.timeScale = 0f;

            isGameOver = true;
            //SendGameData(patientID, SceneManager.GetActiveScene().buildIndex + 1, (int)score);

            gameSpeed = 0f;
            enabled = false;

            if (player != null)
                player.gameObject.SetActive(false);
            if (spawner != null)
                spawner.gameObject.SetActive(false);

            if (gameOverText != null)
                gameOverText.gameObject.SetActive(true);
            if (retryButton != null)
                retryButton.gameObject.SetActive(true);

            UpdateHiscore();
            initialGameStarted = false;

            // ✅ END SESSION AND SEND DATA TO API
            if (GameActionTracker.Instance != null)
            {
                GameActionTracker.Instance.EndSession((int)score, "lose");
            }

            //Debug.Log("GameOver() called - starting retry input handler");

            // Start the retry input handler
            //StartCoroutine(HandleRetryInputDelayed());
            if (retryCoroutine != null)
            {
                StopCoroutine(retryCoroutine);
            }
            retryCoroutine = StartCoroutine(HandleRestartInput());
        }

        private IEnumerator HandleRestartInput()
        {
            // Wait a bit to ensure the game over state is fully set
            float delayTime = 0.5f;
            Debug.Log($"[{Time.time}] Waiting {delayTime} seconds...");
            yield return new WaitForSecondsRealtime(delayTime);

            while (currentGameState == GameState.GameOver) // Check state instead of enabled
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    //Debug.Log($"[{Time.time}] Space pressed in coroutine - calling NewGame()");
                    NewGame();
                    yield break;
                }
                yield return null;
            }
        }



        // This coroutine waits until 60 seconds have passed since the game first started.
        private IEnumerator LevelDurationCoroutine()
        {
            yield return new WaitForSeconds(120f);
            if (!levelCompleteTriggered)
            {
                StopRecording();
                LevelComplete();
            }
        }

        private void LevelComplete()
        {
            StopRecording();
            levelCompleteTriggered = true; // ensure this is only triggered once.
            currentGameState = GameState.LevelComplete;
            //SendGameData(patientID, SceneManager.GetActiveScene().buildIndex + 1, (int)score);
            UpdateHiscore();

            // Stop gameplay regardless of current state.
            gameSpeed = 0f;
            enabled = false;

            if (player != null)
                player.gameObject.SetActive(false);
            if (spawner != null)
                spawner.gameObject.SetActive(false);

            // Hide any Game Over UI.
            if (gameOverText != null)
                gameOverText.gameObject.SetActive(false);
            if (retryButton != null)
                retryButton.gameObject.SetActive(false);

            // ✅ END SESSION AND SEND DATA TO API
            if (GameActionTracker.Instance != null)
            {
                GameActionTracker.Instance.EndSession((int)score, "completed");
            }

            // Activate the rest screen panel.
            if (restPanel != null)
            {
                restPanel.SetActive(true);
            }
            //if (restMessageText != null)
            //{
            //    restMessageText.text = "Great job! Choose an option to continue.";
            //}

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(true);
            }
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);
            }
        }

        // Method to restart the current level.
        // Method to restart the current level with debug info
        private void PlayAgain()
        {
            //Debug.Log("PlayAgain() called - starting scene reload...");
            //float startTime = Time.realtimeSinceStartup;

            // ✅ END SESSION AND SEND DATA TO API (Retry)
            if (GameActionTracker.Instance != null)
            {
                GameActionTracker.Instance.EndSession((int)score, "retry");
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            //Debug.Log($"Scene reload requested - took {Time.realtimeSinceStartup - startTime} seconds");
        }

        // Method to load the next level.
        public void LoadNextLevel()
        {
            // ✅ SAFETY CHECK: End session before loading next level (if not already ended)
            if (GameActionTracker.Instance != null)
            {
                GameActionTracker.Instance.EndSession((int)score, "completed");
            }

            // ✅ SCENE TRANSITION FIX: Reset pause state before loading next level
            PauseMenu.isPaused = false;
            Time.timeScale = 1f;
            ResumeRecording();
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

            //StartCoroutine(LoadSceneAsyncRoutine(SceneManager.GetActiveScene().buildIndex + 1));
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Find the current scene in our dino scenes list
            int currentIndexInList = dinoGameScenes.IndexOf(currentSceneIndex);

            if (currentIndexInList >= 0 && currentIndexInList < dinoGameScenes.Count - 1)
            {
                // Load next scene in the dino game list
                int nextSceneIndex = dinoGameScenes[currentIndexInList + 1];
                StartCoroutine(LoadSceneAsyncRoutine(nextSceneIndex));
            }

        }

        private IEnumerator LoadSceneAsyncRoutine(int sceneIndex)
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
        void Update()
        {

            //StartGameAndCloseFirstPanel();
            //StartGameFromKey();





            if (enabled && currentGameState == GameState.Playing)
            {
                gameSpeed += gameSpeedIncrease * Time.deltaTime;
                score += gameSpeed * Time.deltaTime;
                scoreText.text = Mathf.FloorToInt(score).ToString("D5");
            }
        }

        public void StartGameFromKey()
        {
            if (currentGameState == GameState.WaitingToStart && !initialGameStarted && !isGameOver)
            {
                /*if (startPanel != null)
                    startPanel.SetActive(false);*/

                initialGameStarted = true;
                currentGameState = GameState.Playing;

                if (first)
                {
                    StartRecording();
                    first = false;
                }

                NewGame();
                if (levelDurationCoroutine != null)
                    StopCoroutine(levelDurationCoroutine);

                levelDurationCoroutine = StartCoroutine(LevelDurationCoroutine());
            }
        }

        public void UnloadApplication()
        {
            Application.Unload();

        }


        public void StartGameAndCloseFirstPanel()
        {

            switch (currentGameState)
            {
                case GameState.WaitingToStart:
                    if (!initialGameStarted && !isGameOver)
                    {
                        initialGameStarted = true;
                        currentGameState = GameState.Playing;
                        //if (startPanel != null) startPanel.SetActive(false);
                        if (first)
                        {
                            StartRecording();
                            first = false;
                        }
                        NewGame();
                        levelDurationCoroutine = StartCoroutine(LevelDurationCoroutine());
                    }
                    break;

                case GameState.GameOver:
                    //Debug.Log($"[{Time.time}] Space pressed in GameOver state - restarting");
                    //NewGame();
                    break;

                case GameState.Playing:
                    // Do nothing here - let Player.cs handle the jump
                    break;

                case GameState.LevelComplete:
                    // Do nothing - level complete screen handles its own input
                    break;
            }

        }

        private void Restart()
        {
            // Reset game state directly
            initialGameStarted = false;
            isGameOver = false;
            levelCompleteTriggered = false;
            score = 0f;  // Explicitly reset score

            // Hide UI elements
            if (gameOverText != null)
                gameOverText.gameObject.SetActive(false);
            if (retryButton != null)
                retryButton.gameObject.SetActive(false);
            if (restPanel != null)
                restPanel.SetActive(false);

            // Show start panel again
            if (startPanel != null)
                startPanel.SetActive(true);

            // Update score display right away
            if (scoreText != null)
                scoreText.text = "0";

            // Instead of reloading the scene, just reset everything
            // Clear obstacles
            Obstacle[] obstacles = Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
            foreach (var obstacle in obstacles)
            {
                Destroy(obstacle.gameObject);
            }

            // Note: We don't call NewGame() here because we want to wait for 
            // the player to press Space again as per your initial game flow
        }
        private void UpdateHiscore()
        {
            //SendGameData(patientID,(int)score);
            float hiscore = PlayerPrefs.GetFloat("hiscore", 0);
            if (score > hiscore)
            {
                hiscore = score;
                PlayerPrefs.SetFloat("hiscore", hiscore);
            }
            hiscoreText.text = Mathf.FloorToInt(hiscore).ToString("D5");
        }

        public void SendGameData(string patientID, int level, int score)
        {
            StartCoroutine(SendData(patientID, level, score));
        }

        IEnumerator SendData(string patientID, int level, int score)
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

