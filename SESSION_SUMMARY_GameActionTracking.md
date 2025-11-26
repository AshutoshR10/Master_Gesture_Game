# 📝 SESSION SUMMARY: Game Action Tracking Implementation

**Date:** 2025-11-25
**Project:** Master Gesture Game - Rehabilitation Gaming Platform
**Focus:** Implementing action tracking and data submission system

---

## 🎯 **PROJECT OVERVIEW**

### **What is This Project?**
A Unity-based rehabilitation gaming platform with 4 mini-games designed for stroke/injury recovery therapy:
1. **Dino Game** (Chrome Dinosaur - Endless Runner)
2. **Space Invaders** (Arcade Shooter)
3. **Flappy Bird** (Casual Physics)
4. **Brick Breaker** (Ball & Paddle)

### **Key Features:**
- Gesture-based input (hand pose recognition)
- Motion capture integration
- 120-second level timer system
- Bilateral hand therapy (healthy/unhealthy hand alternation)
- Performance tracking and analytics

---

## 🔍 **ANALYSIS COMPLETED**

### **1. Project Architecture Understanding**

#### **Master Game Manager System** (`MasterGameManager.cs`)
- **Purpose:** Global orchestration across all 4 games with unified gesture UI system
- **Key Variable:** `public static string lastGestureKey` - Stores current gesture code

#### **Gesture System:**
| Code | UIType Enum | Human-Readable Name |
|------|-------------|---------------------|
| "30" | OpenPalm | "OpenPalm" |
| "32" | TwoFinger | "TwoFinger" |
| "33" | ThreeFinger | "ThreeFinger" |
| "34" | WristRadialUlnar | "WristRadialUlnar" |
| "35" | WristFlexionExtension | "WristFlexionExtension" |
| "36" | ForearmPronationSupination | "ForearmPronationSupination" |

#### **How Gesture UI Works:**
```
1. Android calls SaveGesture("DINO", "33")
   → lastGestureKey = "33"
   → LoadGameScene("DinoLvl1")

2. Scene loads
   → FindGestureUIsByTag() finds all UI panels
   → LoadGameGestures("33") activates ThreeFinger UI
   → All other gesture UIs disabled

3. Player sees ThreeFinger instruction panel
```

**Key Methods:**
- `FindGestureUIsByTag()` - Discovers gesture UI panels in scene using tags
- `LoadGameGestures(string keys)` - Activates corresponding gesture UI, saves to lastGestureKey
- `ApplyGestureUI(string keys)` - Converts gesture code to UIType and enables UI
- `SetUIActive(UIType activeUI)` - Shows one gesture UI, hides all others

**To Get Current Gesture:**
```csharp
string currentGesture = MasterGameManager.lastGestureKey; // Returns "33"
```

---

### **2. GameOverAPI Analysis** (`Assets/Scripts/GameOverAPI.cs`)

#### **Purpose:**
Sends game completion data to remote server for tracking and analytics.

#### **Current Implementation:**
```csharp
public void SubmitAnswer(string gameId, string gameProgress, int gameScore, string gameResult)
{
    StartCoroutine(SubmitGameProgress(gameId, gameProgress, gameScore, gameResult));
}
```

#### **API Endpoint:**
- **URL:** `http://145.223.23.182:5005/api/game/save`
- **Method:** POST
- **Authentication:** Bearer Token
- **Content-Type:** application/json

#### **JSON Payload Structure:**
```json
{
    "game_id": "dino_game",
    "game_progress": "{...JSON string with detailed progress...}",
    "game_result": "win",
    "game_score": 1500
}
```

#### **🚨 CRITICAL BUG FOUND:**
**Line 20:** `string token = "";` - Token is hardcoded as empty!

**Issue:** API call ALWAYS fails because token check fails immediately.

**Fix Required:**
```csharp
// Change line 20 from:
string token = "";

// To:
string token = MasterGameManager.userToken;
```

**How Token is Set:**
- MasterGameManager has `public static string userToken`
- Android calls `Authorization(string token)` method at game start
- Token should be retrieved from there

---

## 🎯 **REQUIREMENTS DEFINED**

### **User's Requirements:**

