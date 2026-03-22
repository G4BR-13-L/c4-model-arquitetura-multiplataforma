#!/bin/bash

minikube delete -p multiplatform
minikube start -p multiplatform

eval $(minikube -p multiplatform docker-env --unset)
docker build -t payment-api:1.0.0 ../payment-service

docker save payment-api:1.0.0 | (eval $(minikube -p multiplatform docker-env) && docker load)

kubectl apply -f ../devops/k8s/payment-service/
kubectl apply -f ../devops/k8s/payment-postgres/

echo "cluster ip: $(minikube ip -p multiplatform)" 