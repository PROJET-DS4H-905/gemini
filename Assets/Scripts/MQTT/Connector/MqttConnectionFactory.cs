/// <summary>
/// Creates the MQTT connection implementation for the current platform.
/// WebGL builds use MQTT.js over WebSocket; every other build (Editor,
/// Android, desktop) keeps using M2Mqtt.
/// </summary>
public static class MqttConnectionFactory
{
  public static IMqttConnection Create()
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    return new MqttJsConnection();
#else
    return new M2MqttConnection();
#endif
  }
}
