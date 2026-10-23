using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

public class Subscriber : MonoBehaviour {

  [SerializeField] private Connector connector;
  private Coroutine waitRoutine;
  private bool logicStarted;

  public string topic;

//-------------Unity LifeCycles-------------

  private void OnEnable()
  {
    if (logicStarted) return;                 // don't re-run
    waitRoutine = StartCoroutine(WaitForMqtt());
  }

  private void OnDestroy()
  {
    if (connector.client != null)
    {
      connector.client.MqttMsgPublishReceived -= OnMessageReceived;
    }
  }

//-------------Class Functions-------------

  private void InitSubscriber()
  {
    if (logicStarted) return;
    logicStarted = true;
    connector.client.MqttMsgPublishReceived += OnMessageReceived;
  }

  void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
  {
    string receivedTopic = e.Topic;

    // Check if the topic is the desired one
    if (topic != receivedTopic) return;

    string msg = Encoding.UTF8.GetString(e.Message);

    string formattedMsg = $"[{receivedTopic}] {msg}";

    Debug.Log($"Subscriber recieved {formattedMsg}");

    // Tentative de parsing du JSON reçu
    try
    {
     SensorData data = JsonConvert.DeserializeObject<SensorData>(msg);
     if (data != null)
     {
       Debug.Log(data);
     }
    }
    catch (System.Exception ex)
    {
     Debug.LogWarning("⚠️ Erreur de parsing JSON : " + ex.Message);
    }
  }

  // Classe pour désérialiser le JSON
  private class SensorData
  {
    public float value;
  }

//-------------Coroutine-------------
  private IEnumerator WaitForMqtt()
  {
    // 1) Wait until the manager exists at all (Awake may not have run yet)
    while (connector.client == null)
      yield return null;

    // 2) Fast path: already connected
    if (connector.client.IsConnected)
    {
      InitSubscriber();
      yield break;
    }

    // 3) Slow path: subscribe to the event and wait
    connector.OnConnected += InitSubscriber;
  }
}
