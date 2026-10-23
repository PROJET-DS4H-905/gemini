using UnityEngine;
using UnityEngine.UI;
using TMPro;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;
using Newtonsoft.Json;

public class Connector : MonoBehaviour
{
  [System.NonSerialized] public MqttClient client;
  public event Action OnConnected;

  [Header("MQTT Configuration")]
  [SerializeField] private string[] topics;
  private MqttSecrets secrets;
  [SerializeField] private string publishTopic = "FABLAB_21_22/unity/test/out";
  [SerializeField] private string messageToSend = "Bonjour de Unity 6.0 !";

  [Header("Reconnexion automatique")]
  public float reconnectCheckInterval = 5f; // Vérification toutes les 5 secondes
  private float reconnectTimer = 0f;
  private bool isReconnecting = false;
  private string clientId;

//-------------Unity LifeCycles-------------
  void Start()
  {
    clientId = System.Guid.NewGuid().ToString();
    secrets = MqttSecrets.Load();
    ConnectToMqtt();
  }

  void Update()
  {
    // 🔄 Vérification et reconnexion automatique
    CheckConnection();
  }

  void OnDestroy()
  {
    // Nettoyage propre lors de la destruction de l'objet
    if (client != null && client.IsConnected)
    {
      try
      {
        client.Disconnect();
        Debug.Log("🔌 Déconnecté du broker MQTT.");
      }
      catch (System.Exception ex)
      {
        Debug.LogWarning("⚠️ Erreur lors de la déconnexion MQTT : " + ex.Message);
      }
    }
  }

//-------------Class Functions-------------
  void ConnectToMqtt()
  {
    if (isReconnecting)
    return;

    if (secrets == null)
    return;

    isReconnecting = true;

    try
    {
      // Créer un nouveau client si nécessaire
      if (client == null) {
        client = new MqttClient(
          secrets.broker, secrets.port, true,
          null, null,
          MqttSslProtocols.TLSv1_2,
          (sender, cert, chain, sslPolicyErrors) => true
        );
      }

      // Tenter la connexion
      if (!client.IsConnected) {
        client.Connect(clientId, secrets.username, secrets.password);
      }

      if (client.IsConnected){
        Debug.Log("✅ Connecté au broker MQTT !");

        OnConnected?.Invoke();

        // S'abonner au topic
        foreach (string topic in topics)
        {
          Debug.Log($"subscribed to topic {topic}");
          client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        }

        client.Publish(publishTopic, Encoding.UTF8.GetBytes(messageToSend), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
        Debug.Log("📤 Message publié sur " + publishTopic + " : " + messageToSend);

      }
      else
      {
        Debug.LogWarning("❌ Connexion MQTT échouée, nouvelle tentative dans " + reconnectCheckInterval + "s");
      }
    }
    catch (System.Exception ex)
    {
      Debug.LogWarning("❗ Erreur MQTT : " + ex.Message + " | Nouvelle tentative dans " + reconnectCheckInterval + "s");
    }
    finally
    {
      isReconnecting = false;
    }
  }

  void CheckConnection()
  {
    reconnectTimer += Time.deltaTime;

    if (reconnectTimer >= reconnectCheckInterval)
    {
      reconnectTimer = 0f;

      // Vérifier si la connexion est perdue
      if (client == null || !client.IsConnected)
      {
        Debug.LogWarning("🔄 Connexion MQTT perdue, tentative de reconnexion...");
        ConnectToMqtt();
      }
    }
  }
}
