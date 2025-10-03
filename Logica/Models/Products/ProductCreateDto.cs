using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models.Products
{
    public class ProductCreateDto
    {
        public required string Title { get; set; }

        public decimal Price { get; set; }

        public required string Description { get; set; }

        public required string Category { get; set; }

        public required string Image { get; set; }

        public RatingDto Rating { get; set; } = new RatingDto { Rate = 0, Count = 0 };

        public InventoryDto Inventory { get; set; } = new InventoryDto { Total = 0, Available = 0 };
    }
}