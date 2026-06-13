FROM golang:1.26-alpine AS build
WORKDIR /src

COPY src/services/notifications/go.mod ./
RUN go mod download

COPY src/services/notifications/ ./
RUN CGO_ENABLED=0 GOOS=linux go build -o /app/notifications ./cmd/notifications

FROM scratch AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/notifications /notifications
ENTRYPOINT ["/notifications"]
