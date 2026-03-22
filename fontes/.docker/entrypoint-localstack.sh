#!/bin/sh
set -eu

create_queue() {
  local queue_name="$1"

  if awslocal sqs get-queue-url --queue-name "$queue_name" >/dev/null 2>&1; then
    echo "Queue '$queue_name' already exists."
    return
  fi

  echo "Creating queue '$queue_name'..."
  awslocal sqs create-queue --queue-name "$queue_name" >/dev/null
}

create_queue "notification.email"
create_queue "rental.created"
create_queue "payment.confirmed"
