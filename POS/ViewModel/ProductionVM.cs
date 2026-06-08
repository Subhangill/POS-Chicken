using POS.Models;
using System.Collections.Generic;

namespace POS.ViewModel
{
    public class ProductionVM
    {
        public Production production { get; set; }
        public List<Product> productList { get; set; }
        public List<RawDetail> rawDetails { get; set; }
        public List<FinishDetail> finishDetails { get; set; }

        public List<Product> RawProductId { get; set; } 
        public List<Product> FinishProductId { get; set; }

    }
}
