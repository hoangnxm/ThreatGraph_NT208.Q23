FROM node:20-slim as build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
RUN echo 'server { \
    listen 80; \
    \
    # Cấu hình chung cho mọi file \
    location / { \
        root /usr/share/nginx/html; \
        index index.html; \
        try_files $uri $uri/ /index.html; \
    } \
    \
    # Ép trình duyệt KHÔNG BAO GIỜ cache file index.html \
    location = /index.html { \
        root /usr/share/nginx/html; \
        add_header Cache-Control "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0"; \
        add_header Pragma "no-cache"; \
        add_header Expires 0; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
