# 🔐 TOKEN MANAGEMENT - QUICK REFERENCE

## 📞 **FOR ANDROID DEVELOPERS**

### **How to Send Token to Unity:**

```java
// When user logs in or token refreshes:
UnityPlayer.UnitySendMessage(
    "MasterGameManager",     // GameObject name
    "Authorization",          // Method name
    yourJwtToken             // Your JWT token string
);
```

### **When to Send:**
1. ✅ On app start (if user already logged in)
2. ✅ After login success
3. ✅ When token refreshes
4. ✅ Before user starts first game

---

## 🎮 **FOR UNITY DEVELOPERS**

### **Setting Token Manually (Testing):**

```csharp
// Get MasterGameManager instance
MasterGameManager manager = FindObjectOfType<MasterGameManager>();

// Set token
manager.Authorization("YOUR_JWT_TOKEN_HERE");
```

### **Checking if Token is Set:**

```csharp
if (MasterGameManager.HasValidToken())
{
    Debug.Log("Token is ready!");
}
```

### **Getting Current Token:**

```csharp
string currentToken = MasterGameManager.GetToken();
```

### **Clearing Token (Logout):**

```csharp
MasterGameManager manager = FindObjectOfType<MasterGameManager>();
manager.ClearToken();
```

---

## 📊 **CONSOLE LOGS TO EXPECT**

### **When Token is First Set:**
```
[MasterGameManager] ========================================
[MasterGameManager] ✅ AUTHORIZATION TOKEN SAVED
[MasterGameManager] Token (first 50 chars): eyJhbGciOiJIUzI1NiI...
[MasterGameManager] Token Length: 182 characters
[MasterGameManager] Token saved to memory and PlayerPrefs
[MasterGameManager] ✅ Ready to make authenticated API calls
[MasterGameManager] ========================================
```

### **When Token Changes:**
```
[MasterGameManager] ========================================
[MasterGameManager] 🔄 TOKEN CHANGE DETECTED
[MasterGameManager] Old Token (first 50 chars): eyJhbG...
[MasterGameManager] New Token (first 50 chars): eyJhbH...
[MasterGameManager] ✅ TOKEN UPDATED SUCCESSFULLY
[MasterGameManager] ========================================
```

### **When App Restarts:**
```
[MasterGameManager] ✅ Token restored from previous session
```

### **When API is Called:**
```
[GameOverAPI] ✅ Using authenticated token (first 50 chars): eyJhbG...
```

---

## 🚨 **ERROR MESSAGES**

### **Empty Token Error:**
```
[MasterGameManager] ❌ ERROR: Attempted to set empty/null token!
[MasterGameManager] Token was NOT updated. Please provide a valid token.
```
**Fix:** Make sure Android is sending a non-empty token string.

### **No Token Warning:**
```
[GameOverAPI] ⚠️ NO USER TOKEN FOUND
[GameOverAPI] Using fallback test token for development
```
**Fix:** Call `Authorization(token)` before making API calls.

---

## ✅ **VERIFICATION**

### **Check Token is Working:**

1. **Console shows token saved:**
   ```
   ✅ AUTHORIZATION TOKEN SAVED
   ```

2. **API calls show token being used:**
   ```
   ✅ Using authenticated token
   ```

3. **Backend returns success:**
   ```
   Success={"success":true,"message":"Game progress saved successfully"...}
   ```

4. **NO 401 Unauthorized errors**

---

## 🔄 **TOKEN LIFECYCLE**

```
1. Android sends token → MasterGameManager.Authorization(token)
2. Unity saves to memory + PlayerPrefs
3. User plays games
4. Game ends → GameOverAPI.SubmitGameProgress()
5. API gets token → MasterGameManager.GetToken()
6. API sends request with Bearer token
7. Backend authenticates and saves data
```

---

## 📱 **ANDROID EXAMPLE CODE**

```java
public class UnityBridge {

    // Send token to Unity
    public void sendTokenToUnity(String jwtToken) {
        UnityPlayer.UnitySendMessage(
            "MasterGameManager",
            "Authorization",
            jwtToken
        );
    }

    // Call after user login
    public void onUserLogin(String token) {
        sendTokenToUnity(token);
    }

    // Call when token refreshes
    public void onTokenRefresh(String newToken) {
        sendTokenToUnity(newToken);
    }
}
```

---

## 🎯 **QUICK TROUBLESHOOTING**

| Problem | Solution |
|---------|----------|
| 401 Unauthorized | Token expired → Send new token from Android |
| Token not saved | Check console for "✅ AUTHORIZATION TOKEN SAVED" |
| Token not restored | Token should restore automatically on restart |
| API using test token | Android hasn't sent token yet → Call Authorization() |

---

**💡 KEY POINTS:**

1. ✅ Token is **automatically** used by all API calls
2. ✅ Token **persists** across app restarts
3. ✅ Token **changes** are automatically detected
4. ✅ No manual token management needed after Authorization() is called

---

**📚 Full Documentation:** See `TOKEN_AUTHORIZATION_GUIDE.md`

---

**Last Updated:** 2025-11-26
