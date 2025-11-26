using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton script that tracks game actions across all mini-games.
/// Persists across scene loads using DontDestroyOnLoad.
/// Saves actions every 3 seconds internally, sends to API only on game end.
/// </summary>
public class GameActionTracker : MonoBehaviour
{
    // ========================
    // SINGLETON PATTERN
    // ========================
    public static GameActionTracker Instance { get; private set; }

    // ========================
    // DATA CLASSES
    // ========================
    [System.Serializable]
    public class ActionData
    {
        public string action;      // "jump", "move_left", "fire", etc.
        public float time;         // Timestamp when action occurred
    }

    [System.Serializable]
    public class GameProgressData
    {
        public string gesture;                  // Human-readable gesture name
        public List<ActionData> actions;        // All actions performed
    }

    // ========================
    // TRACKING VARIABLES
    // ========================
    private bool isTracking = false;
    private string currentGameId = "";
    private string currentGestureName = "";  // ✅ Store gesture when session starts
    private int currentScore = 0;
    private float sessionStartTime = 0f;

    // Two-stage storage
    private List<ActionData> pendingActions = new List<ActionData>();    // Temporary (0-3 seconds)
    private List<ActionData> savedActions = new List<ActionData>();      // Permanent storage

    private Coroutine autoSaveCoroutine = null;

