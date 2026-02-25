using Microsoft.AspNetCore.Mvc;
using OrderService.Services;
using Microsoft.Extensions.Logging;
using OrderService.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderRepository _orderRepository;
        private readonly ILogger<OrderController> _logger;
        private readonly KafkaConfig _kafkaConfig;

        public OrderController(OrderRepository orderRepository, ILogger<OrderController> logger, KafkaConfig kafkaConfig)
        {
            _orderRepository = orderRepository;
            _logger = logger;
            _kafkaConfig = kafkaConfig;
        }

        [HttpGet("getAllOrderIdsFromTopic")]
        public IActionResult GetAllOrdersFromTopic(string topicName)
        {
            try
            {
                _logger.LogInformation($"Received request to get all orders from topic: {topicName}");

                if (topicName != _kafkaConfig.OrderCreatedTopic && topicName != _kafkaConfig.OrderUpdatedTopic)
                {
                    _logger.LogWarning($"Invalid topic name provided: {topicName}");
                    return BadRequest("Invalid topic name.");
                }

                var orders = _orderRepository.GetAllOrderIdentifiers(topicName);

                if (orders == null || !orders.Any())
                {
                    _logger.LogWarning($"No orders found in topic: {topicName}");
                    return NotFound("No orders found in the specified topic.");
                }

                int orderCount = orders.Count();
                _logger.LogInformation($"Retrieved {orderCount} orders from topic: {topicName}");

                var response = new
                {
                    Message = $"Total orders in topic '{topicName}': {orderCount}",
                    Orders = orders
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving orders from topic {topicName}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving orders.");
            }
        }

        [HttpGet("order-details")]
        public IActionResult GetOrderDetails(string orderId)
        {
            try
            {
                _logger.LogInformation($"Received request to get order details for ID: {orderId}");

                var order = _orderRepository.FetchOrderDetails(orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order with ID {orderId} not found.");
                    return NotFound($"Order with ID {orderId} not found.");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching order details for ID {orderId}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving order details.");
            }
        }
    }
}
