# 🧪 GESTURE TRACKING - COMPLETE TEST GUIDE

## 📋 **HOW THE SYSTEM WORKS:**

### **Key Principle:**
- ✅ Gesture is **captured ONCE** when `StartSession()` is called
- ✅ Gesture is **stored** in `currentGestureName` for the entire session
- ✅ Gesture is **sent with data** when `EndSession()` is called
- ✅ Gesture is **cleared** after data is sent
- ✅ **Each new session** captures the gesture fresh from `MasterGameManager.lastGestureKey`

---

## 🧪 **TEST SCENARIOS:**

### **Test 1: DINO with TwoFinger**

**Steps:**
1. Select DINO game with gesture "32" (TwoFinger)
2. Play the game
3. Jump a few times
4. Die (hit obstacle)

**Expected Console Output:**
```
[GameActionTracker] ========================================
[GameActionTracker] StartSession() called for: DINO
[GameActionTracker] ========================================
[GameActionTracker] *** CAPTURING GESTURE ***
[GameActionTracker] Reading MasterGameManager.lastGestureKey = '32'
[GameActionTracker] ✅ Gesture converted: '32' → 'TwoFinger'
[GameActionTracker] This gesture will be used for the entire session until EndSession() is called.
[GameActionTracker] ✅ SESSION STARTED
[GameActionTracker]    Game ID: DINO
[GameActionTracker]    Gesture: TwoFinger (LOCKED for this session)
[GameActionTracker]    Tracking: ACTIVE
[GameActionTracker]    Auto-save: Every 3 seconds
[GameActionTracker] ========================================

... (jumping and auto-saving) ...

[GameActionTracker] ========================================
[GameActionTracker] EndSession() called - Result: lose
[GameActionTracker] ========================================
[GameActionTracker] ✅ SESSION ENDING
[GameActionTracker]    Game ID: DINO
[GameActionTracker]    Gesture: TwoFinger (captured at session start)
[GameActionTracker]    Result: lose
[GameActionTracker]    Score: 150
[GameActionTracker]    Total Actions: 5
[GameActionTracker] ========================================
[GameActionTracker] Sending to API:
game_id: DINO
game_result: lose
game_score: 150
game_progress: {"gesture":"TwoFinger","actions":[...]}
```

**Expected Backend Data:**
```json
{
  "game_id": "DINO",
  "game_progress": {
    "gesture": "TwoFinger",  ← MUST BE "TwoFinger"
    "actions": [...]
  },
  "game_result": "lose",
  "game_score": 150
}
```

✅ **PASS if gesture = "TwoFinger"**

---

### **Test 2: DINO AGAIN with ThreeFinger (Different Gesture)**

**Steps:**
1. Go back to menu (or Android sends new gesture)
2. Select DINO game with gesture "33" (ThreeFinger)
3. Play the game
4. Jump a few times
5. Complete level (120 seconds)

**Expected Console Output:**
```
[GameActionTracker] ========================================
[GameActionTracker] StartSession() called for: DINO
[GameActionTracker] ========================================
[GameActionTracker] *** CAPTURING GESTURE ***
[GameActionTracker] Reading MasterGameManager.lastGestureKey = '33'  ← NEW!
[GameActionTracker] ✅ Gesture converted: '33' → 'ThreeFinger'  ← NEW!
[GameActionTracker] This gesture will be used for the entire session until EndSession() is called.
[GameActionTracker] ✅ SESSION STARTED
[GameActionTracker]    Game ID: DINO
[GameActionTracker]    Gesture: ThreeFinger (LOCKED for this session)  ← NEW!
[GameActionTracker]    Tracking: ACTIVE
[GameActionTracker]    Auto-save: Every 3 seconds
[GameActionTracker] ========================================

... (playing) ...

[GameActionTracker] ========================================
[GameActionTracker] EndSession() called - Result: completed
[GameActionTracker] ========================================
[GameActionTracker] ✅ SESSION ENDING
[GameActionTracker]    Game ID: DINO
[GameActionTracker]    Gesture: ThreeFinger (captured at session start)  ← NEW!
[GameActionTracker]    Result: completed
[GameActionTracker]    Score: 1500
[GameActionTracker]    Total Actions: 45
[GameActionTracker] ========================================
```

**Expected Backend Data:**
```json
{
  "game_id": "DINO",
  "game_progress": {
    "gesture": "ThreeFinger",  ← MUST BE "ThreeFinger" (NOT "TwoFinger")
    "actions": [...]
  },
  "game_result": "completed",
  "game_score": 1500
}
```

✅ **PASS if gesture = "ThreeFinger"** (proves gesture changes correctly)

---

### **Test 3: SPACE with OpenPalm**

**Steps:**
1. Go back to menu
2. Select SPACE game with gesture "30" (OpenPalm)
3. Play the game
4. Move left and right
5. Die

**Expected Console Output:**
```
[GameActionTracker] ========================================
[GameActionTracker] StartSession() called for: SPACE  ← Different game
[GameActionTracker] ========================================
[GameActionTracker] *** CAPTURING GESTURE ***
[GameActionTracker] Reading MasterGameManager.lastGestureKey = '30'  ← New gesture
[GameActionTracker] ✅ Gesture converted: '30' → 'OpenPalm'  ← New gesture
[GameActionTracker] ✅ SESSION STARTED
[GameActionTracker]    Game ID: SPACE  ← Different game
[GameActionTracker]    Gesture: OpenPalm (LOCKED for this session)  ← New gesture
[GameActionTracker] ========================================

[GameActionTracker] ========================================
[GameActionTracker] EndSession() called - Result: lose
[GameActionTracker] ========================================
[GameActionTracker] ✅ SESSION ENDING
[GameActionTracker]    Game ID: SPACE  ← Correct game ID
[GameActionTracker]    Gesture: OpenPalm (captured at session start)  ← Correct gesture
[GameActionTracker]    Result: lose
```

