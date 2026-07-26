# --- build ---
FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json .npmrc* ./
# --legacy-peer-deps : le projet est en Vite 8 alors que vite-plugin-pwa déclare
# un peer Vite <= 7 (compatible en pratique). À retirer quand pwa supportera Vite 8.
RUN npm ci --legacy-peer-deps
COPY . .
RUN npm run build

# --- runtime (nginx sert le SPA + proxy /api) ---
FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
