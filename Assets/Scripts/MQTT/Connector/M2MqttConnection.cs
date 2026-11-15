#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine;

/// <summary>
/// Native/Editor implementation backed by M2Mqtt (raw TCP or TLS sockets).
/// </summary>
public class M2MqttConnection : IMqttConnection
{
  private readonly string clientId = Guid.NewGuid().ToString();
  private readonly Dictionary<string, int> topicRefCounts = new Dictionary<string, int>();
  private readonly Dictionary<string, MqttQosLevel> topicQos = new Dictionary<string, MqttQosLevel>();

  private MqttClient client;

  public bool IsConnected => client != null && client.IsConnected;

  public event Action Connected;
  public event Action Disconnected;
  public event Action<string, byte[]> MessageReceived;
  public event Action<string> Error;

  public IMqttConnection Connect(MqttSecrets secrets)
  {
    if (secrets == null || IsConnected)
      return this;

    try
    {
      if (client == null)
        CreateClient(secrets);

      if (!client.IsConnected)
        client.Connect(clientId, secrets.username, secrets.password);
    }
    catch (Exception ex)
    {
      Error?.Invoke(ex.Message);
      return this;
    }

    if (client != null && client.IsConnected)
    {
      ResubscribeAll();
      Debug.Log("Connecté au broker MQTT (M2Mqtt) !");
      Connected?.Invoke();
    }
    else
    {
      Error?.Invoke("Connexion MQTT impossible.");
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

    // First subscriber for this topic: tell the broker.
    if (count == 0 && IsConnected)
      SubscribeOnBroker(topic, qosLevel);
  }

  public void Unsubscribe(string topic)
  {
    if (string.IsNullOrEmpty(topic) || !topicRefCounts.TryGetValue(topic, out int count))
      return;

    // Other subscribers still need this topic.
    if (count > 1)
    {
      topicRefCounts[topic] = count - 1;
      return;
    }

    topicRefCounts.Remove(topic);
    topicQos.Remove(topic);

    if (IsConnected)
    {
      try
      {
        client.Unsubscribe(new[] { topic });
      }
      catch (Exception ex)
      {
        Error?.Invoke(ex.Message);
      }
    }
  }

  public void Publish(string topic, byte[] payload,
    MqttQosLevel qosLevel = MqttQosLevel.AtLeastOnce, bool retain = false)
  {
    if (!IsConnected || string.IsNullOrEmpty(topic))
      return;

    try
    {
      client.Publish(topic, payload ?? Array.Empty<byte>(), (byte)qosLevel, retain);
    }
    catch (Exception ex)
    {
      Error?.Invoke(ex.Message);
    }
  }

  public void Disconnect()
  {
    if (client == null)
      return;

    try
    {
      client.MqttMsgPublishReceived -= OnMqttMsgPublishReceived;
      client.ConnectionClosed -= OnConnectionClosed;

      if (client.IsConnected)
        client.Disconnect();
    }
    catch (Exception ex)
    {
      Debug.LogWarning("⚠️ Erreur lors de la déconnexion MQTT : " + ex.Message);
    }
    finally
    {
      client = null;
    }
  }

  private void CreateClient(MqttSecrets secrets)
  {
    client = new MqttClient(
      secrets.broker, secrets.port, secrets.useTls,
      null, null,
      MqttSslProtocols.TLSv1_2,
      (sender, cert, chain, sslPolicyErrors) => true
    );

    client.MqttMsgPublishReceived += OnMqttMsgPublishReceived;
    client.ConnectionClosed += OnConnectionClosed;
  }

  /// <summary>A clean session loses broker subscriptions on reconnect.</summary>
  private void ResubscribeAll()
  {
    foreach (KeyValuePair<string, int> entry in topicRefCounts)
    {
      if (entry.Value <= 0)
        continue;

      topicQos.TryGetValue(entry.Key, out MqttQosLevel qos);
      SubscribeOnBroker(entry.Key, qos);
    }
  }

  private void SubscribeOnBroker(string topic, MqttQosLevel qosLevel)
  {
    try
    {
      client.Subscribe(new[] { topic }, new[] { (byte)qosLevel });
    }
    catch (Exception ex)
    {
      Error?.Invoke(ex.Message);
    }
  }

  private void OnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
  {
    // M2Mqtt raises this on its network thread.
    MessageReceived?.Invoke(e.Topic, e.Message);
  }

  private void OnConnectionClosed(object sender, EventArgs e)
  {
    Disconnected?.Invoke();
  }
}
#endif