**Expected Backend Data:**
```json
{
  "game_id": "SPACE",  ← MUST BE "SPACE"
  "game_progress": {
    "gesture": "OpenPalm",  ← MUST BE "OpenPalm"
    "actions": [{"action":"move_left",...}, {"action":"move_right",...}]
  },
  "game_result": "lose",
  "game_score": 200
}
```

✅ **PASS if game_id = "SPACE" AND gesture = "OpenPalm"**

---

### **Test 4: Retry with SAME Gesture**

**Steps:**
1. Play DINO with "TwoFinger"
2. Die
3. Click "Retry" (WITHOUT going back to menu)
4. Play again

**Expected Console Output:**
```
First session:
[GameActionTracker]    Gesture: TwoFinger (LOCKED for this session)
... (game ends) ...
[GameActionTracker] EndSession() called - Result: lose
[GameActionTracker]    Gesture: TwoFinger (captured at session start)

Scene reloads...

Second session:
[GameActionTracker] StartSession() called for: DINO
[GameActionTracker] Reading MasterGameManager.lastGestureKey = '32'  ← Still "32"!
[GameActionTracker] ✅ Gesture converted: '32' → 'TwoFinger'  ← Still "TwoFinger"!
[GameActionTracker]    Gesture: TwoFinger (LOCKED for this session)  ← Same gesture
```

**Expected Backend Data for BOTH sessions:**
```json
First session: {"game_id": "DINO", "game_progress": {"gesture": "TwoFinger", ...}}
Second session: {"game_id": "DINO", "game_progress": {"gesture": "TwoFinger", ...}}
```

✅ **PASS if both sessions have gesture = "TwoFinger"**

---

### **Test 5: All 4 Games with Different Gestures**

**Steps:**
1. Play DINO with "ThreeFinger" → Check backend
2. Play SPACE with "TwoFinger" → Check backend
3. Play FLAPPY with "OpenPalm" → Check backend
4. Play BRICK with "WristRadialUlnar" → Check backend

**Expected Backend Data:**
```json
[
  {"game_id": "DINO", "game_progress": {"gesture": "ThreeFinger", ...}},
  {"game_id": "SPACE", "game_progress": {"gesture": "TwoFinger", ...}},
  {"game_id": "FLAPPY", "game_progress": {"gesture": "OpenPalm", ...}},
  {"game_id": "BRICK", "game_progress": {"gesture": "WristRadialUlnar", ...}}
]
```

✅ **PASS if all 4 entries have correct game_id AND gesture**

---

## 🔍 **HOW TO VERIFY:**

### **In Unity Console:**
Look for these key logs:

1. **Session Start:**
   ```
   [GameActionTracker] Reading MasterGameManager.lastGestureKey = 'XX'
   [GameActionTracker] ✅ Gesture converted: 'XX' → 'GestureName'
   [GameActionTracker]    Gesture: GestureName (LOCKED for this session)
   ```

2. **Session End:**
   ```
   [GameActionTracker]    Gesture: GestureName (captured at session start)
   [GameActionTracker] Sending to API:
   game_progress: {"gesture":"GestureName","actions":[...]}
   ```

### **In Backend:**
Check the JSON data received:
```json
{
  "game_progress": {
    "gesture": "VERIFY THIS MATCHES WHAT YOU SELECTED",
    "actions": [...]
  }
}
```

---

## ✅ **EXPECTED BEHAVIOR:**

| Scenario | Expected Result |
|----------|----------------|
| Play DINO with TwoFinger | Backend gets gesture: "TwoFinger" ✅ |
| Play DINO AGAIN with ThreeFinger | Backend gets gesture: "ThreeFinger" ✅ |
| Play SPACE with OpenPalm | Backend gets gesture: "OpenPalm" ✅ |
| Retry same game | Backend gets SAME gesture ✅ |
| Different games | Backend gets DIFFERENT game_id ✅ |

---

## 🚨 **IF SOMETHING IS WRONG:**

### **Problem: Gesture is always "Unknown"**
**Cause:** `MasterGameManager.lastGestureKey` is empty
**Check:** Make sure `SaveGesture()` is called before loading the scene

### **Problem: Gesture is from previous game**
**Cause:** Gesture not being captured at StartSession
**Check:** Verify `StartSession()` is being called when game starts

### **Problem: Wrong game_id in data**
**Cause:** Wrong parameter passed to StartSession
**Check:** Each game should call `StartSession("CORRECT_GAME_NAME")`

---

## 📊 **GESTURE CODE MAPPING:**

| Code | Gesture Name |
|------|--------------|
| "30" | OpenPalm |
| "32" | TwoFinger |
| "33" | ThreeFinger |
| "34" | WristRadialUlnar |
| "35" | WristFlexionExtension |
| "36" | ForearmPronationSupination |

---

**Test with these exact scenarios and verify the console logs match!**
