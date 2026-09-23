# Device simulator

Stands in for the ESP32, which has been disassembled. It speaks the same four
`X-Device-Key` endpoints as `Esp32.IoT/Esp32.IoT.ino`, so the full control loop
(web command -> device poll -> ack -> status -> dashboard) can be exercised
without hardware.

**In Docker**

```bash
docker compose --profile simulator up -d
docker compose logs -f entrio-device-sim
```

**On the host** (reads the repo-root `.env` for `DEVICE_KEY`)

```bash
node --env-file=../../.env simulator.mjs
```

Environment: `SERVER_BASE_URL` (default `http://localhost:5263`), `DEVICE_KEY`,
`POLL_MS` (default 2000).
