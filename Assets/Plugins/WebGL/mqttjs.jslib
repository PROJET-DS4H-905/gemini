/*
 * MQTT over WebSocket bridge for Unity WebGL (MQTT.js).
 * Payloads are plain JSON text, never base64.
 * Everything must live inside mergeInto: Unity only emits library functions.
 */
mergeInto(LibraryManager.library, {
  MqttJs_Connect: function (id, targetPtr, urlPtr, clientIdPtr, usernamePtr, passwordPtr, keepAlive, reconnectPeriod) {
    var target = UTF8ToString(targetPtr);
    var username = UTF8ToString(usernamePtr);
    var password = UTF8ToString(passwordPtr);
    var options = {
      clientId: UTF8ToString(clientIdPtr),
      clean: true,
      keepalive: keepAlive || 60,
      reconnectPeriod: reconnectPeriod || 5000,
      protocolVersion: 4,
      resubscribe: false
    };
    var client;

    if (typeof mqtt === 'undefined') {
      setTimeout(function () {
        SendMessage(target, 'OnMqttJsEvent', JSON.stringify({ type: 'fatal', message: 'mqtt.js is not loaded' }));
      }, 0);
      return;
    }

    if (username) options.username = username;
    if (password) options.password = password;

    if (!Module.mqttjsClients) Module.mqttjsClients = {};

    client = mqtt.connect(UTF8ToString(urlPtr), options);
    Module.mqttjsClients[id] = client;

    client.on('connect', function () {
      SendMessage(target, 'OnMqttJsEvent', JSON.stringify({ type: 'connected' }));
    });

    client.on('message', function (topic, payload) {
      SendMessage(target, 'OnMqttJsEvent', JSON.stringify({
        type: 'message',
        topic: topic,
        payload: payload.toString()
      }));
    });

    client.on('close', function () {
      SendMessage(target, 'OnMqttJsEvent', JSON.stringify({ type: 'close' }));
    });

    client.on('error', function (error) {
      SendMessage(target, 'OnMqttJsEvent', JSON.stringify({ type: 'error', message: error.message }));
    });
  },

  MqttJs_Subscribe: function (id, topicPtr, qos) {
    var client = Module.mqttjsClients && Module.mqttjsClients[id];
    if (client && client.connected)
      client.subscribe(UTF8ToString(topicPtr), { qos: qos });
  },

  MqttJs_Unsubscribe: function (id, topicPtr) {
    var client = Module.mqttjsClients && Module.mqttjsClients[id];
    if (client && client.connected)
      client.unsubscribe(UTF8ToString(topicPtr));
  },

  MqttJs_Publish: function (id, topicPtr, payloadPtr, qos, retain) {
    var client = Module.mqttjsClients && Module.mqttjsClients[id];
    if (client && client.connected)
      client.publish(UTF8ToString(topicPtr), UTF8ToString(payloadPtr), { qos: qos, retain: !!retain });
  },

  MqttJs_Disconnect: function (id) {
    var client = Module.mqttjsClients && Module.mqttjsClients[id];
    if (!client)
      return;

    delete Module.mqttjsClients[id];
    client.end(true);
  }
});
