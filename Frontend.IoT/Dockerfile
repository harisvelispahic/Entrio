# syntax=docker/dockerfile:1

# =============================================================================
# Entrio web — multi-stage: Node BUILDS the app, nginx SERVES the result.
#
# There is no Node at runtime. `vite build` emits plain static .html/.js/.css,
# and the final image is just nginx handing those files to the browser.
# =============================================================================

# --- Build stage --------------------------------------------------------------
FROM node:22.22.0-alpine3.22 AS build
WORKDIR /app

# Manifests first so the (slow) install layer caches independently of source edits.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Skip the "prebuild" hook: it generates env.js from a repo-root .env that does
# not exist in the build context. In the container env.js is written at START-UP
# by 40-env.sh instead, which is the whole point — the API URL must not be baked
# into the bundle at build time.
RUN npm run build --ignore-scripts

# --- Runtime stage ------------------------------------------------------------
# nginx 1.30.5 on alpine — same tag already present locally from another project.
FROM nginx:1.30.5-alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

# The official nginx image runs every executable in /docker-entrypoint.d/ before
# starting nginx, which is where runtime config generation belongs.
COPY docker-entrypoint.d/40-env.sh /docker-entrypoint.d/40-env.sh
RUN chmod +x /docker-entrypoint.d/40-env.sh

EXPOSE 80
