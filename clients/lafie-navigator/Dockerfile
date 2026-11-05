# APP_ENV : 'production' (défaut) ou 'development'.
# Pilote à la fois le mode de build Vite (PWA désactivée en dev) et la conf nginx servie.

# --- build ---
FROM node:24-alpine AS build
ARG APP_ENV=production
WORKDIR /app
COPY package*.json .npmrc* ./
# --legacy-peer-deps : le projet est en Vite 8 alors que vite-plugin-pwa déclare
# un peer Vite <= 7 (compatible en pratique). À retirer quand pwa supportera Vite 8.
RUN npm ci --legacy-peer-deps
COPY . .
RUN npm run build -- --mode ${APP_ENV}

# --- runtime (nginx sert le SPA + proxy /api) ---
FROM nginx:alpine AS runtime
ARG APP_ENV=production
# Conf nginx selon l'environnement : nginx.development.conf / nginx.production.conf.
COPY nginx.${APP_ENV}.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