1. **Track game actions** (e.g., jump, move, fire) during gameplay
2. **Save action data every 3 seconds** (in memory, not to API)
3. **Send complete data to API ONLY on these triggers:**
   - ✅ Level Complete
   - ✅ Game Over
   - ✅ Player Retry
   - ✅ Player Exit

4. **Data to include:**
   - `game_id` - Game name ("DINO", "SPACE", "FLAPPY", "BRICK")
   - `gesture` - Human-readable gesture name ("ThreeFinger", not "33")
   - `game_score` - Final score
   - `game_result` - Outcome ("completed", "lose", "retry", "exit")
   - `actions` - Array of all actions performed with timestamps

5. **Backend token:** Will be provided later (use temporary for now)

---

## 🏗️ **IMPLEMENTATION ARCHITECTURE DECIDED**

### **Decision: ONE COMMON SCRIPT** ✅

**Why not separate scripts for each game?**
- ✅ Code reusability (write once, use everywhere)
- ✅ Easier maintenance (fix bug once → fixed in all games)
- ✅ Consistent data format
- ✅ Simple integration
- ✅ Less files to manage

**Architecture:**
```
GameActionTracker.cs (SINGLETON - One instance for entire game)
        ↓
   Used by all 4 games:
   - Dino.GameManager
   - Space.GameManager
   - Flappy.GameManager
   - BrickBreaker.GameManager
```

**Same methods, different action names:**
```csharp
// DINO Game
GameActionTracker.Instance.RecordAction("jump", playerPos);

// SPACE Game
GameActionTracker.Instance.RecordAction("move_left", playerPos);
GameActionTracker.Instance.RecordAction("fire", playerPos);

// FLAPPY Game
GameActionTracker.Instance.RecordAction("flap", playerPos);

// BRICK Game
GameActionTracker.Instance.RecordAction("move_paddle", playerPos);
```

---

## 🔄 **DATA FLOW EXPLAINED**

### **Complete Timeline:**

```
Time    Event                           Storage                  API Call
─────────────────────────────────────────────────────────────────────────
0s      StartSession("DINO")            actions: []              🚫 No
        - gameId = "DINO"
        - gesture = "ThreeFinger"
        - Start 3s timer

1s      RecordAction("jump")            pendingActions: [1]      🚫 No
2s      RecordAction("jump")            pendingActions: [2]      🚫 No

3s      ⏰ AUTO-SAVE TRIGGERED         actions: [2]             🚫 No
        - Move pendingActions →
          currentSession.actions
        - Clear pendingActions
        - Restart timer
        - NO API CALL

4s      RecordAction("jump")            pendingActions: [1]      🚫 No
5s      RecordAction("jump")            pendingActions: [2]      🚫 No

6s      ⏰ AUTO-SAVE TRIGGERED         actions: [4]             🚫 No

9s      ⏰ AUTO-SAVE                    actions: [7]             🚫 No
12s     ⏰ AUTO-SAVE                    actions: [11]            🚫 No
...     (continues every 3s)            actions: [growing]       🚫 No

120s    🎯 LEVEL COMPLETE              actions: [45]            ✅ YES! 🚀
        EndSession(1500, "completed")
        - Stop auto-save timer
        - Add remaining pending actions
        - Fill final data:
          * gameScore = 1500
          * gameResult = "completed"
          * sessionDuration = 120.5s
        - Convert gesture code to name
        - Build complete JSON
        - SEND TO API
```

### **Key Points:**
- ⏰ Auto-save runs every 3 seconds (moves data internally)
- 💾 All data stored in RAM memory (not sent)
- 🚫 API called **ZERO times** during gameplay
- ✅ API called **ONE time** when trigger occurs
- 📦 All accumulated actions sent in single API call

---

## 📦 **DATA STRUCTURE DESIGN**

### **ActionData Class:**
```csharp
public class ActionData
{
    public string action;           // "jump", "move_left", "fire", etc.
    public float timestamp;         // Time when action occurred
    public Vector3 playerPosition;  // Player position at that moment
    public string additionalData;   // Optional extra info
}
```

