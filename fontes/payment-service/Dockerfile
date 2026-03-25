FROM golang:1.25-alpine AS builder
WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY *.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -o api .

FROM scratch
WORKDIR /app
COPY --from=builder /app/api .
EXPOSE 3005
CMD ["./api"]
