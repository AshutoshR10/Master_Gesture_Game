# 📱 INSTRUCTIONS FOR ANDROID DEVELOPER

**Date:** 2025-11-26

---

## 🎯 HOW TO SEND TOKEN TO UNITY

### **Method: Unity SendMessage**

```java
UnityPlayer.UnitySendMessage(
    "MasterGameManager",     // GameObject name (EXACT - case sensitive)
    "Authorization",          // Method name (EXACT - case sensitive)
    yourJwtToken             // Your JWT token as string
);
```

---

## 📋 COMPLETE EXAMPLE CODE

### **Basic Example:**

```java
public class UnityTokenManager {

    /**
     * Send authentication token to Unity
     * @param token JWT Bearer token from your backend
     */
    public void sendTokenToUnity(String token) {
        // Validate token is not empty
        if (token == null || token.isEmpty()) {
            Log.e("UnityTokenManager", "Token is empty! Cannot send to Unity.");
            return;
        }

        // Send to Unity
        try {
            UnityPlayer.UnitySendMessage(
                "MasterGameManager",    // Target GameObject
                "Authorization",         // Target Method
                token                    // JWT Token
            );
            Log.i("UnityTokenManager", "✅ Token sent to Unity successfully");
        } catch (Exception e) {
            Log.e("UnityTokenManager", "❌ Failed to send token to Unity: " + e.getMessage());
        }
    }
}
```

---

## 🔄 WHEN TO SEND TOKEN

### **Scenario 1: App Start (User Already Logged In)**

```java
@Override
protected void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);

    // Check if user is logged in
    if (isUserLoggedIn()) {
        String token = getStoredToken();  // Get from SharedPreferences
        sendTokenToUnity(token);
    }
}
```

---

### **Scenario 2: After User Login**

```java
public void onLoginSuccess(String username, String password) {
    // Call your backend API to login
    authService.login(username, password, new Callback() {
        @Override
        public void onSuccess(LoginResponse response) {
            String jwtToken = response.getToken();

            // Save token locally
            saveToken(jwtToken);

            // Send to Unity
            sendTokenToUnity(jwtToken);

            Log.i("Login", "✅ User logged in and token sent to Unity");
        }

        @Override
        public void onFailure(String error) {
            Log.e("Login", "Login failed: " + error);
        }
    });
}
```

---

### **Scenario 3: Token Refresh**

```java
public void onTokenRefreshed(String newToken) {
    // Save new token
    saveToken(newToken);

    // Send to Unity
    sendTokenToUnity(newToken);

    Log.i("TokenRefresh", "✅ Token refreshed and sent to Unity");
}
```

---

### **Scenario 4: Before User Starts First Game**

```java
public void onGameStart() {
    // Get current token
    String currentToken = getStoredToken();

    if (currentToken != null && !currentToken.isEmpty()) {
        // Send fresh token to Unity before game starts
        sendTokenToUnity(currentToken);
    } else {
        Log.e("GameStart", "❌ No token available! User needs to login.");
        // Show login screen
        showLoginScreen();
    }
}
```

---

## 📝 TOKEN FORMAT REQUIREMENTS

### **What Unity Expects:**

| Requirement | Description | Example |
|-------------|-------------|---------|
| **Type** | String | `"eyJhbGciOiJIUzI1..."` |
| **Format** | JWT Token | Standard JWT format |
| **NOT Empty** | Must have value | Not `""` or `null` |
| **NOT Expired** | Valid token | Check expiry before sending |

### **Valid JWT Token Example:**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY5MWVjMGJiMjVkZGI3YjQ4NGE0NTJkZiIsImlhdCI6MTc2NDE0NjMxNywiZXhwIjoxNzY0MjMyNzE3fQ.bOdrPcr1GK8VMZV-ClUZbrQu9D4XERrEII4ndWBAX8g
```

**Structure:**
- Part 1: Header (algorithm, type)
- Part 2: Payload (user data, expiry)
- Part 3: Signature (verification)
- Separated by dots (.)

---

## 🔒 COMPLETE INTEGRATION EXAMPLE

```java
public class GameAuthManager {

    private static final String TAG = "GameAuthManager";
    private static final String PREF_TOKEN_KEY = "user_jwt_token";

    private SharedPreferences preferences;

