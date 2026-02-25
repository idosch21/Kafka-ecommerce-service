using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OrderService.Models;

namespace OrderService.Services
{
    public class OrderRepository
    {
        private readonly ConcurrentDictionary<string, Order> _orders = new ConcurrentDictionary<string, Order>();
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(ILogger<OrderRepository> logger)
        {
            _logger = logger;
        }

        public void AddOrUpdateOrder(Order order)
        {
            
            order.ShippingCost = 0.02m * order.TotalAmount;
            _orders[order.OrderId] = order;

            if (order.Status == "new")
            {
                _logger.LogInformation($"Order {order.OrderId} added to repository (created).");
            }
            else
            {
                _logger.LogInformation($"Order {order.OrderId} moved to updated repository.");
            }
        }

        public Order FetchOrderDetails(string orderId)
        {
            string cleanedOrderId = StringHelper.RemoveSpaces(orderId);

            if (string.IsNullOrWhiteSpace(cleanedOrderId))
            {
                _logger.LogWarning("FetchOrderDetails was called with an empty orderId.");
                return null;
            }

            if (_orders.TryGetValue(cleanedOrderId, out Order order))
            {
                return order;
            }

            _logger.LogWarning($"No order found with ID: {cleanedOrderId}");
            return null;
        }
        public List<Order> GetAllOrderIdentifiers(string topicName)
        {
            List<string> orderIds;
            List<Order> orders = new List<Order>();

            if (topicName == "order-created-topic")
            {
                orderIds = _orders.Where(x => x.Value.Status.ToLower() == "new")
                                  .Select(x => x.Key)
                                  .ToList();

                if (!orderIds.Any())
                {
                    _logger.LogWarning("No new orders found in this topic - (created).");
                    return new List<Order>(); // Return empty list instead of null
                }

                _logger.LogInformation($"Retrieved {orderIds.Count} order IDs from repository (created).");
            }
            else if (topicName == "order-updated-topic")
            {
                orderIds = _orders.Where(x => x.Value.Status.ToLower() != "new")
                                  .Select(x => x.Key)
                                  .ToList();

                if (!orderIds.Any())
                {
                    _logger.LogWarning("No updated orders found in this topic.");
                    return new List<Order>(); // Return empty list instead of null
                }

                _logger.LogInformation($"Retrieved {orderIds.Count} order IDs from repository (updated).");
            }
            else
            {
                _logger.LogWarning($"Invalid topic name provided to GetAllOrderIdentifiers: {topicName}");
                return new List<Order>(); // Return empty list instead of null
            }

            foreach (var orderId in orderIds)
            {
                var order = FetchOrderDetails(orderId);
                if (order != null)
                {
                    orders.Add(order);
                }
                else
                {
                    _logger.LogWarning($"Order with ID {orderId} not found in repository.");
                }
            }

            return orders;
        }

        public static class StringHelper
        {
            public static string RemoveSpaces(string input)
            {
                return string.IsNullOrEmpty(input) ? input : new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
            }
        }
    }
}
