🛒 Microservices Order System (Kafka + .NET 8)
A distributed order management system utilizing a Producer-Consumer architecture. This project demonstrates event-driven communication between microservices using Apache Kafka and Docker Compose.

🏗 System Architecture
Producer (CartService): Entry point for order creation and status updates. It persists data locally and publishes events to Kafka.

Consumer (OrderService): Listens for Kafka events to sync state and provides global order lookup capabilities.

Message Broker: Apache Kafka (managed via Confluentinc images).

🚀 Getting Started
Prerequisites
Docker Desktop

Postman (for testing)

Deployment
Run the following command in the root directory to build and start the entire cluster:

Bash
docker-compose up --build
🛠 API Reference & Testing (Postman)
1. Create a New Order
Producer (CartService) - Port 5000

Method: POST

URL: http://localhost:5000/Order/create-order

Body (JSON):

JSON
{
  "OrderId": "test-order-101",
  "ItemsNum": 3
}
2. Update Order Status
Producer (CartService) - Port 5000

Method: PUT

URL: http://localhost:5000/Order/update-order

Body (JSON):

JSON
{
  "orderId": "test-order-101",
  "status": "Shipped"
}
Note: You can set the status to any custom string.

3. Get Order Details
Consumer (OrderService) - Port 7000

Method: GET

URL: http://localhost:7000/order/order-details?orderId=test-order-101

4. Bulk Topic Audit
Consumer (OrderService) - Port 7000

Method: GET

URL: http://localhost:7000/order/getAllOrderIdsFromTopic?topicName=order-updated-topic

Supported Topics: order-created-topic, order-updated-topic

📡 Kafka Implementation Details
Topic Strategy
order-created-topic: Dedicated to initial order events.

order-updated-topic: Dedicated to state changes.

Message Keying: All messages use OrderId as the key. This guarantees that all events for a specific order land in the same partition, ensuring strict chronological processing order.

Performance Observation (Initial Lag)
During testing, a delay (30s to 3m) was observed during the first update event.

Technical Root Cause: This is attributed to the initial Kafka Consumer Group Rebalance and metadata synchronization that occurs when a new topic is dynamically created.

Result: Subsequent updates are processed instantaneously once the consumer group stabilizes and the partitions are assigned.