    public GameAuthManager(Context context) {
        preferences = context.getSharedPreferences("GameAuth", Context.MODE_PRIVATE);
    }

    /**
     * Initialize Unity with stored token (call on app start)
     */
    public void initializeUnityAuth() {
        String storedToken = getSavedToken();

        if (storedToken != null && !storedToken.isEmpty()) {
            // Check if token is expired
            if (isTokenValid(storedToken)) {
                sendTokenToUnity(storedToken);
                Log.i(TAG, "✅ Unity initialized with stored token");
            } else {
                Log.w(TAG, "⚠️ Stored token is expired, need to refresh");
                refreshToken();
            }
        } else {
            Log.w(TAG, "⚠️ No token found, user needs to login");
        }
    }

    /**
     * Handle user login
     */
    public void handleLogin(String username, String password) {
        // Call your backend API
        yourBackendAPI.login(username, password, new ApiCallback() {
            @Override
            public void onSuccess(String jwtToken) {
                // Save token to SharedPreferences
                saveToken(jwtToken);

                // Send to Unity
                sendTokenToUnity(jwtToken);

                Log.i(TAG, "✅ Login successful, token sent to Unity");
            }

            @Override
            public void onError(String error) {
                Log.e(TAG, "❌ Login failed: " + error);
            }
        });
    }

    /**
     * Send token to Unity game
     */
    private void sendTokenToUnity(String token) {
        if (token == null || token.isEmpty()) {
            Log.e(TAG, "❌ Cannot send empty token to Unity");
            return;
        }

        try {
            UnityPlayer.UnitySendMessage(
                "MasterGameManager",
                "Authorization",
                token
            );
            Log.i(TAG, "✅ Token sent to Unity: " + token.substring(0, 50) + "...");
        } catch (Exception e) {
            Log.e(TAG, "❌ Failed to send token to Unity: " + e.getMessage());
        }
    }

    /**
     * Save token to SharedPreferences
     */
    private void saveToken(String token) {
        preferences.edit()
            .putString(PREF_TOKEN_KEY, token)
            .apply();
        Log.i(TAG, "Token saved to SharedPreferences");
    }

    /**
     * Get saved token from SharedPreferences
     */
    private String getSavedToken() {
        return preferences.getString(PREF_TOKEN_KEY, null);
    }

    /**
     * Check if token is still valid (not expired)
     */
    private boolean isTokenValid(String token) {
        try {
            // Decode JWT and check expiry
            // (You'll need a JWT library for this)
            String[] parts = token.split("\\.");
            if (parts.length != 3) return false;

            // Decode payload (Base64)
            String payload = new String(Base64.decode(parts[1], Base64.DEFAULT));
            JSONObject json = new JSONObject(payload);

            // Check expiry
            long exp = json.getLong("exp");
            long now = System.currentTimeMillis() / 1000;

            return exp > now;
        } catch (Exception e) {
            Log.e(TAG, "Error validating token: " + e.getMessage());
            return false;
        }
    }

    /**
     * Refresh expired token
     */
    private void refreshToken() {
        // Call your backend to refresh token
        yourBackendAPI.refreshToken(new ApiCallback() {
            @Override
            public void onSuccess(String newToken) {
                saveToken(newToken);
                sendTokenToUnity(newToken);
                Log.i(TAG, "✅ Token refreshed and sent to Unity");
            }

            @Override
            public void onError(String error) {
                Log.e(TAG, "❌ Token refresh failed: " + error);
            }
        });
    }

    /**
     * Handle logout
     */
    public void handleLogout() {
        // Clear token from SharedPreferences
        preferences.edit().remove(PREF_TOKEN_KEY).apply();

        // Optionally tell Unity to clear token (if needed)
        try {
            UnityPlayer.UnitySendMessage(
                "MasterGameManager",
                "ClearToken",
                ""
            );
        } catch (Exception e) {
            Log.e(TAG, "Error clearing Unity token: " + e.getMessage());
        }

        Log.i(TAG, "✅ User logged out");
    }
}
```

---

## 📞 USAGE IN YOUR ACTIVITY

```java
public class MainActivity extends UnityPlayerActivity {

    private GameAuthManager authManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Initialize auth manager
        authManager = new GameAuthManager(this);

