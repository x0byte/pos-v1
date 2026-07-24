using System;
using WindowsFormsApp1;

namespace WindowsFormsApp1.Tests
{
    internal static class UpdateSafetyServiceTests
    {
        private static int failures;

        private static void Main()
        {
            InstallIsBlockedWhenActiveBillExists();
            InstallIsBlockedWhenShiftIsActive();
            InstallIsBlockedWhenPendingSyncEventsExist();
            InstallIsAllowedWhenSafetyChecksPass();

            if (failures > 0)
            {
                Environment.Exit(1);
            }

            Console.WriteLine("All update safety tests passed.");
        }

        private static void InstallIsBlockedWhenActiveBillExists()
        {
            var result = CreateService(hasOpenBill: true).CanInstallUpdate();
            AssertBlocked(result, "Cannot update while a bill is open.", nameof(InstallIsBlockedWhenActiveBillExists));
        }

        private static void InstallIsBlockedWhenShiftIsActive()
        {
            var result = CreateService(hasActiveShift: true).CanInstallUpdate();
            AssertBlocked(result, "Cannot update while a shift is active.", nameof(InstallIsBlockedWhenShiftIsActive));
        }

        private static void InstallIsBlockedWhenPendingSyncEventsExist()
        {
            var result = CreateService(hasPendingSyncEvents: true).CanInstallUpdate();
            AssertBlocked(result, "Cannot update while there are pending sync events.", nameof(InstallIsBlockedWhenPendingSyncEventsExist));
        }

        private static void InstallIsAllowedWhenSafetyChecksPass()
        {
            var result = CreateService().CanInstallUpdate();
            if (!result.Allowed)
            {
                Fail(nameof(InstallIsAllowedWhenSafetyChecksPass), "Expected update install to be allowed.");
            }
        }

        private static UpdateSafetyService CreateService(
            bool hasOpenBill = false,
            bool hasActiveShift = false,
            bool hasPendingSyncEvents = false)
        {
            return new UpdateSafetyService(new FakeUpdateSafetyStateProvider
            {
                OpenBill = hasOpenBill,
                ActiveShift = hasActiveShift,
                PendingSyncEvents = hasPendingSyncEvents
            });
        }

        private static void AssertBlocked(UpdateSafetyResult result, string expectedReason, string testName)
        {
            if (result.Allowed || result.Reason != expectedReason)
            {
                Fail(testName, "Expected block reason '" + expectedReason + "' but got '" + result.Reason + "'.");
            }
        }

        private static void Fail(string testName, string message)
        {
            failures++;
            Console.Error.WriteLine(testName + ": " + message);
        }

        private class FakeUpdateSafetyStateProvider : IUpdateSafetyStateProvider
        {
            public bool OpenBill { get; set; }
            public bool ActiveShift { get; set; }
            public bool PendingSyncEvents { get; set; }

            public bool HasOpenBill()
            {
                return OpenBill;
            }

            public bool HasActiveShift()
            {
                return ActiveShift;
            }

            public bool HasPendingSyncEvents()
            {
                return PendingSyncEvents;
            }
        }
    }
}
