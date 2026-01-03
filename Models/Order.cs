using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UrosPetrusovic.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public IdentityUser? User { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Na čekanju";

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}