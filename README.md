# Distributed Class Manager Platform
> **Academic Capstone Project** | Software Engineering Fundamentals (Fundamentos de Ingenieria de Software)  
> **Universidad ORT Uruguay** | Semester: August 2024

A distributed system designed to manage users and online classes, demonstrating the evolution from a monolithic TCP server to a microservices-inspired architecture.

## Tech Stack:

- Core: C# .NET 8 / .NET 9

- Communication: TCP Sockets (Legacy Clients), gRPC (Inter-service Auth), REST API (Logs Querying).

- Messaging: RabbitMQ (Asynchronous Logging).

- Integrations: HTTP Webhooks for real-time external notifications.

- Infrastructure: Docker & Docker Compose (Multi-stage builds).

## Key Features:

- Async log processing using a Producer/Consumer pattern with RabbitMQ.

- Secure service-to-service validation using gRPC.

- Automated class notifications via external Webhooks.

- RESTful API for filtering and monitoring system logs.
