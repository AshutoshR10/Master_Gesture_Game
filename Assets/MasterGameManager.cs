using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Collections;

public enum UIType
{
    None,
    OpenPalm,
    TwoFinger,
    ThreeFinger,
    WristRadialUlnar,
    WristFlexionExtension,
    ForearmPronationSupination
}

public class MasterGameManager : MonoBehaviour
{
    [Header("Gesture UI Panels (Auto-Filled by Tag)")]
    private Dictionary<UIType, GameObject[]> uiGroups;

    public static string lastGestureKey;

    private bool isSceneLoading = false;

    [Header("Testing")]
    [SerializeField] string _testScene = "DINO";
    [SerializeField] string _testGesture = "33";

    [ContextMenu("Test Load dino")]
    public void TestLoadSceneFlappy()
    {
        SaveGesture(_testScene, _testGesture);
    }

    public void SaveGesture(string sceneName, string gestureKey)
    {
        lastGestureKey = gestureKey;
        LoadGameScene(sceneName);
    }

    public void FindGestureUIsByTag()
    {
        uiGroups = new Dictionary<UIType, GameObject[]>
        {
            { UIType.OpenPalm, FindUIByTag("OpenPalmUI")},
            { UIType.TwoFinger, FindUIByTag("TwoFingerUI") },
            { UIType.ThreeFinger, FindUIByTag("ThreeFingerUI") },
            { UIType.WristRadialUlnar, FindUIByTag("WristRadialUlnarUI") },
            { UIType.WristFlexionExtension, FindUIByTag("WristFlexionExtensionUI") },
            { UIType.ForearmPronationSupination, FindUIByTag("ForearmPronationSupinationUI")}};
    }


