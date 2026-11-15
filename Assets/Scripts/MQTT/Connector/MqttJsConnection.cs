#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public class MqttJsConnection : IMqttConnection
{
  [DllImport("__Internal")]
  private static extern void MqttJs_Connect(int id, string target, string url, string clientId,
    string username, string password, int keepAlive, int reconnectPeriod);

  [DllImport("__Internal")]
  private static extern void MqttJs_Subscribe(int id, string topic, int qos);

  [DllImport("__Internal")]
  private static extern void MqttJs_Unsubscribe(int id, string topic);

  [DllImport("__Internal")]
  private static extern void MqttJs_Publish(int id, string topic, string payloadText, int qos, int retain);

  [DllImport("__Internal")]
  private static extern void MqttJs_Disconnect(int id);

  private const int KeepAliveSeconds = 60;
  private const int ReconnectPeriodMs = 5000;
  private const string DefaultWebSocketPath = "/mqtt";

  private static int nextId = 1;

  private readonly int id = nextId++;
  private readonly string clientId = Guid.NewGuid().ToString();
  private readonly Dictionary<string, int> topicRefCounts = new Dictionary<string, int>();
  private readonly Dictionary<string, MqttQosLevel> topicQos = new Dictionary<string, MqttQosLevel>();

  private MqttJsBridge bridge;
  private bool isConnected;
  private bool connectRequested;
  private bool disposed;

  public bool IsConnected => isConnected;

  public event Action Connected;
  public event Action Disconnected;
  public event Action<string, byte[]> MessageReceived;
  public event Action<string> Error;

  public IMqttConnection Connect(MqttSecrets secrets)
  {
    if (disposed || secrets == null || isConnected || connectRequested)
      return this;

    string url = ResolveUrl(secrets);
    EnsureBridge();

    if (bridge == null)
    {
      Error?.Invoke("Impossible de créer le pont JavaScript MQTT.");
      return this;
    }

    connectRequested = true;
    Debug.Log("Connexion WebSocket MQTT vers " + url);

    try
    {
      MqttJs_Connect(id, bridge.gameObject.name, url, clientId,
        secrets.username ?? string.Empty, secrets.password ?? string.Empty,
        KeepAliveSeconds, ReconnectPeriodMs);
    }
    catch (Exception ex)
    {
      connectRequested = false;
      Error?.Invoke(ex.Message);
    }

    return this;
  }

  public void Subscribe(string topic, MqttQosLevel qosLevel = MqttQosLevel.AtMostOnce)
  {
    if (string.IsNullOrEmpty(topic))
      return;

    topicRefCounts.TryGetValue(topic, out int count);
    topicRefCounts[topic] = count + 1;
    topicQos[topic] = qosLevel;

    // The full registry is replayed on every (re)connect.
    if (!disposed)
      MqttJs_Subscribe(id, topic, (int)qosLevel);
  }

  public void Unsubscribe(string topic)
  {
    if (string.IsNullOrEmpty(topic) || !topicRefCounts.TryGetValue(topic, out int count))
      return;

    if (count > 1)
    {
      topicRefCounts[topic] = count - 1;
      return;
    }

    topicRefCounts.Remove(topic);
    topicQos.Remove(topic);

    if (!disposed)
      MqttJs_Unsubscribe(id, topic);
  }

  public void Publish(string topic, byte[] payload,
    MqttQosLevel qosLevel = MqttQosLevel.AtLeastOnce, bool retain = false)
  {
    if (disposed || !isConnected || string.IsNullOrEmpty(topic))
      return;

    string payloadText = Encoding.UTF8.GetString(payload ?? Array.Empty<byte>());
    MqttJs_Publish(id, topic, payloadText, (int)qosLevel, retain ? 1 : 0);
  }

  public void Disconnect()
  {
    if (disposed)
      return;

    disposed = true;
    isConnected = false;
    connectRequested = false;

    try
    {
      MqttJs_Disconnect(id);
    }
    catch (Exception ex)
    {
      Debug.LogWarning("Erreur lors de la déconnexion WebSocket MQTT : " + ex.Message);
    }

    DestroyBridge();
  }

  public void HandleEvent(string json)
  {
    if (disposed)
      return;

    MqttJsEvent evt;
    try
    {
      evt = JsonConvert.DeserializeObject<MqttJsEvent>(json);
    }
    catch (Exception ex)
    {
      Debug.LogWarning("Événement MQTT.js illisible : " + ex.Message);
      return;
    }

    if (evt == null || string.IsNullOrEmpty(evt.type))
      return;

    switch (evt.type)
    {
      case "connected":
        isConnected = true;
        ReapplySubscriptions();
        Debug.Log("Connecté au broker MQTT (WebSocket) !");
        Connected?.Invoke();
        break;

      case "message":
        DispatchMessage(evt);
        break;

      case "close":
        isConnected = false;
        Disconnected?.Invoke();
        break;

      case "error":
        Error?.Invoke(evt.message ?? "Erreur MQTT.js");
        break;

      case "fatal":
        isConnected = false;
        connectRequested = false;
        Error?.Invoke(evt.message ?? "Erreur fatale MQTT.js");
        break;
    }
  }

  public void NotifyBridgeDestroyed()
  {
    bridge = null;

    if (disposed)
      return;

    bool wasConnected = isConnected;
    isConnected = false;
    connectRequested = false;

    // Stop the orphaned JS client so the Connector can create a fresh one.
    try
    {
      MqttJs_Disconnect(id);
    }
    catch (Exception ex)
    {
      Debug.LogWarning("Erreur lors de l'arrêt du client MQTT.js : " + ex.Message);
    }

    if (wasConnected)
      Disconnected?.Invoke();
  }

  private void ReapplySubscriptions()
  {
    foreach (KeyValuePair<string, int> entry in topicRefCounts)
    {
      if (entry.Value <= 0)
        continue;

      topicQos.TryGetValue(entry.Key, out MqttQosLevel qos);
      MqttJs_Subscribe(id, entry.Key, (int)qos);
    }
  }

  private void DispatchMessage(MqttJsEvent evt)
  {
    if (string.IsNullOrEmpty(evt.topic))
      return;

    byte[] payload = Encoding.UTF8.GetBytes(evt.payload ?? string.Empty);
    MessageReceived?.Invoke(evt.topic, payload);
  }

  private void EnsureBridge()
  {
    if (bridge != null)
      return;

    GameObject bridgeObject = new GameObject("MqttJsBridge_" + id);
    bridge = bridgeObject.AddComponent<MqttJsBridge>();
    bridge.Owner = this;
  }

  private void DestroyBridge()
  {
    if (bridge == null)
      return;

    MqttJsBridge bridgeToDestroy = bridge;
    bridge = null;

    if (bridgeToDestroy != null)
      UnityEngine.Object.Destroy(bridgeToDestroy.gameObject);
  }

  private static string ResolveUrl(MqttSecrets secrets)
  {
    if (!string.IsNullOrEmpty(secrets.websocketUrl))
      return secrets.websocketUrl.Trim();

    string broker = (secrets.broker ?? string.Empty).Trim();

    if (broker.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
        broker.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
      return broker;

    if (broker.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
      return "wss://" + broker.Substring("https://".Length);

    if (broker.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
      return "ws://" + broker.Substring("http://".Length);

    string scheme = secrets.useTls ? "wss" : "ws";
    string port = secrets.port > 0 ? ":" + secrets.port : string.Empty;
    return scheme + "://" + broker + port + DefaultWebSocketPath;
  }
}
#endif
