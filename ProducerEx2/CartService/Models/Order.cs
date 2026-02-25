using System.Collections.Generic;

namespace CartService
{
    public class Order
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public string OrderDate { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