### **GameSessionData Class:**
```csharp
public class GameSessionData
{
    public string gameId;                    // "DINO", "SPACE", "FLAPPY", "BRICK"
    public string gesture;                   // "ThreeFinger" (human-readable)
    public int gameScore;                    // Final score
    public string gameResult;                // "completed", "lose", "retry", "exit"
    public float sessionDuration;            // Total time played
    public List<ActionData> actions;         // All recorded actions
    public string patientId;                 // Patient identifier
    public int level;                        // Current level (1, 2, 3)
    public DateTime sessionStartTime;        // When session started
    public DateTime sessionEndTime;          // When session ended
}
```

### **Final JSON Example Sent to API:**
```json
{
    "game_id": "DINO",
    "gesture": "ThreeFinger",
    "game_score": 1500,
    "game_result": "completed",
    "session_duration": 120.50,
    "patient_id": "PAT001",
    "level": 1,
    "session_start": "2025-11-25 10:30:00",
    "session_end": "2025-11-25 10:32:00",
    "total_actions": 45,
    "actions": [
        {
            "action": "jump",
            "timestamp": "0.50",
            "position_x": "0.00",
            "position_y": "1.50",
            "position_z": "0.00"
        },
        {
            "action": "jump",
            "timestamp": "1.20",
            "position_x": "0.00",
            "position_y": "1.50",
            "position_z": "0.00"
        },
        // ... (43 more actions)
    ]
}
```

---

## 🛠️ **IMPLEMENTATION PLAN**

### **Files to Create:**

#### **1. GameActionTracker.cs** (NEW)
**Location:** `Assets/Scripts/GameActionTracker.cs`

**Key Methods:**
```csharp
// Start tracking when game begins
public void StartSession(string gameId, int level, string patientId = "Unknown")

// Record player action
public void RecordAction(string actionName, Vector3 playerPosition, string extraData = "")

// End session and send to API
public void EndSession(int finalScore, string result)

// Auto-save coroutine (runs every 3 seconds)
private IEnumerator AutoSaveRoutine()

// Send to server via GameOverAPI
private void SendDataToServer()

// Helper: Get gesture name from code
private string GetGestureName(string gestureCode)
```

**Features:**
- Singleton pattern (`Instance`)
- Auto-save every 3 seconds
- Two-stage storage (pendingActions → session.actions)
- Gesture code to name conversion
- OnApplicationQuit handler (sends data on exit)

---

### **Files to Modify:**

#### **2. GameOverAPI.cs** (MODIFY)
**Location:** `Assets/Scripts/GameOverAPI.cs`

**Changes Required:**
```csharp
// LINE 20 - FIX TOKEN RETRIEVAL
// BEFORE:
string token = "";

// AFTER:
string token = MasterGameManager.userToken;

// ADD: Validation check
if (string.IsNullOrEmpty(token))
{
    Debug.LogWarning("Token not set. Using empty token for testing.");
    // For testing, allow empty token temporarily
}
```

---

#### **3. Dino.GameManager.cs** (MODIFY)
**Location:** `Assets/My_Assets_Dino/Dino_Scripts/GameManager.cs`

**Changes to Add:**

**A. In Start() or NewGame():**
```csharp
void Start()
{
    // ... existing code ...

    // START TRACKING
    GameActionTracker.Instance.StartSession(
        "DINO",
        SceneManager.GetActiveScene().buildIndex + 1,  // Level number
        patientID  // Already exists in your code
    );
}
```

**B. In Player Jump Logic:**
```csharp
// In Player.cs or wherever jump is handled
void Jump()
{
    // Existing jump code...

    // RECORD ACTION
    GameActionTracker.Instance.RecordAction(
        "jump",
        transform.position
    );
}
```

**C. In GameOver():**
```csharp
void GameOver()
{
    // ... existing code ...

    // END SESSION AND SEND DATA
    GameActionTracker.Instance.EndSession(
        (int)score,
        "lose"
    );
}
```

**D. In LevelComplete():**
```csharp
void LevelComplete()
{
    // ... existing code ...

    // END SESSION AND SEND DATA
    GameActionTracker.Instance.EndSession(
        (int)score,
        "completed"
    );
}
```