    // ========================
    // UNITY LIFECYCLE
    // ========================
    void Awake()
    {
        // Singleton setup - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // ✅ PERSISTS ACROSS ALL SCENES
            Debug.Log("[GameActionTracker] Initialized and persisted across scenes");
        }
        else
        {
            Destroy(gameObject);  // Destroy duplicates
        }
    }

    // ========================
    // PUBLIC METHODS
    // ========================

    /// <summary>
    /// Start tracking a new game session
    /// Call this in each game's Start() or NewGame() method
    /// </summary>
    /// <param name="gameId">Game identifier: "DINO", "SPACE", "FLAPPY", "BRICK"</param>
    public void StartSession(string gameId)
    {
        Debug.Log($"[GameActionTracker] ========================================");
        Debug.Log($"[GameActionTracker] StartSession() called for: {gameId}");
        Debug.Log($"[GameActionTracker] ========================================");

        // Stop any previous session
        if (isTracking)
        {
            Debug.LogWarning($"[GameActionTracker] ⚠️ WARNING: Starting new session '{gameId}' while '{currentGameId}' was still active");
            Debug.LogWarning($"[GameActionTracker] Previous session will be stopped without sending data.");
            StopTracking();
        }

        // ✅ CAPTURE GESTURE AT SESSION START (from MasterGameManager.lastGestureKey)
        // This is THE ONLY place where gesture is captured!
        currentGestureName = GetGestureNameFromMasterGameManager();

        // Initialize new session
        currentGameId = gameId;
        currentScore = 0;
        sessionStartTime = Time.time;
        pendingActions.Clear();
        savedActions.Clear();
        isTracking = true;

        // Start auto-save coroutine (runs every 3 seconds)
        autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());

        Debug.Log($"[GameActionTracker] ✅ SESSION STARTED");
        Debug.Log($"[GameActionTracker]    Game ID: {currentGameId}");
        Debug.Log($"[GameActionTracker]    Gesture: {currentGestureName} (LOCKED for this session)");
        Debug.Log($"[GameActionTracker]    Tracking: ACTIVE");
        Debug.Log($"[GameActionTracker]    Auto-save: Every 3 seconds");
        Debug.Log($"[GameActionTracker] ========================================");
    }

    /// <summary>
    /// Record a player action
    /// Call this whenever player performs an action (jump, move, fire, etc.)
    /// </summary>
    /// <param name="actionName">Name of action: "jump", "move_left", "fire", etc.</param>
    public void RecordAction(string actionName)
    {
        if (!isTracking)
        {
            Debug.LogWarning("[GameActionTracker] Tried to record action but no session is active");
            return;
        }

        // Add to temporary pending list
        pendingActions.Add(new ActionData
        {
            action = actionName,
            time = Time.time - sessionStartTime  // Relative time from session start
        });

        // No API call - just stored in RAM
    }

    /// <summary>
    /// End the current session and send data to API
    /// Call this on: GameOver, LevelComplete, Retry, Exit
    /// </summary>
    /// <param name="finalScore">Final game score</param>
    /// <param name="result">"completed", "lose", "retry", or "exit"</param>
    public void EndSession(int finalScore, string result)
    {
        Debug.Log($"[GameActionTracker] ========================================");
        Debug.Log($"[GameActionTracker] EndSession() called - Result: {result}");
        Debug.Log($"[GameActionTracker] ========================================");

        if (!isTracking)
        {
            Debug.LogWarning("[GameActionTracker] ⚠️ WARNING: EndSession called but no active session");
            Debug.LogWarning("[GameActionTracker] This might be a duplicate call. No data will be sent.");
            return;
        }

        currentScore = finalScore;

        // Stop auto-save
        StopTracking();

        // Add any remaining pending actions to saved actions
        if (pendingActions.Count > 0)
        {
            Debug.Log($"[GameActionTracker] Adding {pendingActions.Count} pending actions to final data");
            savedActions.AddRange(pendingActions);
            pendingActions.Clear();
        }

        Debug.Log($"[GameActionTracker] ✅ SESSION ENDING");
        Debug.Log($"[GameActionTracker]    Game ID: {currentGameId}");
        Debug.Log($"[GameActionTracker]    Gesture: {currentGestureName} (captured at session start)");
        Debug.Log($"[GameActionTracker]    Result: {result}");
        Debug.Log($"[GameActionTracker]    Score: {finalScore}");
        Debug.Log($"[GameActionTracker]    Total Actions: {savedActions.Count}");
        Debug.Log($"[GameActionTracker] ========================================");

        // Send data to API
        SendDataToAPI(result);

        // Clear session data for next session
        ClearSession();

        Debug.Log($"[GameActionTracker] Session cleared - ready for next game");
    }

    // ========================
    // PRIVATE METHODS
    // ========================

    /// <summary>
    /// Auto-save coroutine - runs every 3 seconds
    /// Moves data from pendingActions to savedActions (NO API call)
    /// </summary>
    private IEnumerator AutoSaveRoutine()
    {
        while (isTracking)
        {
            yield return new WaitForSeconds(3f);

            if (pendingActions.Count > 0)
            {
                // Move pending actions to permanent storage
                savedActions.AddRange(pendingActions);
                pendingActions.Clear();

                Debug.Log($"[GameActionTracker] Auto-saved | Total actions: {savedActions.Count}");
                // 🚫 NO API CALL - just internal data movement
            }
        }
    }

    /// <summary>
    /// Stop tracking and auto-save coroutine
    /// </summary>
    private void StopTracking()
    {
        isTracking = false;

        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }
    }

    /// <summary>
    /// Send accumulated data to API via GameOverAPI
    /// </summary>
    private void SendDataToAPI(string result)
    {
        // Build game_progress JSON using STORED gesture (captured at session start)
        GameProgressData progressData = new GameProgressData
        {
            gesture = currentGestureName,  // ✅ Use stored gesture from session start
            actions = savedActions
        };

        // Convert to JSON string
        string progressJson = JsonUtility.ToJson(progressData);

        Debug.Log($"[GameActionTracker] Sending to API:\n" +
                  $"game_id: {currentGameId}\n" +
                  $"game_result: {result}\n" +
                  $"game_score: {currentScore}\n" +
                  $"game_progress: {progressJson}");

        // Find GameOverAPI in scene (or create one)
        GameOverAPI api = FindFirstObjectByType<GameOverAPI>();
        if (api == null)
        {
            Debug.LogError("[GameActionTracker] GameOverAPI not found in scene! Cannot send data.");
            return;
        }

        // Send data via existing API
        api.SubmitAnswer(currentGameId, progressJson, currentScore, result);
    }

    /// <summary>
    /// Get human-readable gesture name from MasterGameManager
    /// Called ONLY at session start to capture the current gesture
    /// </summary>
    private string GetGestureNameFromMasterGameManager()
    {
        // ✅ IMPORTANT: Read gesture code from MasterGameManager at SESSION START
        // This ensures we capture the gesture that was selected for THIS game session
        string gestureCode = MasterGameManager.lastGestureKey;

        Debug.Log($"[GameActionTracker] *** CAPTURING GESTURE ***");
        Debug.Log($"[GameActionTracker] Reading MasterGameManager.lastGestureKey = '{gestureCode}'");

        if (string.IsNullOrEmpty(gestureCode))
        {
            Debug.LogError("[GameActionTracker] ❌ ERROR: No gesture code found in MasterGameManager.lastGestureKey!");
            Debug.LogError("[GameActionTracker] Make sure SaveGesture() was called before starting the game.");
            return "Unknown";
        }

        // Convert code to human-readable name
        string gestureName = gestureCode switch
        {
            "30" => "OpenPalm",
            "32" => "TwoFinger",
            "33" => "ThreeFinger",
            "34" => "WristRadialUlnar",
            "35" => "WristFlexionExtension",
            "36" => "ForearmPronationSupination",
            _ => $"Unknown (code: {gestureCode})"
        };

        Debug.Log($"[GameActionTracker] ✅ Gesture converted: '{gestureCode}' → '{gestureName}'");
        Debug.Log($"[GameActionTracker] This gesture will be used for the entire session until EndSession() is called.");

        return gestureName;
    }

    /// <summary>
    /// Clear all session data
    /// </summary>
    private void ClearSession()
    {
        currentGameId = "";
        currentGestureName = "";  // ✅ Clear stored gesture
        currentScore = 0;
        sessionStartTime = 0f;
        pendingActions.Clear();
        savedActions.Clear();
    }

    // ========================
    // APPLICATION QUIT HANDLER
    // ========================
    void OnApplicationQuit()
    {
        // If game is still tracking when app closes, send data as "exit"
        if (isTracking)
        {
            Debug.Log("[GameActionTracker] App closing - sending final data");
            EndSession(currentScore, "exit");
        }
    }
}
