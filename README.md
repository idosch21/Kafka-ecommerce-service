# 🛒 Microservices Order System (Kafka + .NET 8)

A distributed order management system utilizing a **Producer-Consumer** architecture. This project demonstrates event-driven communication between microservices using **Apache Kafka** and **Docker Compose**.

---

## 🏗 System Architecture
* **Producer (CartService):** Entry point for order creation and status updates. It persists data locally and publishes events to Kafka.
* **Consumer (OrderService):** Listens for Kafka events to sync state and provides global order lookup capabilities.
* **Message Broker:** Apache Kafka (managed via Confluentinc images).

---

## 🚀 Getting Started

### Prerequisites
* Docker Desktop
* Postman (for testing)

### Deployment
Run the following command in the root directory to build and start the entire cluster:

```bash
docker-compose up --build
```

### 🛠 API Reference & Testing (Postman)
1. Create a New Order
Producer (CartService) - Port 5000

Method: POST
URL: http://localhost:5000/Order/create-order
Body (JSON):

JSON
{
  "OrderId": "test",
  "ItemsNum": 2
}

2. Update Order Status
Producer (CartService) - Port 5000
Method: PUT
URL: http://localhost:5000/Order/update-order
Body (JSON):

JSON
{
  "orderId": "test",
  "status": "updated"
}
Note: To update an existing order, simply change the status string to whatever you want it to be.

### 📡 Kafka Implementation Details
Topic Strategy
order-created-topic: Used for publishing new order events.

order-updated-topic: Used for publishing events when an order status changes.

Message Keying: All messages use OrderId as the key. This ensures that all events for the same order are processed in the correct sequence by the same consumer instance.

Performance Observation (Initial Lag)
During testing, a delay (30s to 3m) was observed during the first update event.

Technical Root Cause: This is attributed to the initial Kafka Consumer Group Rebalance and metadata synchronization that occurs when a new topic is dynamically created.

Result: Subsequent updates are processed instantaneously once the consumer group stabilizes.

### 🛡️ Error Handling & Resilience
1. Kafka Connection & Availability
Producer/Consumer: Implemented a retry mechanism with exponential backoff to ensure transient connection issues do not result in message loss.

Startup Delays: The producer starts 30s after deployment and the consumer 37s after deployment to ensure the Kafka Broker is fully operational.

2. Message Processing Failures
Idempotency: The consumer is designed to be idempotent; processing the same message multiple times will not lead to inconsistent data.

Manual Offset Control: Offsets are committed only after successful processing to prevent losing messages during a crash.

📄 Program Names

Producer: CartService

Consumer: OrderService
