#include <SPI.h>
#include <MFRC522.h>
#include <AccelStepper.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>

// =====================================================
// WIFI + BACKEND CONFIG
// =====================================================
// WIFI_SSID, WIFI_PASSWORD, SERVER_BASE_URL and DEVICE_KEY now live in
// secrets.h, which is gitignored so credentials stay out of the repository.
//
// FIRST-TIME SETUP: copy secrets.example.h to secrets.h (in this same folder)
// and fill in your values. The Arduino IDE compiles every file in the sketch
// folder, so no further configuration is needed.
#include "secrets.h"

// =====================================================
// RFID CONFIG
// =====================================================
#define SS_PIN   5
#define RST_PIN  4

MFRC522 rfid(SS_PIN, RST_PIN);

const char* allowedUIDs[] = {
  "11013503",
  "111353",
  "A1B2C3D4"
};
const int allowedCount = sizeof(allowedUIDs) / sizeof(allowedUIDs[0]);

#define GREEN_LED 33
#define RED_LED   25

// =====================================================
// LDR CONFIG
// =====================================================
const int LDR_PIN = 36;
const int FLASH_THRESHOLD = 500;
const unsigned long FLASH_WINDOW = 1500;

int flashCount = 0;
unsigned long firstFlashTime = 0;
bool wasBright = false;

// =====================================================
// AUTH WINDOW
// =====================================================
bool authWindowActive = false;
unsigned long authWindowStartTime = 0;
const unsigned long AUTH_WINDOW_DURATION = 10000;

// =====================================================
// HC-SR04 CONFIG
// =====================================================
#define TRIG_PIN 26
#define ECHO_PIN 32

const float OBSTACLE_DISTANCE_CM = 20.0;
const unsigned long OBSTACLE_CLEAR_TIME = 3000;

bool motorPaused = false;
unsigned long obstacleClearStart = 0;

// obstacle event spam prevention
bool obstacleLatched = false;

// ✅ NEW: remember intent to resume closing after obstacle clears
bool resumeClosingAfterObstacle = false;

// =====================================================
// STEPPER CONFIG
// =====================================================
#define IN1 13
#define IN2 12
#define IN3 14
#define IN4 27

AccelStepper stepper(AccelStepper::FULL4WIRE, IN1, IN3, IN2, IN4);

const long OPEN_POSITION   = 4096;
const long CLOSED_POSITION = 0;

// =====================================================
// VENT CONFIG (percentage-based)
// =====================================================
const int MIN_VENT_PERCENT = 1;
const int MAX_VENT_PERCENT = 99;

// =====================================================
// DOOR STATE
// =====================================================
enum class DoorState : uint8_t {
  Closed,
  Opening,
  Open,
  Closing,
  Stopped
};

DoorState doorState = DoorState::Closed;

bool motorActive = false;
bool opening = true; // true while moving to OPEN_POSITION, false while moving to CLOSED_POSITION

// Venting target (absolute step position)
bool ventingActive = false;
long ventTargetPosition = 0;

enum class LastSource : uint8_t { System, LocalRFID, Remote };
LastSource lastOpenSource = LastSource::System;

// =====================================================
// EVENT QUEUE (simple flags)
// =====================================================
bool pendingDoorOpenedEvent = false;
bool pendingDoorClosedEvent = false;
bool pendingObstacleEvent   = false;
bool pendingObstacleClearedEvent = false;

// =====================================================
// REMOTE COMMAND POLLING + ACK
// =====================================================
unsigned long lastCommandPollMs = 0;
String lastAckedCommandId = "";

// reduce serial spam for HTTP -1
unsigned long lastHttpErrorLogMs = 0;

// =====================================================
// WIFI FUNCTIONS
// =====================================================
void connectWiFi() {
  Serial.println("Connecting to WiFi...");
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  unsigned long start = millis();
  while (WiFi.status() != WL_CONNECTED) {
    delay(250);
    Serial.print(".");
    if (millis() - start > 15000) {
      Serial.println("\nWiFi FAILED (timeout)");
      return;
    }
  }

  Serial.println("\nWiFi connected!");
  Serial.print("ESP32 IP: ");
  Serial.println(WiFi.localIP());
}

