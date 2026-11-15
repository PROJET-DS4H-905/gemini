using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[System.Serializable]
public class MqttSecrets
{
    public string broker;
    public int port;

    public bool useTls;

    public string username;
    public string password;

    // Optional full WebSocket URL, used only by the WebGL implementation.
    public string websocketUrl;

    public static IEnumerator LoadAsync(Action<MqttSecrets> onLoaded)
    {
        const string fileName = "secrets.json";
        bool useWebRequest = RequiresWebRequest();
        string location = useWebRequest
            ? Application.streamingAssetsPath + "/" + fileName
            : Path.Combine(Application.streamingAssetsPath, fileName);

        if (useWebRequest)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(location))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Unable to load MQTT secrets from {location}: {request.error}");
                    onLoaded?.Invoke(null);
                    yield break;
                }

                onLoaded?.Invoke(Parse(request.downloadHandler.text, location));
            }

            yield break;
        }

        if (!File.Exists(location))
        {
            Debug.LogError($"Secrets file not found at {location}. Copy secrets.example.json to secrets.json and fill in the values.");
            onLoaded?.Invoke(null);
            yield break;
        }

        string json;
        try
        {
            json = File.ReadAllText(location);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unable to read MQTT secrets at {location}: {ex.Message}");
            onLoaded?.Invoke(null);
            yield break;
        }

        onLoaded?.Invoke(Parse(json, location));
    }

    private static bool RequiresWebRequest()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return true;
#else
        // Android serves StreamingAssets from a jar:file:// URL.
        return Application.streamingAssetsPath.Contains("://");
#endif
    }

    private static MqttSecrets Parse(string json, string location)
    {
        try
        {
            MqttSecrets secrets = JsonConvert.DeserializeObject<MqttSecrets>(json);
            if (secrets == null)
                Debug.LogError($"MQTT secrets at {location} are empty.");

            return secrets;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unable to parse MQTT secrets at {location}: {ex.Message}");
            return null;
        }
    }
}
