namespace WindowsFormsApp1
{
    public interface IUpdateSafetyStateProvider
    {
        bool HasOpenBill();
        bool HasActiveShift();
        bool HasPendingSyncEvents();
    }
}