        // Wait for Unity to load, then send token
        Handler handler = new Handler();
        handler.postDelayed(() -> {
            authManager.initializeUnityAuth();
        }, 2000); // Wait 2 seconds for Unity to initialize
    }

    // When user logs in
    public void onLoginButtonClicked(String username, String password) {
        authManager.handleLogin(username, password);
    }

    // When user logs out
    public void onLogoutButtonClicked() {
        authManager.handleLogout();
    }
}
```

---

## ✅ VERIFICATION (How to Check It's Working)

### **1. Check Android Logs:**
```
I/GameAuthManager: ✅ Token sent to Unity: eyJhbGciOiJIUzI1NiIsInR5c...
```

### **2. Check Unity Console:**
```
[MasterGameManager] ========================================
[MasterGameManager] ✅ AUTHORIZATION TOKEN SAVED
[MasterGameManager] Token (first 50 chars): eyJhbGciOiJIUzI1NiIsInR5c...
[MasterGameManager] Token Length: 182 characters
[MasterGameManager] ✅ Ready to make authenticated API calls
[MasterGameManager] ========================================
```

### **3. Check API Calls:**
```
[GameOverAPI] ✅ Using authenticated token (first 50 chars): eyJhbG...
Success={"success":true,"message":"Game progress saved successfully"...}
```

### **4. Check Backend:**
- API requests should return `200 OK`
- NOT `401 Unauthorized`
- Response: `{"success": true, "message": "Game progress saved successfully"}`

---

## 🚨 COMMON ISSUES & FIXES

### **Issue 1: Token Not Reaching Unity**

**Symptoms:**
- Unity shows: `⚠️ NO USER TOKEN FOUND`
- API uses fallback test token

**Fixes:**
```java
// Check 1: Is Unity loaded?
if (mUnityPlayer == null) {
    Log.e(TAG, "Unity not loaded yet!");
    // Wait and try again
}

// Check 2: Correct object and method names?
UnityPlayer.UnitySendMessage(
    "MasterGameManager",  // ✅ EXACT name
    "Authorization",       // ✅ EXACT name
    token
);

// Check 3: Token not empty?
if (token == null || token.isEmpty()) {
    Log.e(TAG, "Token is empty!");
}
```

---

### **Issue 2: 401 Unauthorized from Backend**

**Cause:** Token expired or invalid

**Fix:**
```java
// Check token expiry before sending
if (isTokenExpired(token)) {
    // Refresh token first
    refreshToken();
} else {
    sendTokenToUnity(token);
}
```

---

### **Issue 3: Token Sent But Not Saved**

**Check Unity Console:**
```
[MasterGameManager] ❌ ERROR: Attempted to set empty/null token!
```

**Fix:** Make sure token is NOT empty string
```java
if (token != null && !token.isEmpty() && token.length() > 10) {
    sendTokenToUnity(token);
}
```

---

## 📋 CHECKLIST FOR ANDROID DEVELOPER

- [ ] Import UnityPlayer in your Activity
- [ ] Create method to send token using `UnitySendMessage`
- [ ] Send token on app start (if user logged in)
- [ ] Send token after login success
- [ ] Send token when token refreshes
- [ ] Save token to SharedPreferences
- [ ] Check token expiry before sending
- [ ] Handle token refresh
- [ ] Add error handling and logging
- [ ] Test in Android logcat
- [ ] Verify Unity console shows "TOKEN SAVED"
- [ ] Verify API calls return success

---

## 🎯 MINIMUM REQUIRED CODE

**If you want the simplest possible implementation:**

```java
// 1. After user login:
String token = yourBackend.getJwtToken();

// 2. Send to Unity:
UnityPlayer.UnitySendMessage("MasterGameManager", "Authorization", token);

// 3. Done! Unity handles the rest automatically
```

---

## 📞 SUPPORT

**If Unity is not receiving the token:**

1. Check Android Logcat for errors
2. Check Unity Console for token received message
3. Verify GameObject name is exactly: `MasterGameManager`
4. Verify method name is exactly: `Authorization`
5. Verify token is a valid JWT string

**Expected Unity Console Output:**
```
[MasterGameManager] ✅ AUTHORIZATION TOKEN SAVED
```

---

**Last Updated:** 2025-11-26
**Status:** Ready for Android Integration
