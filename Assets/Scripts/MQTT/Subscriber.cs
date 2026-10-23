using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

public class Subscriber : MonoBehaviour {

  [SerializeField] private Connector connector;
  private Coroutine waitRoutine;
  private bool logicStarted;
  private InfoCard infoCard;
  private volatile string pendingText;

  public string topic;

//-------------Unity LifeCycles-------------

  private void Awake()
  {
    infoCard = GetComponent<InfoCard>();
  }

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

  private void Update()
  {
    // OnMessageReceived tourne sur un thread M2Mqtt ; on applique ici, sur le main thread.
    if (pendingText == null || infoCard == null)
      return;

    string text = pendingText;
    pendingText = null;
    infoCard.SetText(text);
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

    Debug.Log($"Subscriber recieved [{receivedTopic}] {msg}");

    if (infoCard == null) return;

    // Tentative de parsing du JSON reçu. OnMessageReceived tourne sur un thread M2Mqtt :
    // on ne touche donc à AUCUN objet Unity ici, on prépare seulement la chaîne,
    // qui sera appliquée dans Update() sur le main thread.
    try
    {
      var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(msg);
      if (data == null || data.Count == 0) return;

      var sb = new StringBuilder();
      foreach (var kvp in data)
        sb.AppendLine($"{kvp.Key}: {kvp.Value}");

      pendingText = sb.ToString().TrimEnd();
    }
    catch (System.Exception ex)
    {
      Debug.LogWarning("⚠️ Erreur de parsing JSON : " + ex.Message);
    }
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
