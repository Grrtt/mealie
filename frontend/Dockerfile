# Stage 1: Build the Nuxt SPA
FROM node:24-alpine AS builder

WORKDIR /app

# Copy package files and install deps
COPY package.json yarn.lock ./
RUN yarn install \
    --prefer-offline \
    --frozen-lockfile \
    --non-interactive \
    --production=false \
    --network-timeout 1000000

# Copy source and generate static output
COPY . .
RUN yarn generate

# Stage 2: Serve with nginx
FROM nginx:1.27-alpine AS runtime

# Copy static output from builder
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -qO- http://127.0.0.1/favicon.ico || exit 1
