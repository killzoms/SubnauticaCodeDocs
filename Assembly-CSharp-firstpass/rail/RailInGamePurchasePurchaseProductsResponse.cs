using System.Collections.Generic;

namespace rail
{
    public class RailInGamePurchasePurchaseProductsResponse : EventBase
    {
        public string order_id;

        public List<RailProductItem> deliveried_products = new List<RailProductItem>();
    }
}
