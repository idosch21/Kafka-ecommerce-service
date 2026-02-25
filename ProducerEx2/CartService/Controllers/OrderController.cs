using CartService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CartService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly KafkaProducerService _kafkaProducer;
        private readonly OrderRepository _orderRepository;
        private readonly ILogger<OrderController> _logger;

        public OrderController(KafkaProducerService kafkaProducer, OrderRepository orderRepository, ILogger<OrderController> logger)
        {
            _kafkaProducer = kafkaProducer;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            _logger.LogInformation($"Received create order request for OrderId: {request.OrderId}, ItemsNum: {request.ItemsNum}");

            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid create order request. " + string.Join(", ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                // Validate request data
                if (string.IsNullOrEmpty(request.OrderId) || request.ItemsNum <= 0)
                {
                    _logger.LogError("Invalid input fields. OrderId must not be null or empty and ItemsNum must be greater than 0.");
                    return BadRequest("Invalid input fields. OrderId must not be null or empty and ItemsNum must be greater than 0.");
                }
                Order order = GenerateRandomOrder(request.OrderId, request.ItemsNum);

                if (_orderRepository.AddOrder(order))
                {
                    bool success = await _kafkaProducer.ProduceOrderCreatedEvent(order.OrderId, order);
                    if (success)
                    {
                        _logger.LogInformation($"Order created successfully: {order.OrderId}");
                        return Ok("Order created successfully");
                    }
                    _logger.LogError($"Failed to produce Kafka event for order: {order.OrderId}");
                    return StatusCode(500, "Failed to create order");
                }
                _logger.LogWarning($"Order already exists: {order.OrderId}");
                return Conflict("Order already exists");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                return StatusCode(500, "An error occurred while creating the order");
            }
        }

        [HttpPut("update-order")]
        public async Task<IActionResult> UpdateOrder([FromBody] OrderUpdate orderUpdate)
        {
            _logger.LogInformation($"Received update order request for OrderId: {orderUpdate.OrderId}");
            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid update order request. " + string.Join(", ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                if (string.IsNullOrEmpty(orderUpdate.OrderId) || string.IsNullOrEmpty(orderUpdate.Status))
                {
                    _logger.LogError("Invalid input fields. OrderId and Status must not be null or empty.");
                    return BadRequest("Invalid input fields. OrderId and Status must not be null or empty.");
                }

                if (_orderRepository.GetOrder(orderUpdate.OrderId) == null)
                {
                    return NotFound($"Order with ID {orderUpdate.OrderId} not found");
                }
                if(_orderRepository.GetOrder(orderUpdate.OrderId).Status == orderUpdate.Status)
                {
                    _logger.LogWarning($"Order has the same status: {orderUpdate.OrderId}");
                    return StatusCode(500, "Order status is the same as entered.");
                }
                if (_orderRepository.UpdateOrder(orderUpdate.OrderId, orderUpdate.Status))
                {
                    var updatedOrder = _orderRepository.GetOrder(orderUpdate.OrderId);
                    bool success = await _kafkaProducer.ProduceOrderUpdatedEvent(orderUpdate.OrderId, updatedOrder);
                    if (success)
                    {
                        _logger.LogInformation($"Order updated successfully: {orderUpdate.OrderId}");
                        return Ok("Order updated successfully");
                    }
                    _logger.LogError($"Failed to produce Kafka event for order update: {orderUpdate.OrderId}");
                    return StatusCode(500, "Failed to update order");
                }
                _logger.LogWarning($"Order not found for update: {orderUpdate.OrderId}");
                return NotFound("Order not found");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the order");
            }
        }
        private Order GenerateRandomOrder(string orderId, int itemsNum)
        {
            var random = new Random();
            var items = Enumerable.Range(1, itemsNum)
                .Select(i => new OrderItem
                {
                    ProductId = $"Product{i}",
                    Quantity = random.Next(1, 5),
                    Price = random.Next(10, 100)
                }).ToList();
            return new Order
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid().ToString(),
                OrderDate = DateTime.UtcNow.ToString(),
                Items = items,
                TotalAmount = items.Sum(item => item.Quantity * item.Price),
                Currency = "USD",
                Status = "new"
            };
        }
    }

    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "OrderId is required")]
        public string OrderId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ItemsNum must be greater than 0")]
        public int ItemsNum { get; set; }
    }

    public class OrderUpdate
    {
        [Required(ErrorMessage = "OrderId is required")]
        public string OrderId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }
    }
}