**E. In PlayAgain() (Retry):**
```csharp
void PlayAgain()
{
    // END CURRENT SESSION
    GameActionTracker.Instance.EndSession(
        (int)score,
        "retry"
    );

    // ... existing reload scene code ...
}
```

**F. In OnApplicationQuit():**
```csharp
void OnApplicationQuit()
{
    // Existing code...

    // SEND DATA ON EXIT
    if (GameActionTracker.Instance != null)
    {
        GameActionTracker.Instance.EndSession(
            (int)score,
            "exit"
        );
    }
}
```

---

#### **4. Space.GameManager.cs** (MODIFY)
**Location:** `Assets/Scripts/GameManager.cs`

**Similar changes as Dino, but record different actions:**

```csharp
// Record movement
void Update()
{
    if (movingLeft)
    {
        GameActionTracker.Instance.RecordAction(
            "move_left",
            player.transform.position
        );
    }

    if (movingRight)
    {
        GameActionTracker.Instance.RecordAction(
            "move_right",
            player.transform.position
        );
    }
}

// Record firing
void Fire()
{
    GameActionTracker.Instance.RecordAction(
        "fire",
        player.transform.position,
        "laser_shot"  // Extra data
    );
}
```

---

#### **5. Flappy.GameManager.cs** (MODIFY)
**Location:** `Assets/FlappyBird/Flappy_Scripts/GameManager.cs`

**Record flap action:**
```csharp
void Flap()
{
    GameActionTracker.Instance.RecordAction(
        "flap",
        player.transform.position,
        $"height:{player.transform.position.y}"
    );
}

void OnPipeCollision()
{
    GameActionTracker.Instance.RecordAction(
        "collision",
        player.transform.position,
        "hit_pipe"
    );
}
```

---

#### **6. BrickBreaker.GameManager.cs** (MODIFY)
**Location:** `Assets/Brick_Breaker_Game/Scripts/GameManager.cs`

**Record paddle movement and brick destruction:**
```csharp
void Update()
{
    if (paddle.velocity.magnitude > 0)
    {
        GameActionTracker.Instance.RecordAction(
            "move_paddle",
            paddle.transform.position,
            $"velocity:{paddle.velocity.x:F2}"
        );
    }
}

void OnBrickDestroyed(Brick brick)
{
    GameActionTracker.Instance.RecordAction(
        "brick_destroyed",
        ball.transform.position,
        $"brick_points:{brick.points}"
    );
}
```

---

## 📋 **IMPLEMENTATION CHECKLIST**

### **Phase 1: Create New Script**
- [ ] Create `GameActionTracker.cs` in `Assets/Scripts/`
- [ ] Add to a GameObject in each game scene (or use DontDestroyOnLoad)
- [ ] Test singleton pattern works

### **Phase 2: Fix GameOverAPI**
- [ ] Modify line 20 to retrieve token from MasterGameManager
- [ ] Add token validation (allow empty for testing)
- [ ] Test API connection with backend team

### **Phase 3: Integrate with Dino Game**
- [ ] Add StartSession() call in NewGame()
- [ ] Add RecordAction() in jump logic
- [ ] Add EndSession() in GameOver()
- [ ] Add EndSession() in LevelComplete()
- [ ] Add EndSession() in PlayAgain()
- [ ] Add EndSession() in OnApplicationQuit()
- [ ] Test complete flow

### **Phase 4: Integrate with Space Invaders**
- [ ] Add StartSession()
- [ ] Record move_left, move_right, fire actions
- [ ] Add all EndSession() calls
- [ ] Test

### **Phase 5: Integrate with Flappy Bird**
- [ ] Add StartSession()
- [ ] Record flap, collision actions
- [ ] Add all EndSession() calls
- [ ] Test

### **Phase 6: Integrate with Brick Breaker**
- [ ] Add StartSession()
- [ ] Record move_paddle, brick_destroyed actions
- [ ] Add all EndSession() calls
- [ ] Test

### **Phase 7: Testing**
- [ ] Test auto-save every 3 seconds (check Debug.Log)
- [ ] Test data NOT sent during gameplay
- [ ] Test data sent on Level Complete
- [ ] Test data sent on Game Over
- [ ] Test data sent on Retry
- [ ] Test data sent on Exit
- [ ] Verify JSON structure matches requirements
- [ ] Verify gesture name conversion works
- [ ] Test with backend API (when available)

