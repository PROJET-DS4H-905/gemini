# WebGL MQTT plugin

`mqtt.min.jspre` is the MQTT.js v3.0.0 browser bundle, vendored verbatim.

- Source: https://unpkg.com/mqtt@3.0.0/dist/mqtt.min.js
- SHA-256: `2ced92964abf2353a857ff6be0a3ff159f86aa0e946087e96ca1404702ea7080`
- License: MIT - Copyright (c) MQTT.js contributors

Unity includes `.jspre` files through Emscripten's `--pre-js`, so the bundle is
prepended to the generated framework JS and exposes the global `mqtt` object.
`mqttjs.jslib` uses that object and bridges it to C# through
`[DllImport("__Internal")]` and `SendMessage`.

Keep both files in ES5 syntax: Unity only officially supports ES5 in
`.jslib`/`.jspre` files.