void ensureWiFi() {
  if (WiFi.status() == WL_CONNECTED) return;

  static unsigned long lastAttempt = 0;
  if (millis() - lastAttempt < 5000) return; // retry every 5s
  lastAttempt = millis();

  Serial.println("WiFi disconnected -> reconnecting...");
  WiFi.disconnect();
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
}

// =====================================================
// HTTP HELPERS
// =====================================================
void httpPostJson(const String& url, const String& jsonBody, int timeoutMs = 900) {
  if (WiFi.status() != WL_CONNECTED) return;

  HTTPClient http;
  http.begin(url);
  http.setTimeout(timeoutMs);
  http.addHeader("Content-Type", "application/json");
  http.addHeader("X-Device-Key", DEVICE_KEY);

  int code = http.POST(jsonBody);

  if (code < 0 && millis() - lastHttpErrorLogMs > 2000) {
    lastHttpErrorLogMs = millis();
    Serial.print("[HTTP] POST error ");
    Serial.print(code);
    Serial.print(" -> ");
    Serial.println(http.errorToString(code));
  }

  http.end();
}

int httpGet(const String& url, String& outBody, int timeoutMs = 900) {
  if (WiFi.status() != WL_CONNECTED) return -1000;

  HTTPClient http;
  http.begin(url);
  http.setTimeout(timeoutMs);
  http.addHeader("X-Device-Key", DEVICE_KEY);

  int code = http.GET();
  if (code == 200) outBody = http.getString();

  if (code < 0 && millis() - lastHttpErrorLogMs > 2000) {
    lastHttpErrorLogMs = millis();
    Serial.print("[HTTP] GET error ");
    Serial.print(code);
    Serial.print(" -> ");
    Serial.println(http.errorToString(code));
  }

  http.end();
  return code;
}

// =====================================================
// BACKEND EVENTS
// =====================================================
void sendDeviceEvent(const char* type, const char* source) {
  String body = String("{\"type\":\"") + type + "\",\"source\":\"" + source + "\"}";
  httpPostJson(String(SERVER_BASE_URL) + "/api/device/events", body);
}

// =====================================================
// STATUS UPDATES → /api/device/status
// =====================================================
void sendStatusUpdate() {
  if (WiFi.status() != WL_CONNECTED) return;

  HTTPClient http;
  http.begin(String(SERVER_BASE_URL) + "/api/device/status");
  http.addHeader("Content-Type", "application/json");
  http.addHeader("X-Device-Key", DEVICE_KEY);

  StaticJsonDocument<256> doc;

  doc["deviceId"] = "0f8fad5b-d9cb-469f-a165-70867728950e";   // <-- IMPORTANT
  doc["doorState"] = (int)doorState;
  doc["positionPercent"] = map(stepper.currentPosition(), CLOSED_POSITION, OPEN_POSITION, 0, 100);
  doc["obstacleDetected"] = obstacleDetected();

  String body;
  serializeJson(doc, body);

  int code = http.POST(body);

  Serial.print("STATUS POST -> ");
  Serial.println(code);

  http.end();
}


void processPendingHttpEvents() {
  ensureWiFi();
  if (WiFi.status() != WL_CONNECTED) return;

  if (pendingDoorOpenedEvent) {
    const char* src = "System";
    if (lastOpenSource == LastSource::LocalRFID) src = "LocalRFID";
    else if (lastOpenSource == LastSource::Remote) src = "Remote";

    sendDeviceEvent("DoorOpened", src);
    pendingDoorOpenedEvent = false;
  }

  if (pendingDoorClosedEvent) {
    sendDeviceEvent("DoorClosed", "System");
    pendingDoorClosedEvent = false;
  }

  if (pendingObstacleEvent) {
    sendDeviceEvent("ObstacleDetected", "System");
    pendingObstacleEvent = false;
  }

  if (pendingObstacleClearedEvent) {
    sendDeviceEvent("ObstacleCleared", "System");
    pendingObstacleClearedEvent = false;
  }
}

// =====================================================
// UID COMPARISON
// =====================================================
bool isAuthorized(String uid) {
  for (int i = 0; i < allowedCount; i++) {
    if (uid == allowedUIDs[i]) return true;
  }
  return false;
}

