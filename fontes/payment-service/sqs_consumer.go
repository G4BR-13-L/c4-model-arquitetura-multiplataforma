package main

import (
	"context"
	"time"

	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/config"
	"github.com/aws/aws-sdk-go-v2/credentials"
	"github.com/aws/aws-sdk-go-v2/service/sqs"
)

func startRentalCreatedConsumer(ctx context.Context) {
	queueName := getEnv("RENTAL_CREATED_QUEUE_NAME", "rental_created.fifo")
	endpoint := getEnv("SQS_ENDPOINT", "http://localstack:4566")
	region := getEnv("AWS_REGION", "us-east-1")
	accessKey := getEnv("AWS_ACCESS_KEY_ID", "default_access_key")
	secretKey := getEnv("AWS_SECRET_ACCESS_KEY", "default_secret_key")

	cfg, err := config.LoadDefaultConfig(ctx,
		config.WithRegion(region),
		config.WithCredentialsProvider(credentials.NewStaticCredentialsProvider(accessKey, secretKey, "")),
		config.WithBaseEndpoint(endpoint),
	)
	if err != nil {
		logger.Error("sqs: failed to load config", "error", err)
		return
	}

	client := sqs.NewFromConfig(cfg)

	var urlOut *sqs.GetQueueUrlOutput
	for {
		var err error
		urlOut, err = client.GetQueueUrl(ctx, &sqs.GetQueueUrlInput{QueueName: &queueName})
		if err == nil {
			break
		}
		logger.Warn("sqs: queue not ready, retrying in 5s", "queue", queueName, "error", err)
		select {
		case <-ctx.Done():
			return
		case <-time.After(5 * time.Second):
		}
	}

	queueURL := aws.ToString(urlOut.QueueUrl)
	logger.Info("sqs: consumer started", "queue", queueName)

	for {
		select {
		case <-ctx.Done():
			logger.Info("sqs: consumer stopped", "queue", queueName)
			return
		default:
		}

		out, err := client.ReceiveMessage(ctx, &sqs.ReceiveMessageInput{
			QueueUrl:            &queueURL,
			MaxNumberOfMessages: 10,
			WaitTimeSeconds:     20,
		})
		if err != nil {
			logger.Error("sqs: failed to receive messages", "queue", queueName, "error", err)
			continue
		}

		for _, msg := range out.Messages {
			logger.Info("sqs: message received", "message_id", aws.ToString(msg.MessageId))

			_, err := client.DeleteMessage(ctx, &sqs.DeleteMessageInput{
				QueueUrl:      &queueURL,
				ReceiptHandle: msg.ReceiptHandle,
			})
			if err != nil {
				logger.Error("sqs: failed to delete message", "message_id", aws.ToString(msg.MessageId), "error", err)
			}
		}
	}
}
