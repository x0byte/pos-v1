namespace WindowsFormsApp1
{
    public class UpdateSafetyResult
    {
        public bool Allowed { get; private set; }
        public string Reason { get; private set; }

        public static UpdateSafetyResult Allow()
        {
            return new UpdateSafetyResult { Allowed = true };
        }

        public static UpdateSafetyResult Block(string reason)
        {
            return new UpdateSafetyResult { Allowed = false, Reason = reason };
        }
    }
}
