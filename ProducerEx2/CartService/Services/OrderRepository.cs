using System.Collections.Concurrent;

namespace CartService.Services
{
    public class OrderRepository
    {
        private readonly ConcurrentDictionary<string, Order> _orders = new ConcurrentDictionary<string, Order>();
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(ILogger<OrderRepository> logger)
        {
            _logger = logger;
        }

        public bool AddOrder(Order order)
        {
            var result = _orders.TryAdd(order.OrderId, order);
            if (result)
            {
                _logger.LogInformation($"Order {order.OrderId} added to repository");
            }
            else
            {
                _logger.LogWarning($"Failed to add order {order.OrderId} to repository. It may already exist.");
            }
            return result;
        }

        public bool UpdateOrder(string orderId, string status)
        {
            if (_orders.TryGetValue(orderId, out var order))
            {
                order.Status = status;
                _logger.LogInformation($"Order {orderId} updated with status: {status}");
                return true;
            }
            _logger.LogWarning($"Order {orderId} not found for update");
            return false;
        }

        public Order GetOrder(string orderId)
        {
            if (_orders.TryGetValue(orderId, out var order))
            {
                _logger.LogInformation($"Retrieved order {orderId} from repository");
                return order;
            }
            _logger.LogWarning($"Order {orderId} not found in repository");
            return null;
        }
    }
}
