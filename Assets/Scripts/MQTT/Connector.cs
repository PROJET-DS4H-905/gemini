using UnityEngine;
using UnityEngine.UI;
using TMPro;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;
using Newtonsoft.Json;

public class Connector : MonoBehaviour
{
  private MqttClient client;

  [Header("MQTT Configuration")]
  public string broker = "urlbroker";
  public int port = 8443;
  public string username = "******";
  public string password = "****";

  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }
}
