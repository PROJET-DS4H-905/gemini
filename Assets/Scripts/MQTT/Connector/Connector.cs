using System;
using System.Collections;
using System.Text;
using UnityEngine;

public class Connector : MonoBehaviour
{
  public event Action OnConnected;

  [Header("MQTT Configuration")]
  [SerializeField] private string publishTopic = "FABLAB_21_22/unity/test/out";
  [SerializeField] private string messageToSend = "Bonjour de Unity 6.0 !";

  [Header("Reconnexion automatique")]
  public float reconnectCheckInterval = 5f;

  public IMqttConnection Connection { get; private set; }

  private MqttSecrets secrets;
  private float reconnectTimer = 0f;
  private string lastError;

//-------------Unity LifeCycles-------------
  void Awake()
  {
    Connection = MqttConnectionFactory.Create();
    Connection.Connected += HandleConnected;
    Connection.Disconnected += HandleDisconnected;
    Connection.Error += HandleError;
  }

  IEnumerator Start()
  {
    yield return MqttSecrets.LoadAsync(loaded => secrets = loaded);

    if (secrets == null)
      yield break;

    ConnectToMqtt();
  }

  void Update()
  {
    if (secrets == null || Connection == null)
      return;

    reconnectTimer += Time.deltaTime;

    if (reconnectTimer >= reconnectCheckInterval)
    {
      reconnectTimer = 0f;

      if (!Connection.IsConnected)
      {
        Debug.LogWarning("Connexion MQTT perdue, tentative de reconnexion...");
        ConnectToMqtt();
      }
    }
  }

  void OnDestroy()
  {
    if (Connection == null)
      return;

    Connection.Connected -= HandleConnected;
    Connection.Disconnected -= HandleDisconnected;
    Connection.Error -= HandleError;

    try
    {
      Connection.Disconnect();
      Debug.Log("Déconnecté du broker MQTT.");
    }
    catch (Exception ex)
    {
      Debug.LogWarning("Erreur lors de la déconnexion MQTT : " + ex.Message);
    }

    Connection = null;
  }

//-------------Class Functions-------------
  void ConnectToMqtt()
  {
    if (secrets == null || Connection == null)
      return;

    // Connect() renvoie l'objet de connexion à utiliser pour la suite.
    Connection = Connection.Connect(secrets);
  }

  void HandleConnected()
  {
    lastError = null;
    Debug.Log("Connecté au broker MQTT !");

    OnConnected?.Invoke();

    if (Connection == null)
      return;

    Connection.Publish(publishTopic, Encoding.UTF8.GetBytes(messageToSend), MqttQosLevel.AtLeastOnce, false);
    Debug.Log("Message publié sur " + publishTopic + " : " + messageToSend);
  }

  void HandleDisconnected()
  {
    Debug.LogWarning("Connexion MQTT perdue, tentative de reconnexion...");
  }

  void HandleError(string message)
  {
    if (message == lastError)
      return;

    lastError = message;
    Debug.LogWarning("Erreur MQTT : " + message);
  }
}
