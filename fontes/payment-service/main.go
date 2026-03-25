package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"
	"math/rand"
	"net/http"
	"os"
	"strconv"
	"time"

	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/credentials"
	"github.com/aws/aws-sdk-go-v2/service/sqs"
	"github.com/google/uuid"
	_ "github.com/lib/pq"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
)

const contentTypeHeader = "Content-Type"
const contentTypeJSON = "application/json"
const msgValidationError = "Validation error, check fields and try again"

type HttpEror struct {
	Code          string `json:"code"`
	Message       string `json:"message"`
	PrettyMessage string `json:"pretty_message"`
}

func respondError(w http.ResponseWriter, status int, code, message, prettyMessage string, err error) {
	logger.Warn(message, "code", code, "error", err.Error())
	w.Header().Set(contentTypeHeader, contentTypeJSON)
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(HttpEror{
		Code:          code,
		Message:       message,
		PrettyMessage: prettyMessage,
	})
}

func respondInternalError(w http.ResponseWriter, code string, err error) {
	logger.Error("Internal server error", "code", code, "error", err.Error())
	w.Header().Set(contentTypeHeader, contentTypeJSON)
	w.WriteHeader(http.StatusInternalServerError)
	json.NewEncoder(w).Encode(HttpEror{
		Code:          code,
		Message:       "Internal server error",
		PrettyMessage: "Erro interno, tente novamente ou contate o suporte",
	})
}

func respondValidationError(w http.ResponseWriter, code string, err error) {
	respondError(w, http.StatusBadRequest, code,
		msgValidationError,
		"Erro durante o pagamento, tente novamente ou contate o suporte",
		err,
	)
}

type Payment struct {
	ID            string    `json:"id"`
	RentalID      string    `json:"rental_id"`
	Amount        int       `json:"amount"`
	PaymentMethod string    `json:"payment_method"`
	Status        string    `json:"status"`
	CheckoutURL   string    `json:"checkout_url"`
	CreatedAt     time.Time `json:"created_at"`
}

var validPaymentMethods = map[string]bool{
	"CREDIT": true,
	"DEBIT":  true,
	"PIX":    true,
}

type CreatePaymentDTO struct {
	RentalID      string `json:"rental_id"`
	Amount        int    `json:"amount"`
	PaymentMethod string `json:"payment_method"`
}

func (dto CreatePaymentDTO) Validate() error {
	if _, err := uuid.Parse(dto.RentalID); err != nil {
		return errors.New("rental_id must be a valid UUID")
	}
	if dto.Amount <= 0 {
		return errors.New("amount must be greater than zero")
	}
	if !validPaymentMethods[dto.PaymentMethod] {
		return errors.New("payment_method must be one of: CREDIT, DEBIT, PIX")
	}
	return nil
}

var logger = slog.New(slog.NewJSONHandler(os.Stdout, nil))

var db *sql.DB
var sqsClient *sqs.Client
var paymentConfirmedQueueURL *string

func connectDB() *sql.DB {
	dsn := fmt.Sprintf(
		"host=%s port=%s user=%s password=%s dbname=%s sslmode=disable",
		getEnv("DB_HOST", "payment-postgres"),
		getEnv("DB_PORT", "5432"),
		getEnv("DB_USER", "postgres"),
		getEnv("DB_PASSWORD", "postgres"),
		getEnv("DB_NAME", "payment"),
	)

	conn, err := sql.Open("postgres", dsn)
	if err != nil {
		logger.Error("Failed to open database", "error", err)
		os.Exit(1)
	}

	if err := conn.Ping(); err != nil {
		logger.Error("Failed to connect to database", "error", err)
		os.Exit(1)
	}

	return conn
}

func getEnv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

type PaginatedResponse struct {
	Data  []Payment `json:"data"`
	Page  int       `json:"page"`
	Size  int       `json:"size"`
	Total int       `json:"total"`
}

type Rental struct {
	ID            string    `json:"id"`
	VehicleID     string    `json:"vehicle_id"`
	UserID        string    `json:"user_id"`
	StartDate     time.Time `json:"start_date"`
	EndDate       time.Time `json:"end_date"`
	TotalAmount   float64   `json:"total_amount"`
	PaymentStatus string    `json:"payment_status"`
	Status        string    `json:"status"`
}

type RentalPaginatedResponse struct {
	Data  []Rental `json:"data"`
	Page  int      `json:"page"`
	Size  int      `json:"size"`
	Total int      `json:"total"`
}

