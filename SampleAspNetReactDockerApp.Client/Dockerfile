# Build stage
FROM node:latest AS build-step
WORKDIR /app
COPY SampleAspNetReactDockerApp.Client/package*.json ./
RUN npm install
COPY SampleAspNetReactDockerApp.Client/ .
RUN npm run build

# Runtime stage
FROM nginx:alpine
COPY --from=build-step /app/dist /usr/share/nginx/html
COPY SampleAspNetReactDockerApp.Client/default.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]