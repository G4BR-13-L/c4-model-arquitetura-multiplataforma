FROM node:22-alpine AS base
WORKDIR /app
EXPOSE 8080

COPY package.json package-lock.json ./
RUN npm ci --omit=dev

COPY src ./src
COPY README.md ./README.md

ENV NODE_ENV=production
CMD ["npm", "start"]