    private GameObject[] FindUIByTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        if (objs.Length <= 0)
        {
            Debug.LogWarning($"UI object with tag '{tag}' not found in scene {SceneManager.GetActiveScene().name}");
        }
        return objs;
    }

    void LoadGameScene(string keyCode)
    {
        switch (keyCode)
        {
            case "DINO":
                SceneManager.LoadScene("DinoLvl1");
                break;
            case "SPACE":
                SceneManager.LoadScene("SpaceInvadersvl1");
                break;
            case "FLAPPY":
                SceneManager.LoadScene("FlappyBirdlvl1");
                break;
            case "BRICK":
                SceneManager.LoadScene("BrickLevel01");
                break;
        }

        Invoke(nameof(ResetLoadingFlag), 0.5f);
    }

    private void ResetLoadingFlag()
    {
        isSceneLoading = false;
    }
    // ========================
    // AUTHORIZATION TOKEN MANAGEMENT
    // ========================
    public static string userToken = "";
    private static bool tokenInitialized = false;

    /// <summary>
    /// Set authorization token from Android/external source
    /// Call this method when receiving token from Android app
    /// </summary>
    /// <param name="token">JWT Bearer token for API authentication</param>
    public void Authorization(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[MasterGameManager] ❌ ERROR: Attempted to set empty/null token!");
            Debug.LogError("[MasterGameManager] Token was NOT updated. Please provide a valid token.");
            return;
        }

        // Check if this is a token change
        bool isTokenChange = !string.IsNullOrEmpty(userToken) && userToken != token;

        if (isTokenChange)
        {
            Debug.Log("[MasterGameManager] ========================================");
            Debug.Log("[MasterGameManager] 🔄 TOKEN CHANGE DETECTED");
            Debug.Log("[MasterGameManager] ========================================");
            Debug.Log($"[MasterGameManager] Old Token (first 50 chars): {userToken.Substring(0, Mathf.Min(50, userToken.Length))}...");
            Debug.Log($"[MasterGameManager] New Token (first 50 chars): {token.Substring(0, Mathf.Min(50, token.Length))}...");
        }

        // Update token
        userToken = token;
        tokenInitialized = true;

        // Save to PlayerPrefs for persistence across app restarts (optional)
        PlayerPrefs.SetString("UserAuthToken", token);
        PlayerPrefs.Save();

        if (isTokenChange)
        {
            Debug.Log("[MasterGameManager] ✅ TOKEN UPDATED SUCCESSFULLY");
            Debug.Log("[MasterGameManager] All future API calls will use the new token.");
            Debug.Log("[MasterGameManager] ========================================");
        }
        else
        {
            Debug.Log("[MasterGameManager] ========================================");
            Debug.Log("[MasterGameManager] ✅ AUTHORIZATION TOKEN SAVED");
            Debug.Log("[MasterGameManager] ========================================");
            Debug.Log($"[MasterGameManager] Token (first 50 chars): {token.Substring(0, Mathf.Min(50, token.Length))}...");
            Debug.Log($"[MasterGameManager] Token Length: {token.Length} characters");
            Debug.Log("[MasterGameManager] Token saved to memory and PlayerPrefs");
            Debug.Log("[MasterGameManager] ✅ Ready to make authenticated API calls");
            Debug.Log("[MasterGameManager] ========================================");
        }
    }

    /// <summary>
    /// Check if a valid authorization token is set
    /// </summary>
    /// <returns>True if token is set, false otherwise</returns>
    public static bool HasValidToken()
    {
        bool hasToken = !string.IsNullOrEmpty(userToken);

        if (!hasToken)
        {
            Debug.LogWarning("[MasterGameManager] ⚠️ No authorization token set!");
            Debug.LogWarning("[MasterGameManager] Call Authorization(token) to set token.");
        }

        return hasToken;
    }

    /// <summary>
    /// Get the current authorization token
    /// </summary>
    /// <returns>Current token or empty string if not set</returns>
    public static string GetToken()
    {
        if (string.IsNullOrEmpty(userToken))
        {
            Debug.LogWarning("[MasterGameManager] ⚠️ Token requested but not set. Checking PlayerPrefs...");

            // Try to load from PlayerPrefs
            string savedToken = PlayerPrefs.GetString("UserAuthToken", "");
            if (!string.IsNullOrEmpty(savedToken))
            {
                userToken = savedToken;
                tokenInitialized = true;
                Debug.Log("[MasterGameManager] ✅ Token restored from PlayerPrefs");
            }
            else
            {
                Debug.LogWarning("[MasterGameManager] ⚠️ No token found in PlayerPrefs either.");
            }
        }

        return userToken;
    }

    /// <summary>
    /// Clear the authorization token (logout)
    /// </summary>
    public void ClearToken()
    {
        Debug.Log("[MasterGameManager] ========================================");
        Debug.Log("[MasterGameManager] 🗑️ CLEARING AUTHORIZATION TOKEN");
        Debug.Log("[MasterGameManager] ========================================");

        userToken = "";
        tokenInitialized = false;
        PlayerPrefs.DeleteKey("UserAuthToken");
        PlayerPrefs.Save();

        Debug.Log("[MasterGameManager] ✅ Token cleared from memory and PlayerPrefs");
        Debug.Log("[MasterGameManager] User is now logged out");
        Debug.Log("[MasterGameManager] ========================================");
    }

    /// <summary>
    /// Load token from PlayerPrefs on app start (if exists)
    /// Call this in Awake() or Start()
    /// </summary>
    private void LoadTokenFromStorage()
    {
        if (!tokenInitialized && string.IsNullOrEmpty(userToken))
        {
            string savedToken = PlayerPrefs.GetString("UserAuthToken", "");
            if (!string.IsNullOrEmpty(savedToken))
            {
                userToken = savedToken;
                tokenInitialized = true;
                Debug.Log("[MasterGameManager] ✅ Token restored from previous session");
                Debug.Log($"[MasterGameManager] Token (first 50 chars): {savedToken.Substring(0, Mathf.Min(50, savedToken.Length))}...");
            }
        }
    }

    private void Awake()
    {
        // Try to load token from storage on app start
        LoadTokenFromStorage();
        Debug.unityLogger.logEnabled = false;
    }

   


    public void LoadGameGestures(string keys)
    {
        if (string.IsNullOrEmpty(keys))
        {
            Debug.LogWarning("Empty gesture key provided");
            return;
        }

        //FindGestureUIsByTag();


        //DisableAllGestures();


        /*switch (keys)
        {
            case "30":
                if (openPalmUI) openPalmUI.SetActive(true);
                break;
            case "32":
                if (twoFingerUI) twoFingerUI.SetActive(true);
                break;
            case "33":
                if (threeFingerUI) threeFingerUI.SetActive(true);
                break;
            case "34":
                if (wristRadialUlnarUI) wristRadialUlnarUI.SetActive(true);
                Debug.Log("34");
                break;
            case "35":
                if (wristFlexionExtensionUI) wristFlexionExtensionUI.SetActive(true);
                break;
            case "36":
                if (forearmPronationSupinationUI) forearmPronationSupinationUI.SetActive(true);
                break;
        }*/


        lastGestureKey = keys;
        //PlayerPrefs.SetString("LastGesture", lastGestureKey);
        //PlayerPrefs.Save();

        ApplyGestureUI(keys);
    }

    private void ApplyGestureUI(string keys)
    {
        switch (keys)
        {
            case "30":
                SetUIActive(UIType.OpenPalm);
                break;
            case "32":
                SetUIActive(UIType.TwoFinger);
                break;
            case "33":
                SetUIActive(UIType.ThreeFinger);
                break;
            case "34":
                SetUIActive(UIType.WristRadialUlnar);
                break;
            case "35":
                SetUIActive(UIType.WristFlexionExtension);
                break;
            case "36":
                SetUIActive(UIType.ForearmPronationSupination);
                break;
            default:
                SetUIActive(UIType.None);
                break;
        }

        Debug.Log($"Gesture {keys} applied in scene {SceneManager.GetActiveScene().name}");
    }

    private void SetUIActive(UIType activeUI)
    {
        foreach (var group in uiGroups)
        {
            bool isActive = group.Key == activeUI;
            foreach (var go in group.Value)
            {
                if (go != null) go.SetActive(isActive);
            }
        }
    }

    private void DisableAllGestures()
    {
        SetUIActive(UIType.None);
    }
}
