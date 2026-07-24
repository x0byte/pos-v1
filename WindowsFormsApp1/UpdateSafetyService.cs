namespace WindowsFormsApp1
{
    public class UpdateSafetyService : IUpdateSafetyService
    {
        private readonly IUpdateSafetyStateProvider stateProvider;

        public UpdateSafetyService(IUpdateSafetyStateProvider stateProvider)
        {
            this.stateProvider = stateProvider;
        }

        public UpdateSafetyResult CanInstallUpdate()
        {
            if (stateProvider.HasOpenBill())
            {
                return UpdateSafetyResult.Block("Cannot update while a bill is open.");
            }

            if (stateProvider.HasActiveShift())
            {
                return UpdateSafetyResult.Block("Cannot update while a shift is active.");
            }

            if (stateProvider.HasPendingSyncEvents())
            {
                return UpdateSafetyResult.Block("Cannot update while there are pending sync events.");
            }

            return UpdateSafetyResult.Allow();
        }
    }
}
