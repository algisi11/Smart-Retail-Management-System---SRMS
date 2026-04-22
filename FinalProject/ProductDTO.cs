using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject
{
    public class ProductDTO
    {
        public int ProductID { get; set; }
        public string Barcode { get; set; }
        public string ProductName { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public int ReorderPoint { get; set; }
        public string ABC_Class { get; set; }
        public decimal SellingPrice { get; set; }
    }

}
