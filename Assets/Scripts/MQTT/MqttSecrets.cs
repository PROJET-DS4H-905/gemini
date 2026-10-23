using System.IO;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class MqttSecrets
{
    public string broker;
    public int port;
    public string username;
    public string password;

    public static MqttSecrets Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "secrets.json");
        if (!File.Exists(path))
        {
            Debug.LogError($"Secrets file not found at {path}. Copy secrets.example.json to secrets.json and fill in the values.");
            return null;
        }

        return JsonConvert.DeserializeObject<MqttSecrets>(File.ReadAllText(path));
    }
}