// =====================================================
// AUTH WINDOW TIMEOUT
// =====================================================
void checkAuthWindowTimeout() {
  if (authWindowActive && millis() - authWindowStartTime > AUTH_WINDOW_DURATION) {
    authWindowActive = false;
    flashCount = 0;
    Serial.println("Auth window expired");
  }
}

// =====================================================
// FLASH DETECTION
// =====================================================
void detectFlashes() {
  int rawValue = analogRead(LDR_PIN);
  bool isBright = rawValue > FLASH_THRESHOLD;

  if (!authWindowActive) {
    wasBright = isBright;
    return;
  }

  if (isBright && !wasBright) {
    unsigned long now = millis();

    if (flashCount == 0) {
      flashCount = 1;
      firstFlashTime = now;
      Serial.println("First flash detected");
    }
    else if (flashCount == 1 && (now - firstFlashTime <= FLASH_WINDOW)) {
      Serial.println("Second flash -> OPEN");
      authWindowActive = false;
      flashCount = 0;

      // open from local logic
      extern void triggerMotorLocal();
      triggerMotorLocal();
    }
  }

  if (flashCount == 1 && (millis() - firstFlashTime > FLASH_WINDOW)) {
    flashCount = 0;
  }

  wasBright = isBright;
}

// =====================================================
// HC-SR04 OBSTACLE DETECTION
// =====================================================
bool obstacleDetected() {
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);

  long duration = pulseIn(ECHO_PIN, HIGH, 25000);
  if (duration == 0) return false;

  float distance = duration * 0.0343 / 2;
  return distance < OBSTACLE_DISTANCE_CM;
}

// =====================================================
// STEPPER MOTION HELPERS
// =====================================================
void cancelStepperTargetToCurrent() {
  long pos = stepper.currentPosition();
  stepper.setCurrentPosition(pos);
  stepper.moveTo(pos);
}

void startOpen(LastSource src) {
  if (doorState == DoorState::Open || doorState == DoorState::Opening) {
    Serial.println("Open ignored (already open/opening)");
    return;
  }

  // if we were waiting to resume closing from obstacle, opening cancels that
  resumeClosingAfterObstacle = false;

  cancelStepperTargetToCurrent();

  motorActive = true;
  motorPaused = false;
  opening = true;

  doorState = DoorState::Opening;
  sendStatusUpdate();
  lastOpenSource = src;

  stepper.moveTo(OPEN_POSITION);
  Serial.println("Door in motion (opening)");
}

void startClose() {
  if (doorState == DoorState::Closed || doorState == DoorState::Closing) {
    Serial.println("Close ignored (already closed/closing)");
    return;
  }

  cancelStepperTargetToCurrent();

  motorActive = true;
  motorPaused = false;
  opening = false;

  doorState = DoorState::Closing;
  sendStatusUpdate();

  // reset obstacle tracking for a fresh close cycle
  obstacleLatched = false;
  obstacleClearStart = 0;

  stepper.moveTo(CLOSED_POSITION);
  Serial.println("Door in motion (closing)");
}

void stopDoor() {
  if (!motorActive && !motorPaused) {
    Serial.println("Stop ignored (already idle)");
    return;
  }

  // STOP should cancel resume intent too
  resumeClosingAfterObstacle = false;
  ventingActive = false;

  stepper.stop();
  motorActive = false;
  motorPaused = true;
  doorState = DoorState::Stopped;
  sendStatusUpdate();

  cancelStepperTargetToCurrent();
  Serial.println("Door STOPPED");
}

// =====================================================
// MOTOR (local trigger)
// =====================================================
void triggerMotorLocal() {
  startOpen(LastSource::LocalRFID);
}

