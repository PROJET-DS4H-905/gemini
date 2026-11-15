using System;

public enum MqttQosLevel : byte
{
  AtMostOnce = 0,
  AtLeastOnce = 1,
  ExactlyOnce = 2
}

public interface IMqttConnection
{
  bool IsConnected { get; }

  event Action Connected;

  event Action Disconnected;

  event Action<string, byte[]> MessageReceived;

  event Action<string> Error;

  IMqttConnection Connect(MqttSecrets secrets);

  void Subscribe(string topic, MqttQosLevel qosLevel = MqttQosLevel.AtMostOnce);

  void Unsubscribe(string topic);

  void Publish(string topic, byte[] payload,
    MqttQosLevel qosLevel = MqttQosLevel.AtLeastOnce, bool retain = false);

  void Disconnect();
}
