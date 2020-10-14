# Deploying The Service to Cluster

The configuration files in this folder enable deployment of the service to a local Kubernetes cluster.

## ConfigMap

`kubectl apply -f .\mvp-icap-service-configmap.yml`

## Deployment

`kubectl delete -f mvp-icap-service-deployment.yml`

## Service

The service is configured as a `NodePort` type enabling the port to be accessible out of the cluster.

`kubectl apply -f mvp-icap-service-service.yml`

Using `kubectl` the public port of the service can be reviewed. In the example below port 30850 can be used to access the service on `localhost`.
```
> kubectl get service icap-service

NAME           TYPE       CLUSTER-IP      EXTERNAL-IP   PORT(S)          AGE
icap-service   NodePort   10.101.232.62   <none>        1344:30850/TCP   5h40m
```