// =====================================================
// RFID LOGIC
// =====================================================
void checkRFID() {
  if (!rfid.PICC_IsNewCardPresent()) return;
  if (!rfid.PICC_ReadCardSerial()) return;

  String uid = "";
  for (byte i = 0; i < rfid.uid.size; i++) {
    uid += String(rfid.uid.uidByte[i], HEX);
  }
  uid.toUpperCase();

  Serial.print("RFID UID: ");
  Serial.println(uid);

  if (isAuthorized(uid)) {
    Serial.println("RFID AUTH OK -> auth window opened + OPEN immediately");
    digitalWrite(GREEN_LED, HIGH);

    authWindowActive = true;
    authWindowStartTime = millis();

    flashCount = 0;                  // reset flashes
    Serial.println("Auth window opened — waiting for double flash");

    delay(250);
    digitalWrite(GREEN_LED, LOW);
  } else {
    Serial.println("RFID DENIED");
    digitalWrite(RED_LED, HIGH);
    delay(250);
    digitalWrite(RED_LED, LOW);
  }

  rfid.PICC_HaltA();
  rfid.PCD_StopCrypto1();
}

// =====================================================
// REMOTE COMMANDS (poll + ack)
// =====================================================
void ackCommand(const String& id) {
  if (WiFi.status() != WL_CONNECTED) return;

  HTTPClient http;
  http.begin(String(SERVER_BASE_URL) + "/api/device/commands/" + id + "/ack");
  http.setTimeout(900);
  http.addHeader("X-Device-Key", DEVICE_KEY);

  int code = http.POST("");

  if (code < 0 && millis() - lastHttpErrorLogMs > 2000) {
    lastHttpErrorLogMs = millis();
    Serial.print("[HTTP] ACK error ");
    Serial.print(code);
    Serial.print(" -> ");
    Serial.println(http.errorToString(code));
  }

  http.end();
}

void startVent(int percent, LastSource src) {
  if (percent <= 0 || percent >= 100) {
    Serial.println("Vent ignored (invalid percent)");
    return;
  }

  long target = CLOSED_POSITION +
                ((OPEN_POSITION - CLOSED_POSITION) * percent) / 100;

  Serial.print("VENT -> ");
  Serial.print(percent);
  Serial.print("% (");
  Serial.print(target);
  Serial.println(" steps)");

  resumeClosingAfterObstacle = false;
  cancelStepperTargetToCurrent();

  motorActive = true;
  motorPaused = false;
  ventingActive = true;
  ventTargetPosition = target;

  lastOpenSource = src;

  // determine direction
  long current = stepper.currentPosition();

  if (target > current) {
    // moving up
    opening = true;
    doorState = DoorState::Opening;
  } else {
    // moving down (like closing)
    opening = false;
    doorState = DoorState::Closing;
  }

  stepper.moveTo(target);
}



void applyRemoteCommand(int cmdType, int targetPercent) {
  // Open=0, Close=1, Stop=2, Vent=3

  if (cmdType == 2) {
    Serial.println("REMOTE -> STOP");
    stopDoor();
    return;
  }

  if (cmdType == 0) {
    Serial.println("REMOTE -> OPEN");
    startOpen(LastSource::Remote);
    return;
  }

  if (cmdType == 1) {
    Serial.println("REMOTE -> CLOSE");
    startClose();
    return;
  }

  if (cmdType == 3) {
    Serial.print("REMOTE -> VENT, requested=");
    Serial.println(targetPercent);

    int effective = 30; // fallback

    if (targetPercent >= MIN_VENT_PERCENT && targetPercent <= MAX_VENT_PERCENT) {
      effective = targetPercent;
    } else {
      Serial.println("VENT: invalid or missing percentage, using 30%");
    }

    startVent(effective, LastSource::Remote);
    return;
  }

  Serial.print("REMOTE -> Unknown commandType=");
  Serial.println(cmdType);
}

void pollRemoteCommands() {
  unsigned long interval = (motorActive || motorPaused) ? 1000 : 4000;
  if (millis() - lastCommandPollMs < interval) return;
  lastCommandPollMs = millis();

  ensureWiFi();
  if (WiFi.status() != WL_CONNECTED) return;

  String body;
  int code = httpGet(String(SERVER_BASE_URL) + "/api/device/commands/pending", body, 900);

  if (code == 204) return;
  if (code != 200) return;

  StaticJsonDocument<256> doc;
  DeserializationError err = deserializeJson(doc, body);
  if (err) {
    if (millis() - lastHttpErrorLogMs > 2000) {
      lastHttpErrorLogMs = millis();
      Serial.print("[JSON] parse error: ");
      Serial.println(err.c_str());
    }
    return;
  }

  String id = doc["id"] | "";
  int cmdType = doc["commandType"] | -1;

  int targetPercent = -1;
  if (!doc["targetPercentage"].isNull()) {
    targetPercent = doc["targetPercentage"].as<int>();
  }

  if (id.length() == 0 || cmdType < 0) return;
  if (id == lastAckedCommandId) return;

  applyRemoteCommand(cmdType, targetPercent);
  ackCommand(id);
  lastAckedCommandId = id;
}

