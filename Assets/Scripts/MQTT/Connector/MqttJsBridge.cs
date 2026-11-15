#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Scripting;

[Serializable, Preserve]
public class MqttJsEvent
{
  public string type;
  public string topic;
  public string payload; // message text
  public string message;
}

[Preserve]
public class MqttJsBridge : MonoBehaviour
{
  public MqttJsConnection Owner;

  [Preserve]
  public void OnMqttJsEvent(string json)
  {
    if (Owner != null)
      Owner.HandleEvent(json);
  }

  private void OnDestroy()
  {
    if (Owner != null)
      Owner.NotifyBridgeDestroyed();
  }
}
#endif
