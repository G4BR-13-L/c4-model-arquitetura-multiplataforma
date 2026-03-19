#!/bin/sh

ENDPOINT=http://localstack:4566
REGION=us-east-1

aws configure set aws_access_key_id default_access_key --profile=localstack
aws configure set aws_secret_access_key default_secret_key --profile=localstack
aws configure set region $REGION --profile=localstack
