using System;

namespace WindowsFormsApp1
{
    public static class InventoryMutationRules
    {
        public const string Sale = "sale";
        public const string SaleVoid = "void_reversal";
        public const string CustomerReturnRestock = "customer_return_restock";
        public const string CustomerReturnNotRestocked = "customer_return_not_restocked";
        public const string PackagingConsumption = "packaging_consumption";
        public const string PackagingOutput = "packaging_output";
        public const string PurchaseReceipt = "purchase_receipt";
        public const string ManualAdjustmentIn = "manual_adjustment_in";
        public const string ManualAdjustmentOut = "manual_adjustment_out";
        public const string StocktakeCorrection = "stocktake_correction";

        public static decimal SaleDelta(decimal quantity)
        {
            EnsurePositive(quantity, "Sale quantity");
            return -quantity;
        }

        public static decimal VoidDelta(decimal originalSaleDelta)
        {
            if (originalSaleDelta >= 0m)
            {
                throw new InvalidOperationException("Original sale movement must be negative.");
            }

            return -originalSaleDelta;
        }

        public static decimal ReturnDelta(decimal quantity, bool restock)
        {
            EnsurePositive(quantity, "Return quantity");
            return restock ? quantity : 0m;
        }

        public static string ReturnMovementType(bool restock)
        {
            return restock ? CustomerReturnRestock : CustomerReturnNotRestocked;
        }

        public static string ManualAdjustmentType(decimal delta)
        {
            if (delta == 0m)
            {
                throw new InvalidOperationException("Adjustment delta cannot be zero.");
            }

            return delta > 0m ? ManualAdjustmentIn : ManualAdjustmentOut;
        }

        private static void EnsurePositive(decimal value, string name)
        {
            if (value <= 0m)
            {
                throw new InvalidOperationException(name + " must be greater than zero.");
            }
        }
    }
}