func listPaymentsHandler(w http.ResponseWriter, r *http.Request) {
	page := 1
	size := 30

	if raw := r.URL.Query().Get("page"); raw != "" {
		v, err := strconv.Atoi(raw)
		if err != nil || v < 1 {
			respondError(w, http.StatusBadRequest, "001C003",
				msgValidationError,
				"Erro durante exibição da lista, tente novamente ou contate o suporte",
				errors.New("page must be a positive integer"),
			)
			return
		}
		page = v
	}

	if raw := r.URL.Query().Get("size"); raw != "" {
		v, err := strconv.Atoi(raw)
		if err != nil || v < 1 || v > 100 {
			respondError(w, http.StatusBadRequest, "001C004",
				msgValidationError,
				"Erro durante exibição da lista, tente novamente ou contate o suporte",
				errors.New("size must be an integer between 1 and 100"),
			)
			return
		}
		size = v
	}

	offset := (page - 1) * size

	var total int
	if err := db.QueryRow(`SELECT COUNT(*) FROM payments`).Scan(&total); err != nil {
		respondInternalError(w, "001I001", err)
		return
	}

	rows, err := db.Query(
		`SELECT id, rental_id, amount, payment_method, status, checkout_url, created_at
		 FROM payments ORDER BY created_at DESC LIMIT $1 OFFSET $2`,
		size, offset,
	)
	if err != nil {
		respondInternalError(w, "001I002", err)
		return
	}
	defer rows.Close()

	payments := []Payment{}
	for rows.Next() {
		var p Payment
		if err := rows.Scan(&p.ID, &p.RentalID, &p.Amount, &p.PaymentMethod, &p.Status, &p.CheckoutURL, &p.CreatedAt); err != nil {
			respondInternalError(w, "001I003", err)
			return
		}
		payments = append(payments, p)
	}

	w.Header().Set(contentTypeHeader, contentTypeJSON)
	json.NewEncoder(w).Encode(PaginatedResponse{
		Data:  payments,
		Page:  page,
		Size:  size,
		Total: total,
	})
}

func createPaymentHandler(w http.ResponseWriter, r *http.Request) {
	var dto CreatePaymentDTO

	if err := json.NewDecoder(r.Body).Decode(&dto); err != nil {
		respondValidationError(w, "001C001", err)
		return
	}

	if err := dto.Validate(); err != nil {
		respondValidationError(w, "001C002", err)
		return
	}

	var exists bool
	if err := db.QueryRow(`SELECT EXISTS(SELECT 1 FROM rentals WHERE id = $1)`, dto.RentalID).Scan(&exists); err != nil {
		respondInternalError(w, "001I005", err)
		return
	}
	if !exists {
		respondError(w, http.StatusUnprocessableEntity, "001C005",
			"rental_id not found",
			"Locação não encontrada, verifique o ID e tente novamente",
			fmt.Errorf("rental %s not found", dto.RentalID),
		)
		return
	}

	ctx := r.Context()
	tracer := otel.Tracer("payment-service")

	finalStatus := "CONFIRMED"
	if rand.Intn(2) == 0 {
		finalStatus = "FAILED"
	}

	payment := Payment{
		ID:            uuid.NewString(),
		RentalID:      dto.RentalID,
		Amount:        dto.Amount,
		PaymentMethod: dto.PaymentMethod,
		Status:        finalStatus,
		CheckoutURL:   "https://puc.pay.me",
		CreatedAt:     time.Now(),
	}

	_, dbSpan := tracer.Start(ctx, "db.insert-payment")
	dbSpan.SetAttributes(
		attribute.String("payment.id", payment.ID),
		attribute.String("payment.status", finalStatus),
		attribute.String("payment.method", payment.PaymentMethod),
	)
	_, err := db.ExecContext(ctx,
		`INSERT INTO payments (id, rental_id, amount, payment_method, status, checkout_url, created_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7)`,
		payment.ID, payment.RentalID, payment.Amount, payment.PaymentMethod,
		payment.Status, payment.CheckoutURL, payment.CreatedAt,
	)
	if err != nil {
		dbSpan.RecordError(err)
		dbSpan.SetStatus(codes.Error, err.Error())
		dbSpan.End()
		respondInternalError(w, "001I004", err)
		return
	}
	dbSpan.End()

	type paymentConfirmedData struct {
		PaymentID string `json:"payment_id"`
		RentalID  string `json:"rental_id"`
		Status    string `json:"status"`
	}
	type paymentConfirmedEvent struct {
		EventType  string               `json:"event_type"`
		OccurredAt string               `json:"occurred_at"`
		Data       paymentConfirmedData `json:"data"`
	}
	eventBody, _ := json.Marshal(paymentConfirmedEvent{
		EventType:  "payment.confirmed",
		OccurredAt: time.Now().UTC().Format(time.RFC3339),
		Data: paymentConfirmedData{
			PaymentID: payment.ID,
			RentalID:  payment.RentalID,
			Status:    finalStatus,
		},
	})

	_, sqsSpan := tracer.Start(ctx, "sqs.publish-payment-confirmed")
	sqsSpan.SetAttributes(attribute.String("messaging.destination", "payment_confirmed_fifo"))
	_, sqsErr := sqsClient.SendMessage(ctx, &sqs.SendMessageInput{
		QueueUrl:    paymentConfirmedQueueURL,
		MessageBody: aws.String(string(eventBody)),
	})
	if sqsErr != nil {
		sqsSpan.RecordError(sqsErr)
		sqsSpan.SetStatus(codes.Error, sqsErr.Error())
		logger.Error("Failed to publish payment_confirmed event", "payment_id", payment.ID, "error", sqsErr)
	} else {
		logger.Info("Payment confirmed", "payment_id", payment.ID, "rental_id", payment.RentalID, "payload", string(eventBody))
	}
	sqsSpan.End()

	w.Header().Set(contentTypeHeader, contentTypeJSON)
	w.WriteHeader(http.StatusCreated)
	json.NewEncoder(w).Encode(payment)
}

