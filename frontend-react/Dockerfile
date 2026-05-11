FROM node:24-alpine AS builder

WORKDIR /app

ARG SUB_PATH=/
ENV SUB_PATH=${SUB_PATH}

COPY package.json yarn.lock ./
RUN yarn install \
    --prefer-offline \
    --frozen-lockfile \
    --non-interactive \
    --production=false \
    --network-timeout 1000000

COPY . .
RUN yarn build

FROM nginx:1.27-alpine AS runtime

COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget -qO- http://127.0.0.1/index.html || exit 1
