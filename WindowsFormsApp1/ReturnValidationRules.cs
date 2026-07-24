using System;

namespace WindowsFormsApp1
{
    public static class ReturnValidationRules
    {
        public static void EnsureLinkedBillItemBelongsToBill(int originalBillId, int actualBillId)
        {
            if (originalBillId <= 0 || actualBillId <= 0 || originalBillId != actualBillId)
            {
                throw new InvalidOperationException("Returned item does not belong to the selected original bill.");
            }
        }

        public static void EnsureLinkedProductMatchesInventoryItem(int requestedProductId, int actualProductId)
        {
            if (requestedProductId <= 0 || actualProductId <= 0 || requestedProductId != actualProductId)
            {
                throw new InvalidOperationException("Returned item does not match the selected inventory product.");
            }
        }

        public static void EnsureReturnQuantityWithinSoldQuantity(decimal soldQuantity, decimal alreadyReturnedQuantity, decimal requestedQuantity)
        {
            if (requestedQuantity <= 0m || alreadyReturnedQuantity + requestedQuantity > soldQuantity)
            {
                throw new InvalidOperationException("Return quantity exceeds the remaining quantity from the original sale.");
            }
        }
    }
}
