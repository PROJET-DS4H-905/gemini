using UnityEngine;
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
    if (connector == null || connector.Connection == null)
      return;

    connector.OnConnected -= InitSubscriber;
    connector.Connection.MessageReceived -= OnMessageReceived;
    connector.Connection.Unsubscribe(topic);
  }

  private void Update()
  {
    // OnMessageReceived peut tourner sur le thread M2Mqtt ; on applique ici, sur le main thread.
    if (pendingText == null || infoCard == null)
      return;

    string text = pendingText;
    pendingText = null;
    infoCard.SetText(text);
  }

//-------------Class Functions-------------

  private void InitSubscriber()
  {
    if (logicStarted || connector == null || connector.Connection == null)
      return;

    logicStarted = true;

    // L'abonnement est en place : on n'a plus besoin de l'événement de connexion.
    connector.OnConnected -= InitSubscriber;

    connector.Connection.MessageReceived += OnMessageReceived;
    connector.Connection.Subscribe(topic, MqttQosLevel.AtMostOnce);
  }

  void OnMessageReceived(string receivedTopic, byte[] payload)
  {
    // Check if the topic is the desired one
    if (topic != receivedTopic) return;

    string msg = Encoding.UTF8.GetString(payload);

    Debug.Log($"Subscriber recieved [{receivedTopic}] {msg}");

    if (infoCard == null) return;

    // Tentative de parsing du JSON reçu. Ce handler peut tourner sur le thread M2Mqtt :
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
      Debug.LogWarning("Erreur de parsing JSON : " + ex.Message);
    }
  }

//-------------Coroutine-------------
  private IEnumerator WaitForMqtt()
  {
    // 1) Wait until the connector and its connection exist (secrets loading is async on WebGL).
    while (connector == null || connector.Connection == null)
      yield return null;

    // 2) Attach first, then check: avoids missing a connection that completes during setup.
    connector.OnConnected += InitSubscriber;

    if (connector.Connection.IsConnected)
      InitSubscriber();
  }
}