func listRentalsHandler(w http.ResponseWriter, r *http.Request) {
	page, size := 1, 30

	if raw := r.URL.Query().Get("page"); raw != "" {
		v, err := strconv.Atoi(raw)
		if err != nil || v < 1 {
			respondError(w, http.StatusBadRequest, "002C001", msgValidationError,
				"Erro durante exibição da lista, tente novamente ou contate o suporte",
				errors.New("page must be a positive integer"))
			return
		}
		page = v
	}
	if raw := r.URL.Query().Get("size"); raw != "" {
		v, err := strconv.Atoi(raw)
		if err != nil || v < 1 || v > 100 {
			respondError(w, http.StatusBadRequest, "002C002", msgValidationError,
				"Erro durante exibição da lista, tente novamente ou contate o suporte",
				errors.New("size must be between 1 and 100"))
			return
		}
		size = v
	}

	offset := (page - 1) * size

	var total int
	if err := db.QueryRow(`SELECT COUNT(*) FROM rentals`).Scan(&total); err != nil {
		respondInternalError(w, "002I001", err)
		return
	}

	rows, err := db.Query(
		`SELECT id, vehicle_id, user_id, start_date, end_date, total_amount, payment_status, status
		 FROM rentals ORDER BY start_date DESC LIMIT $1 OFFSET $2`,
		size, offset,
	)
	if err != nil {
		respondInternalError(w, "002I002", err)
		return
	}
	defer rows.Close()

	rentals := []Rental{}
	for rows.Next() {
		var rental Rental
		if err := rows.Scan(&rental.ID, &rental.VehicleID, &rental.UserID,
			&rental.StartDate, &rental.EndDate, &rental.TotalAmount,
			&rental.PaymentStatus, &rental.Status); err != nil {
			respondInternalError(w, "002I003", err)
			return
		}
		rentals = append(rentals, rental)
	}

	w.Header().Set(contentTypeHeader, contentTypeJSON)
	json.NewEncoder(w).Encode(RentalPaginatedResponse{Data: rentals, Page: page, Size: size, Total: total})
}

func resolveQueueURL(queueName string) *string {
	for {
		result, err := sqsClient.GetQueueUrl(context.Background(), &sqs.GetQueueUrlInput{
			QueueName: aws.String(queueName),
		})
		if err != nil {
			logger.Warn("SQS: queue not ready, retrying in 5s", "queue", queueName, "error", err)
			time.Sleep(5 * time.Second)
			continue
		}
		return result.QueueUrl
	}
}

func initSQS() {
	endpoint := getEnv("SQS_ENDPOINT", "http://localhost:4566")
	region := getEnv("AWS_REGION", "us-east-1")

	cfg, err := config.LoadDefaultConfig(context.Background(),
		config.WithRegion(region),
		config.WithCredentialsProvider(credentials.NewStaticCredentialsProvider(
			getEnv("AWS_ACCESS_KEY_ID", "test"),
			getEnv("AWS_SECRET_ACCESS_KEY", "test"),
			"",
		)),
	)
	if err != nil {
		logger.Error("SQS: failed to load AWS config", "error", err)
		os.Exit(1)
	}

	sqsClient = sqs.NewFromConfig(cfg, func(o *sqs.Options) {
		o.BaseEndpoint = aws.String(endpoint)
	})

	paymentConfirmedQueueURL = resolveQueueURL("payment_confirmed_fifo")
	logger.Info("SQS producer ready", "queue", "payment_confirmed_fifo")
}

