#!/bin/sh
# Writes /usr/share/nginx/html/env.js from the container environment, on every start.
#
# This is the container half of the runtime-config pair; the local half is
# Frontend.IoT/scripts/generate-env.mjs. Both produce the same window.__env
# shape, so the app cannot tell which one ran.
#
# Run automatically by the nginx image, which executes everything in
# /docker-entrypoint.d/ before nginx itself starts.
set -eu

: "${API_BASE_URL:?API_BASE_URL is not set on the entrio-web service}"

# envsubst is already in the nginx image — no extra package needed.
# Only the listed variables are substituted, so a stray ${...} in the template
# is left alone rather than silently blanked.
export API_BASE_URL
envsubst '${API_BASE_URL}' > /usr/share/nginx/html/env.js <<'TEMPLATE'
// GENERATED AT CONTAINER START — do not edit.
window.__env = {
  "API_BASE_URL": "${API_BASE_URL}"
};
TEMPLATE

echo "40-env.sh: wrote env.js with API_BASE_URL=${API_BASE_URL}"