---

## 🎯 **EXPECTED BEHAVIOR**

### **During Gameplay (0-120 seconds):**
1. Player performs actions (jump, move, fire, etc.)
2. Each action recorded to `pendingActions` list
3. Every 3 seconds, auto-save moves data to permanent storage
4. **NO API CALLS** - data only stored in RAM
5. Continue until game ends

### **On Trigger Event:**
1. Stop auto-save timer
2. Add remaining pending actions
3. Fill in final data (score, result, duration)
4. Convert gesture code to readable name
5. Build complete JSON with ALL actions
6. **SEND TO API** via GameOverAPI
7. Clear session data

### **API Receives:**
- Complete game session data
- All actions from start to end
- Timestamps for each action
- Player positions
- Final score and result
- Gesture used
- Session duration

---

## 🚀 **NEXT STEPS**

### **When User Returns:**

1. **Review this document** to recall where we left off

2. **Get approval** to proceed with implementation:
   - Confirm architecture (one common script)
   - Confirm data structure
   - Confirm trigger events
   - Confirm API integration approach

3. **Execute implementation:**
   - Create GameActionTracker.cs
   - Fix GameOverAPI.cs token issue
   - Modify all 4 GameManager scripts
   - Add action recording calls

4. **Test thoroughly:**
   - Verify auto-save works
   - Verify API only called on triggers
   - Verify JSON structure
   - Verify gesture name conversion

5. **Coordinate with backend team:**
   - Provide JSON structure documentation
   - Get production API endpoint
   - Get authentication token setup
   - Test data submission

---

## 📝 **IMPORTANT NOTES**

### **Current Status:**
- ✅ Analysis complete
- ✅ Requirements defined
- ✅ Architecture decided
- ✅ Data structure designed
- ✅ Implementation plan created
- ⏸️ **PAUSED - Awaiting approval to proceed with implementation**

### **Questions Answered:**
1. ✅ How gesture UI system works
2. ✅ How to get current gesture data
3. ✅ What GameOverAPI does
4. ✅ How to track actions every 3 seconds without API calls
5. ✅ How to send data only on triggers
6. ✅ One script vs separate scripts decision

### **Files Ready to Create/Modify:**
1. **Create:** `Assets/Scripts/GameActionTracker.cs`
2. **Modify:** `Assets/Scripts/GameOverAPI.cs`
3. **Modify:** `Assets/My_Assets_Dino/Dino_Scripts/GameManager.cs`
4. **Modify:** `Assets/Scripts/GameManager.cs` (Space Invaders)
5. **Modify:** `Assets/FlappyBird/Flappy_Scripts/GameManager.cs`
6. **Modify:** `Assets/Brick_Breaker_Game/Scripts/GameManager.cs`

---

## 🔗 **KEY REFERENCES**

### **Existing Code Locations:**

**MasterGameManager:**
- File: `Assets/MasterGameManager.cs`
- Key: `lastGestureKey` (line 23)
- Key: `userToken` (line 91)

**GameOverAPI:**
- File: `Assets/Scripts/GameOverAPI.cs`
- Bug: Line 20 (empty token)
- Method: `SubmitAnswer()` (line 13)

**Dino GameManager:**
- File: `Assets/My_Assets_Dino/Dino_Scripts/GameManager.cs`
- GameOver: Line 387
- LevelComplete: Line 458
- PlayAgain: Line 503
- OnApplicationQuit: Line 192

**Space GameManager:**
- File: `Assets/Scripts/GameManager.cs`
- Namespace: `Space {}`

**Flappy GameManager:**
- File: `Assets/FlappyBird/Flappy_Scripts/GameManager.cs`
- Namespace: `Flappy {}`

**Brick GameManager:**
- File: `Assets/Brick_Breaker_Game/Scripts/GameManager.cs`
- Namespace: `BrickBreaker {}`

---

## 💡 **RECOMMENDATIONS**

