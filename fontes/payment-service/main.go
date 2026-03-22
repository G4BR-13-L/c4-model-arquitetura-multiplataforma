package main

import (
	"database/sql"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"strconv"
	"time"

	"github.com/google/uuid"
	_ "github.com/lib/pq"
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

	payment := Payment{
		ID:            uuid.NewString(),
		RentalID:      dto.RentalID,
		Amount:        dto.Amount,
		PaymentMethod: dto.PaymentMethod,
		Status:        "PENDING",
		CheckoutURL:   "https://puc.pay.me",
		CreatedAt:     time.Now(),
	}

	_, err := db.Exec(
		`INSERT INTO payments (id, rental_id, amount, payment_method, status, checkout_url, created_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7)`,
		payment.ID, payment.RentalID, payment.Amount, payment.PaymentMethod,
		payment.Status, payment.CheckoutURL, payment.CreatedAt,
	)
	if err != nil {
		respondInternalError(w, "001I004", err)
		return
	}

	w.Header().Set(contentTypeHeader, contentTypeJSON)
	w.WriteHeader(http.StatusCreated)
	json.NewEncoder(w).Encode(payment)
}

func main() {
	db = connectDB()
	defer db.Close()

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

	http.Handle("GET /payments", http.HandlerFunc(listPaymentsHandler))
	http.Handle("POST /payments", http.HandlerFunc(createPaymentHandler))

	logger.Info("Server started", "service", "payment-api", "port", 3005)
	if err := http.ListenAndServe(":3005", nil); err != nil {
		logger.Error("Server bootstrap error", "error", err)
		os.Exit(1)
	}
}
