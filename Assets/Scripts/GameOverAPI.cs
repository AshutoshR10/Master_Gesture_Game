using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameOverAPI : MonoBehaviour
{
    public static GameOverAPI instance;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void SubmitAnswer(string gameId, string gameProgress, int gameScore, string gameResult)
    {
        StartCoroutine(SubmitGameProgress(gameId, gameProgress, gameScore, gameResult));
    }
    public IEnumerator SubmitGameProgress(string gameId, string gameProgress, int gameScore, string gameResult)
    {
        string url = "http://145.223.23.182:5005/api/game/save";

        // ✅ Get token from MasterGameManager (with automatic fallback to storage)
        string token = MasterGameManager.GetToken();

        // If token is still empty, use the developer-provided token for testing
        if (string.IsNullOrEmpty(token))
        {
            // Temporary token for testing (provided by developer)
            token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY5MWVjMGJiMjVkZGI3YjQ4NGE0NTJkZiIsImlhdCI6MTc2NDE0NjMxNywiZXhwIjoxNzY0MjMyNzE3fQ.bOdrPcr1GK8VMZV-ClUZbrQu9D4XERrEII4ndWBAX8g";
            Debug.LogWarning("[GameOverAPI] ========================================");
            Debug.LogWarning("[GameOverAPI] ⚠️ NO USER TOKEN FOUND");
            Debug.LogWarning("[GameOverAPI] Using fallback test token for development");
            Debug.LogWarning("[GameOverAPI] Production should set token via MasterGameManager.Authorization()");
            Debug.LogWarning("[GameOverAPI] ========================================");
        }
        else
        {
            Debug.Log($"[GameOverAPI] ✅ Using authenticated token (first 50 chars): {token.Substring(0, Mathf.Min(50, token.Length))}...");
        }

        // Build JSON body using SimpleJSON
        JSONClass body = new JSONClass();
        body["game_id"] = gameId;

        // ✅ FIX: Parse gameProgress JSON string into JSONNode
        JSONNode progressNode = JSON.Parse(gameProgress);
        body["game_progress"] = progressNode;

        body["game_result"] = gameResult;
        body["game_score"].AsInt = gameScore;

        string jsonBody = body.ToString();
        Debug.Log("Submitting JSON: " + jsonBody);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {

                Debug.Log("Success=" + request.downloadHandler.text);

            }
            else
            {
                Debug.LogError(":x: Quiz submission failed: " + request.error);
                Debug.LogError(request.downloadHandler.text);

            }
            request.Dispose();
        }
    }
}
