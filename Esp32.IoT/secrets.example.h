// =====================================================
// TEMPLATE — tracked in git. Copy to "secrets.h" and fill in real values:
//     cp secrets.example.h secrets.h
// secrets.h is gitignored and must never be committed.
//
// WHY THESE FOUR AND NOT THE REST
// -------------------------------
// These are the bootstrap set: the device needs every one of them BEFORE it can
// reach the backend, so none of them can be fetched from the API or read from
// appsettings.json. Everything else (pin mappings, distances, thresholds) is a
// hardware fact and stays in the sketch.
//
// Keep DEVICE_KEY identical to DEVICE_KEY in the repo-root .env — the backend
// seeds its device row from that value and compares the X-Device-Key header
// against the stored hash.
// =====================================================

#pragma once

// --- WiFi ---------------------------------------------------------------
const char* WIFI_SSID     = "YourNetworkName";
const char* WIFI_PASSWORD = "YourNetworkPassword";

// --- Backend ------------------------------------------------------------
// Your PC's LAN IP, not "localhost": the ESP32 resolves this on the network,
// so localhost would point at the ESP32 itself. Port 5263 is where the API is
// published, whether it runs in Docker or directly from Visual Studio.
// Find it with:  ipconfig   (look for IPv4 Address on your active adapter)
const char* SERVER_BASE_URL = "http://192.168.1.100:5263";

// Shared secret sent as the X-Device-Key header on every request.
const char* DEVICE_KEY = "replace-with-the-DEVICE_KEY-from-your-.env";
