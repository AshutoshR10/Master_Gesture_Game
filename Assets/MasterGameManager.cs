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