### **Before Implementation:**
1. **Backup project** (create git commit or copy folder)
2. **Test in one game first** (recommend starting with Dino)
3. **Use debug logs extensively** to verify flow
4. **Test with empty API endpoint** first (no backend needed)

### **During Implementation:**
1. **Implement incrementally** (one game at a time)
2. **Test after each change** (don't modify all files at once)
3. **Keep detailed logs** of what works/doesn't work
4. **Document any issues** encountered

### **After Implementation:**
1. **Provide JSON structure to backend team**
2. **Coordinate on authentication token flow**
3. **Test with real API endpoint**
4. **Monitor for any performance issues**
5. **Consider offline data storage** for future enhancement

---

## 📊 **TECHNICAL SPECIFICATIONS**

### **Performance Considerations:**
- Auto-save every 3 seconds is lightweight (just array copy)
- Actions stored in RAM (no file I/O during gameplay)
- Single API call per game session (minimal network usage)
- JSON serialization happens only once (at EndSession)

### **Memory Usage:**
- Each ActionData: ~60 bytes
- 45 actions per 120s game: ~2.7 KB
- Negligible impact on mobile devices

### **Network Usage:**
- One API call per game session
- Typical payload: 5-15 KB
- Sent on WiFi or mobile data
- Retry logic needed for offline scenarios (future)

---

## 🎯 **SUCCESS CRITERIA**

### **Implementation Complete When:**
- ✅ GameActionTracker.cs created and functional
- ✅ All 4 games integrated
- ✅ Actions recorded for each game type
- ✅ Auto-save runs every 3 seconds
- ✅ API called ONLY on triggers
- ✅ Data structure matches requirements
- ✅ Gesture names converted properly
- ✅ Backend successfully receives data
- ✅ No performance issues observed

### **Testing Passed When:**
- ✅ Play complete 120s game → verify API called once
- ✅ Die during game → verify API called with "lose"
- ✅ Retry game → verify API called with "retry"
- ✅ Exit game → verify API called with "exit"
- ✅ Verify action count matches gameplay
- ✅ Verify timestamps are sequential
- ✅ Verify gesture name is readable
- ✅ Backend confirms data received correctly

---

## 📞 **CONTACT POINTS**

### **Team Coordination Needed:**
1. **Backend Developer:**
   - Confirm API endpoint URL
   - Provide authentication token
   - Validate JSON structure
   - Test data reception

2. **Android Developer:**
   - Confirm token passing mechanism
   - Test gesture code sending
   - Verify integration flow

3. **QA/Testing:**
   - Test all 4 games
   - Test all trigger scenarios
   - Verify data accuracy
   - Check edge cases

---

## 🔐 **SECURITY NOTES**

### **Current Issues:**
- ⚠️ HTTP instead of HTTPS (line 19 in GameOverAPI)
- ⚠️ Empty token allows bypass (line 20-25 in GameOverAPI)

### **Recommendations:**
- Use HTTPS for production
- Validate token properly
- Implement token refresh mechanism
- Add request signing for data integrity

---

## 📅 **ESTIMATED TIMELINE**

### **Implementation:**
- GameActionTracker.cs creation: 1 hour
- GameOverAPI fix: 15 minutes
- Dino integration: 1 hour
- Space integration: 1 hour
- Flappy integration: 1 hour
- Brick integration: 1 hour
- **Total: ~5-6 hours**

### **Testing:**
- Unit testing: 2 hours
- Integration testing: 2 hours
- Backend coordination: 1 hour
- **Total: ~5 hours**

### **Overall: 10-12 hours of development work**

---

## ✅ **APPROVAL CHECKPOINT**

**Before proceeding, confirm:**
- [ ] Architecture approach approved (one common script)
- [ ] Data structure approved
- [ ] Trigger events confirmed (complete/over/retry/exit)
- [ ] API integration approach confirmed
- [ ] Ready to begin implementation

**User stated:** "Save all this discussion and changes in session so next time we read all this data then continue where we left"

**Status:** ✅ **SAVED - Ready to resume implementation when user returns**

---

**END OF SESSION SUMMARY**

**Last Updated:** 2025-11-25
**Created By:** Claude (Anthropic AI Assistant)
**Project:** Master Gesture Game - Game Action Tracking Implementation
