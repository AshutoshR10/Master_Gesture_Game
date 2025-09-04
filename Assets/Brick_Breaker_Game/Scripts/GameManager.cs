namespace BrickBreaker
{
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

    [DefaultExecutionOrder(-1)]
    public class GameManager : MasterGameManager
    {
        public static GameManager Instance { get; private set; }

        private const int NUM_LEVELS = 3;

        private Ball ball;
        private Paddle paddle;
        private Brick[] bricks;

        public int level { get; private set; } = 1;
        public int score { get; private set; } = 0;
        public int lives { get; private set; } = 100;

        // Timer Variables
        private float timeLeft = 120f;
        private bool isPaused = true; // Game starts paused until Spacebar is pressed
        private bool levelCompleteTriggered = false;
        public bool gameStarted = false; // Ensures Start Panel logic
        private static bool isFirstStart = true; // Track if this is the first start

        // UI Elements
        [SerializeField] private GameObject startPanel; // Start Panel UI
        [SerializeField] private TextMeshProUGUI startPanelText; // Text in start panel
        [SerializeField] private GameObject restPanel;
        [SerializeField] private TextMeshProUGUI restMessageText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text hiscoreText;

        [SerializeField] private List<int> brickGameScenes = new List<int> { 10, 11, 12 };

        public static string patientID = "Unknown";
        private TcpClient client;
        private NetworkStream stream;
        private bool isConnected = false;

        private bool first = true;

        private void Awake()
        {
            if (Instance != null)
            {
                //DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
                FindSceneReferences();
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
            //if (Instance == this)
            //{
            //    Instance = null;
            //}
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
            Debug.Log("Sent start!");
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
            SetupStartPanel();
            //ConnectToPython();
            score = 0;  // Set score first
            GameObject scoreObj = GameObject.Find("Score");
            if (scoreObj != null)
            {
                scoreText = scoreObj.GetComponent<Text>();
                if (scoreText != null)
                {
                    scoreText.text = "00000";
                }
            }

            // Assign button listeners for the rest panel
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(PlayAgain);

            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(LoadNextLevel);

            if (restPanel != null)
            {
                restPanel.SetActive(false);
            }
            UpdateHiscore();
        }

        private void SetupStartPanel()
        {
            // Show the start panel and pause the game at every level start
            Time.timeScale = 0f;
            isPaused = true;
            gameStarted = false;

            // If startPanel is null, try to find it in the scene
            if (startPanel == null)
            {
                startPanel = GameObject.Find("Start");
                if (startPanel == null)
                {
                    Debug.LogWarning("Could not find StartPanel in the scene!");
                    return;
                }
            }

            startPanel.SetActive(true);

            // Find the text component if not already assigned
            if (startPanelText == null)
            {
                startPanelText = startPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (startPanelText == null)
                {
                    Debug.LogWarning("TextMeshProUGUI not found in the start panel!");
                    return;
                }
            }

            // Set dynamic text based on whether it's the first start
            if (isFirstStart)
            {
                startPanelText.text = "Raise Healthy Hand";
            }
            else
            {
                startPanelText.text = "Raise Unhealthy Hand";
            }
        }

        private void Update()
        {
            // Start the game when spacebar is pressed
            if (!gameStarted && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
            {
                StartGame();
                if (first)
                {
                    StartRecording();
                    first = false;
                }
            }

            // Only update score text if it exists
            if (scoreText != null)
            {
                scoreText.text = Mathf.FloorToInt(score).ToString("D5");
            }
        }


        public void GameStartKey()
        {
            if (!gameStarted)
            {
                StartGame();
                if (first)
                {
                    StartRecording();
                    first = false;
                }
            }
        }

        private void StartGame()
        {
            gameStarted = true;
            isPaused = false;
            Time.timeScale = 1f;
            FindSceneReferences();
            score = 0;  // Set score first
            GameObject scoreObj = GameObject.Find("Score");
            if (scoreObj != null)
            {
                scoreText = scoreObj.GetComponent<Text>();
                if (scoreText != null)
                {
                    scoreText.text = "00000";
                }
            }
            // Find UI elements if they're null
            //if (scoreText == null)
            //{
            //    GameObject scoreObj = GameObject.Find("Score");

            //}

            // Update score text with null check
            //if (scoreText != null)
            //{
            //    scoreText.text = Mathf.FloorToInt(0).ToString("D5");
            //}
            //else
            //{
            //    Debug.LogWarning("ScoreText is null in StartGame");
            //}

            if (startPanel != null)
            {
                startPanel.SetActive(false); // Hide the start panel
            }

            // After the first start, change the flag for future starts
            if (isFirstStart)
            {
                isFirstStart = false;
            }

            StartCoroutine(LevelTimerCoroutine());
        }

        private void FindSceneReferences()
        {
            ball = Object.FindAnyObjectByType<Ball>();
            paddle = Object.FindAnyObjectByType<Paddle>();
            bricks = Object.FindObjectsByType<Brick>(FindObjectsSortMode.None);

            // Debug logging to help diagnose issues
            Debug.Log($"Scene references found: Ball={ball != null}, Paddle={paddle != null}, Bricks count={bricks?.Length ?? 0}");

            // If no bricks were found, try again after a short delay
            //if (bricks == null || bricks.Length == 0)
            //{
            //    StartCoroutine(DelayedFindBricks());
            //}
        }

        //private IEnumerator DelayedFindBricks()
        //{
        //    yield return new WaitForSeconds(0.2f);
        //    bricks = FindObjectsOfType<Brick>();
        //    Debug.Log($"Delayed bricks search found: {bricks?.Length ?? 0} bricks");
        //}

        private void LoadLevel(int newLevel)
        {
            level = newLevel;
            levelCompleteTriggered = false;
            timeLeft = 120f;

            if (level > NUM_LEVELS)
            {
                Debug.LogWarning("All levels completed");
                return;
            }
            


            

           
            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                if (this != null && Instance != null)
                {
                    StartCoroutine(InitializeAfterSceneLoad());
                }
                FindGestureUIsByTag();


                LoadGameGestures(lastGestureKey);

            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene($"Level{level}");
           
        }




        private void OnLevelLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnLevelLoaded;

            // Wait for one frame to ensure all scene objects are properly initialized
            StartCoroutine(InitializeAfterSceneLoad());
        }

        private IEnumerator InitializeAfterSceneLoad()
        {
            // Wait for the end of the frame to ensure all objects are initialized
            yield return new WaitForEndOfFrame();
            first = true;
            FindSceneReferences();

            // Only try to reset objects if they were found
            if (ball != null && paddle != null)
            {
                ResetGameObjects();
            }
            else
            {
                Debug.LogWarning("Could not find ball or paddle after scene reload");
            }  // Ensure Paddle & Ball are reactivated

            // Find the start panel
            startPanel = GameObject.Find("Start");
            if (startPanel == null)
            {
                Debug.LogWarning("StartPanel not found in the scene!");
            }

            // Find the text in the start panel
            if (startPanel != null)
            {
                startPanelText = startPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (startPanelText == null)
                {
                    Debug.LogWarning("TextMeshProUGUI not found in the start panel!");
                }
            }

            SetupStartPanel();  // Show Start Panel for each level

            // Also find rest panel
            restPanel = GameObject.Find("RestScreen");

            // Reassign UI references with null-checks
            score = 0;  // Set score first
            GameObject scoreObj = GameObject.Find("Score");
            if (scoreObj != null)
            {
                scoreText = scoreObj.GetComponent<Text>();
                if (scoreText != null)
                {
                    scoreText.text = "00000";
                }
            }
            else
            {
                Debug.LogWarning("ScoreText not found in the scene!");
            }

            GameObject hiscoreObj = GameObject.Find("HighScore");
            if (hiscoreObj != null)
            {
                hiscoreText = hiscoreObj.GetComponent<Text>();
            }
            else
            {
                Debug.LogWarning("HiscoreText not found in the scene!");
            }

            // Reconnect button listeners if they're in the new scene
            Button playAgainBtn = GameObject.Find("PlayAgain")?.GetComponent<Button>();
            if (playAgainBtn != null)
            {
                playAgainButton = playAgainBtn;
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(PlayAgain);
            }

            Button nextLevelBtn = GameObject.Find("NextLevel")?.GetComponent<Button>();
            if (nextLevelBtn != null)
            {
                nextLevelButton = nextLevelBtn;
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(LoadNextLevel);
            }
            UpdateHiscore();
        }

        public void OnBallMiss()
        {
            //lives--;

            if (lives > 0)
            {
                ResetLevel();
            }
            else
            {
                GameOver();
            }
        }

        private void ResetLevel()
        {
            paddle.ResetPaddle();
            ball.ResetBall();
        }

        private void GameOver()
        {
            UpdateHiscore();
            int level = SceneManager.GetActiveScene().buildIndex;
            SendGameData(patientID, level + 1, score);
            NewGame();
        }

        private void NewGame()
        {
            score = 0;  // Set score first
            GameObject scoreObj = GameObject.Find("Score");
            if (scoreObj != null)
            {
                scoreText = scoreObj.GetComponent<Text>();
                if (scoreText != null)
                {
                    scoreText.text = "00000";
                }
            }
            lives = 100;
            LoadLevel(1);
        }

        public void OnBrickHit(Brick brick)
        {
            score += brick.points;

            if (Cleared())
            {
                LoadLevel(level + 1);
            }
        }

        private bool Cleared()
        {
            // Check if bricks array is null or empty
            if (bricks == null || bricks.Length == 0)
            {
                // Attempt to find bricks again
                bricks = Object.FindObjectsByType<Brick>(FindObjectsSortMode.None);

                // If still no bricks, log a warning and return false
                if (bricks == null || bricks.Length == 0)
                {
                    Debug.LogWarning("No bricks found in scene when checking if cleared");
                    return false;
                }
            }

            for (int i = 0; i < bricks.Length; i++)
            {
                // Check if the brick reference is valid
                if (bricks[i] != null && bricks[i].gameObject != null && bricks[i].gameObject.activeInHierarchy)
                {
                    return false;
                }
            }
            return true;
        }

        // 60-Second Level Timer that runs independently of player deaths
        private IEnumerator LevelTimerCoroutine()
        {
            float startTime = Time.unscaledTime;
            float elapsedTime = 0f;

            while (elapsedTime < timeLeft)
            {
                if (!isPaused)
                {
                    elapsedTime = Time.unscaledTime - startTime;
                }
                yield return null;
            }

            if (!levelCompleteTriggered)
            {
                StopRecording();
                LevelComplete();
            }
        }

        private void LevelComplete()
        {
            StopRecording();
            UpdateHiscore();
            int level = SceneManager.GetActiveScene().buildIndex;
            SendGameData(patientID, level + 1, score);
            levelCompleteTriggered = true;

            // Stop gameplay and show the rest panel
            if (ball != null) ball.gameObject.SetActive(false);
            if (paddle != null) paddle.gameObject.SetActive(false);

            restPanel.SetActive(true);
            // restMessageText.text = "Time's up! Choose an option to continue.";
        }

        public void PlayAgain()
        {
            // Reset game state variables
            levelCompleteTriggered = false;
            gameStarted = false;
            isPaused = true;
            timeLeft = 120f;
            score = 0;  // Set score first
            GameObject scoreObj = GameObject.Find("Score");
            if (scoreObj != null)
            {
                scoreText = scoreObj.GetComponent<Text>();
                if (scoreText != null)
                {
                    scoreText.text = "00000";
                }
            }

            // Update score text
            //if (scoreText != null)
            //{
            //    scoreText.text = Mathf.FloorToInt(0).ToString("D5");
            //}

            // Stop any active coroutines
            StopAllCoroutines();

            // Stop any previous recordings
            StopRecording();

            // Hide the rest panel
            if (restPanel != null)
            {
                restPanel.SetActive(false);
            }

            // Re-enable paddle and ball
            if (ball != null)
            {
                ball.gameObject.SetActive(true);
                ball.ResetBall();
            }

            if (paddle != null)
            {
                paddle.gameObject.SetActive(true);
                paddle.ResetPaddle();
            }

            // Reset bricks
            //ResetBricks();

            // Show start panel with appropriate text
            SetupStartPanel();

            // Prepare for the next timer when the game starts
            first = true;
        }

        //public void LoadNextLevel()
        //{
        //    Time.timeScale = 1f;
        //    int nextLevel = level + 1;

        //    if (nextLevel > NUM_LEVELS)
        //    {
        //        //nextLevel = 1; // Restart at Level 1 if all levels are completed
        //        Debug.LogWarning("All levels completed");
        //    }

        //    LoadLevel(nextLevel);
        //}
        public void LoadNextLevel()
        {
            Time.timeScale = 1f;
            /*int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            int nextBuildIndex = currentBuildIndex + 1;

            // Check if next level exists
            if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
            }
            else
            {
                Debug.LogWarning("All levels completed");
                // Or loop back to first level: SceneManager.LoadScene(0);
            }*/

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Find the current scene in our dino scenes list
            int currentIndexInList = brickGameScenes.IndexOf(currentSceneIndex);

            if (currentIndexInList >= 0 && currentIndexInList < brickGameScenes.Count - 1)
            {
                // Load next scene in the dino game list
                int nextSceneIndex = brickGameScenes[currentIndexInList + 1];
                StartCoroutine(LoadSceneAsyncRoutineBrick(nextSceneIndex));
            }
        }

        private IEnumerator LoadSceneAsyncRoutineBrick(int sceneIndex)
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


        private void ResetGameObjects()
        {
            // Reactivate Paddle and Ball after loading the new scene
            if (ball != null) ball.gameObject.SetActive(true);
            if (paddle != null) paddle.gameObject.SetActive(true);
        }

        //private void ResetBricks()
        //{
        //    // Find all bricks in the scene
        //    bricks = FindObjectsOfType<Brick>();

        //    // Reactivate all bricks
        //    foreach (Brick brick in bricks)
        //    {
        //        brick.gameObject.SetActive(true);
        //    }
        //}

        private void UpdateHiscore()
        {
            float hiscore = PlayerPrefs.GetFloat("hiscore", 0);
            if (score > hiscore)
            {
                hiscore = score;
                PlayerPrefs.SetFloat("hiscore", hiscore);
            }
            hiscoreText.text = "HI:" + Mathf.FloorToInt(hiscore).ToString("D5");
        }

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

        public void PrepareForSceneReload()
        {
            // Clear references to scene objects that will be destroyed
            ball = null;
            paddle = null;
            bricks = null;

            // Clear all UI references
            scoreText = null;
            hiscoreText = null;
            startPanel = null;
            startPanelText = null;
            restPanel = null;
            restMessageText = null;
            playAgainButton = null;
            nextLevelButton = null;

            // Reset game state
            score = 0;
            levelCompleteTriggered = false;
            isPaused = true;
            gameStarted = false;
            timeLeft = 120f;

            // Stop any active coroutines
            StopAllCoroutines();

            // Stop recording if it's active
            StopRecording();
        }
    }
}