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

🛠 API Reference & Testing (Postman)
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

3. Get Order Details
Consumer (OrderService) - Port 7000

Method: GET

URL: http://localhost:7000/order/order-details?orderId=test

📡 Kafka Implementation Details
Topic Strategy
order-created-topic: Used for publishing new order events.

order-updated-topic: Used for publishing events when an order status changes.

Message Keying: All messages use OrderId as the key. This ensures chronological sequence per order.

Performance Observation (Initial Lag)
During testing, a delay (30s to 3m) was observed during the first update event.

Technical Root Cause: This is attributed to the initial Kafka Consumer Group Rebalance occurring when a new topic is dynamically created.

Result: Subsequent updates are processed instantaneously once the group stabilizes.

🛡️ Error Handling & Resilience
1. Kafka Connection & Availability
Producer/Consumer: Implemented a retry mechanism with exponential backoff.

Startup Delays: The producer starts 30s after deployment and the consumer 37s after deployment to ensure the Kafka Broker is fully operational.

2. Message Processing Failures
Idempotency: The consumer is designed to be idempotent.

Manual Offset Control: Offsets are committed only after successful processing to prevent data loss.

📄 Program Names
Producer: CartService

Consumer: OrderService
