using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class DefaultUpdateSafetyStateProvider : IUpdateSafetyStateProvider
    {
        public bool HasOpenBill()
        {
            return Application.OpenForms
                .OfType<billing>()
                .Any(form => !form.IsDisposed && form.BillItemCount > 0);
        }

        public bool HasActiveShift()
        {
            return UserSession.IsCashierSessionActive;
        }

        public bool HasPendingSyncEvents()
        {
            return FallbackBillLogger.HasUnsyncedBills();
        }
    }
}
