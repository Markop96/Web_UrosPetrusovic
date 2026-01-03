using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace UrosPetrusovic.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ManufacturerPartNumber { get; set; }
        public byte[]? Slika { get; set; }
        public int LeadTime { get; set; }
        public decimal Price { get; set; }

        public string? Description { get; set; }
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public int CatalogId { get; set; }
        public Catalog? Catalog { get; set; }

        [NotMapped]
        public IFormFile? SlikaFile { get; set; }
    }
}