// =====================================================
// MOTOR LOOP
// =====================================================
void runMotor() {
  if (!motorActive) {
    // if we are paused due to obstacle, we still need to check for clearance
    // (this is the reason resume used to never happen in your previous build)
    if (motorPaused && resumeClosingAfterObstacle) {
      bool obstacle = obstacleDetected();

      if (!obstacle) {
        if (obstacleClearStart == 0) obstacleClearStart = millis();
        else if (millis() - obstacleClearStart >= OBSTACLE_CLEAR_TIME) {
          Serial.println("Obstacle cleared -> resume closing");
          motorPaused = false;
          obstacleClearStart = 0;
          obstacleLatched = false;

          pendingObstacleClearedEvent = true;

          resumeClosingAfterObstacle = false;
          startClose();
        }
      } else {
        // still blocked, reset clear timer
        obstacleClearStart = 0;
      }
    }
    return;
  }

  // Only check obstacle while closing (while moving)
  if (doorState == DoorState::Closing) {
    bool obstacle = obstacleDetected();

    if (obstacle) {
      if (!motorPaused) {
        Serial.println("Obstacle detected -> pause");
        stepper.stop();

        motorPaused = true;
        motorActive = false;
        doorState = DoorState::Stopped;

        // remember we must resume closing later
        resumeClosingAfterObstacle = true;
        sendStatusUpdate();

        // cancel target so it doesn't keep trying
        cancelStepperTargetToCurrent();

        if (!obstacleLatched) {
          obstacleLatched = true;
          pendingObstacleEvent = true;
        }

        obstacleClearStart = 0;
      }
      return; // paused; don't run motor further
    }
  }

  // run stepper
  if (!motorPaused) {
    stepper.run();
  }

  // reached target?
  if (!motorPaused && stepper.distanceToGo() == 0) {
    motorActive = false;

  if (ventingActive) {
    Serial.println("Vent position reached");
    ventingActive = false;
    doorState = DoorState::Stopped;   // ← IMPORTANT
    pendingDoorOpenedEvent = true;    // still valid logically
    sendStatusUpdate();
    return;
  }


    if (opening) {
      Serial.println("Door fully open");
      doorState = DoorState::Open;
      pendingDoorOpenedEvent = true;
      sendStatusUpdate();
      // no auto-close (as requested)
    } else {
      Serial.println("Door fully closed");
      doorState = DoorState::Closed;
      pendingDoorClosedEvent = true;
      sendStatusUpdate();
    }
  }
}

// =====================================================
// SETUP
// =====================================================
void setup() {
  Serial.begin(115200);

  pinMode(GREEN_LED, OUTPUT);
  pinMode(RED_LED, OUTPUT);

  pinMode(TRIG_PIN, OUTPUT);
  pinMode(ECHO_PIN, INPUT);

  SPI.begin();
  rfid.PCD_Init();

  stepper.setMaxSpeed(700.0);
  stepper.setAcceleration(80.0);
  stepper.setCurrentPosition(CLOSED_POSITION);
  stepper.moveTo(CLOSED_POSITION);

  connectWiFi();

  Serial.println("System ready. Waiting for RFID...");
}

// =====================================================
// MAIN LOOP
// =====================================================
void loop() {
  processPendingHttpEvents();
  pollRemoteCommands();

  static unsigned long lastStatusMs = 0;
  if (millis() - lastStatusMs > 10000) {
    sendStatusUpdate();
    lastStatusMs = millis();
  }

  if (!motorActive) {
    checkRFID();
    checkAuthWindowTimeout();

    if (authWindowActive) {
      detectFlashes();
    }
  }

  runMotor();
}