func startSQSConsumer() {
	queueName := getEnv("SQS_QUEUE_NAME", "rental_created_fifo")
	queueURL := resolveQueueURL(queueName)

	logger.Info("SQS consumer started", "queue", queueName)

	for {
		result, err := sqsClient.ReceiveMessage(context.Background(), &sqs.ReceiveMessageInput{
			QueueUrl:            queueURL,
			MaxNumberOfMessages: 10,
			WaitTimeSeconds:     5,
		})
		if err != nil {
			logger.Error("SQS consumer: failed to receive messages", "error", err)
			time.Sleep(5 * time.Second)
			continue
		}

		for _, msg := range result.Messages {
			msgID := aws.ToString(msg.MessageId)
			logger.Info("SQS message received", "message_id", msgID)

			var envelope struct {
				EventType  string `json:"event_type"`
				OccurredAt string `json:"occurred_at"`
				Data       Rental `json:"data"`
			}
			if err := json.Unmarshal([]byte(aws.ToString(msg.Body)), &envelope); err != nil {
				logger.Error("SQS consumer: failed to parse message", "message_id", msgID, "error", err)
			} else {
				_, err := db.Exec(
					`INSERT INTO rentals (id, vehicle_id, user_id, start_date, end_date, total_amount, payment_status, status)
					 VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
					 ON CONFLICT (id) DO NOTHING`,
					envelope.Data.ID, envelope.Data.VehicleID, envelope.Data.UserID,
					envelope.Data.StartDate, envelope.Data.EndDate, envelope.Data.TotalAmount,
					envelope.Data.PaymentStatus, envelope.Data.Status,
				)
				if err != nil {
					logger.Error("SQS consumer: failed to save rental", "message_id", msgID, "error", err)
				} else {
					logger.Info("SQS consumer: rental saved", "rental_id", envelope.Data.ID)
				}
			}

			_, err := sqsClient.DeleteMessage(context.Background(), &sqs.DeleteMessageInput{
				QueueUrl:      queueURL,
				ReceiptHandle: msg.ReceiptHandle,
			})
			if err != nil {
				logger.Error("SQS consumer: failed to delete message", "message_id", msgID, "error", err)
			}
		}
	}
}

func main() {
	shutdown := initTracer()
	defer shutdown(context.Background())

	db = connectDB()
	defer db.Close()

	initSQS()
	go startSQSConsumer()

	_, err := db.Exec(`
		CREATE TABLE IF NOT EXISTS payments (
			id             TEXT PRIMARY KEY,
			rental_id      TEXT NOT NULL,
			amount         INTEGER NOT NULL,
			payment_method TEXT NOT NULL,
			status         TEXT NOT NULL,
			checkout_url   TEXT NOT NULL,
			created_at     TIMESTAMPTZ NOT NULL
		)
	`)
	if err != nil {
		logger.Error("Failed to create payments table", "error", err)
		os.Exit(1)
	}

	_, err = db.Exec(`
		CREATE TABLE IF NOT EXISTS rentals (
			id             TEXT PRIMARY KEY,
			vehicle_id     TEXT NOT NULL,
			user_id        TEXT NOT NULL,
			start_date     TIMESTAMPTZ NOT NULL,
			end_date       TIMESTAMPTZ NOT NULL,
			total_amount   NUMERIC NOT NULL,
			payment_status TEXT NOT NULL,
			status         TEXT NOT NULL
		)
	`)
	if err != nil {
		logger.Error("Failed to create rentals table", "error", err)
		os.Exit(1)
	}

	http.Handle("GET /payments", otelhttp.NewHandler(http.HandlerFunc(listPaymentsHandler), "GET /payments"))
	http.Handle("POST /payments", otelhttp.NewHandler(http.HandlerFunc(createPaymentHandler), "POST /payments"))
	http.Handle("GET /rentals", otelhttp.NewHandler(http.HandlerFunc(listRentalsHandler), "GET /rentals"))

	logger.Info("Server started", "service", "payment-api", "port", 3005)
	if err := http.ListenAndServe(":3005", nil); err != nil {
		logger.Error("Server bootstrap error", "error", err)
		os.Exit(1)
	}
}
