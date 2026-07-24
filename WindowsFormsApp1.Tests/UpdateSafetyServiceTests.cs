using System;
using System.IO;
using WindowsFormsApp1;

namespace WindowsFormsApp1.Tests
{
    internal static class UpdateSafetyServiceTests
    {
        private static int failures;
        private static int passed;

        private static void Main()
        {
            InstallIsBlockedWhenActiveBillExists();
            InstallIsBlockedWhenShiftIsActive();
            InstallIsBlockedWhenPendingSyncEventsExist();
            InstallIsAllowedWhenSafetyChecksPass();
            SaleDeltaDecreasesInventory();
            VoidDeltaRestoresInventory();
            RestockedReturnIncreasesInventory();
            NonRestockedReturnDoesNotChangeSellableInventory();
            ManualAdjustmentMovementTypeMatchesDelta();
            ParserRejectsEmptyAndPlaceholderValues();
            ParserAcceptsSupportedDecimalFormats();
            ParserRejectsNegativeQuantity();
            PasswordHashVerifiesCorrectPassword();
            PasswordHashRejectsIncorrectPassword();
            LinkedReturnRejectsBillItemFromDifferentBill();
            LinkedReturnRejectsWrongProduct();
            LinkedReturnRejectsExcessCumulativeQuantity();
            WinFormsProjectTargetsX86();
            Migration007HasDatabaseIdempotencyConstraints();
            PreflightScriptReportsMigrationBlockers();
            FallbackSyncPersistsCreditAccountId();

            if (failures > 0)
            {
                Console.Error.WriteLine("Passed: " + passed + ", Failed: " + failures);
                Environment.Exit(1);
            }

            Console.WriteLine("Passed: " + passed + ", Failed: " + failures);
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
                return;
            }
            Pass();
        }

        private static void SaleDeltaDecreasesInventory()
        {
            AssertEqual(-2.5m, InventoryMutationRules.SaleDelta(2.5m), nameof(SaleDeltaDecreasesInventory));
        }

        private static void VoidDeltaRestoresInventory()
        {
            AssertEqual(2.5m, InventoryMutationRules.VoidDelta(-2.5m), nameof(VoidDeltaRestoresInventory));
        }

        private static void RestockedReturnIncreasesInventory()
        {
            AssertEqual(3m, InventoryMutationRules.ReturnDelta(3m, true), nameof(RestockedReturnIncreasesInventory));
        }

        private static void NonRestockedReturnDoesNotChangeSellableInventory()
        {
            AssertEqual(0m, InventoryMutationRules.ReturnDelta(3m, false), nameof(NonRestockedReturnDoesNotChangeSellableInventory));
        }

        private static void ManualAdjustmentMovementTypeMatchesDelta()
        {
            if (InventoryMutationRules.ManualAdjustmentType(1m) != InventoryMutationRules.ManualAdjustmentIn ||
                InventoryMutationRules.ManualAdjustmentType(-1m) != InventoryMutationRules.ManualAdjustmentOut)
            {
                Fail(nameof(ManualAdjustmentMovementTypeMatchesDelta), "Adjustment movement type did not match delta direction.");
                return;
            }
            Pass();
        }

        private static void ParserRejectsEmptyAndPlaceholderValues()
        {
            decimal ignored;
            if (PosNumberParser.TryParseMoney("", out ignored) ||
                PosNumberParser.TryParseMoney("Price: Not available", out ignored))
            {
                Fail(nameof(ParserRejectsEmptyAndPlaceholderValues), "Parser accepted empty or placeholder values.");
                return;
            }
            Pass();
        }

        private static void ParserAcceptsSupportedDecimalFormats()
        {
            decimal value;
            if (!PosNumberParser.TryParseMoney("1,234.50", out value) || value != 1234.50m)
            {
                Fail(nameof(ParserAcceptsSupportedDecimalFormats), "Parser did not accept expected decimal format.");
                return;
            }
            Pass();
        }

        private static void ParserRejectsNegativeQuantity()
        {
            decimal ignored;
            if (PosNumberParser.TryParseQuantity("-1", out ignored))
            {
                Fail(nameof(ParserRejectsNegativeQuantity), "Parser accepted a negative quantity.");
                return;
            }
            Pass();
        }

        private static void PasswordHashVerifiesCorrectPassword()
        {
            PasswordHash hash = PasswordHasher.Create("secret");
            if (!PasswordHasher.Verify("secret", hash.HashBase64, hash.SaltBase64, hash.Iterations))
            {
                Fail(nameof(PasswordHashVerifiesCorrectPassword), "Correct password did not verify.");
                return;
            }
            Pass();
        }

        private static void PasswordHashRejectsIncorrectPassword()
        {
            PasswordHash hash = PasswordHasher.Create("secret");
            if (PasswordHasher.Verify("wrong", hash.HashBase64, hash.SaltBase64, hash.Iterations))
            {
                Fail(nameof(PasswordHashRejectsIncorrectPassword), "Incorrect password verified.");
                return;
            }
            Pass();
        }

        private static void LinkedReturnRejectsBillItemFromDifferentBill()
        {
            AssertThrows(
                () => ReturnValidationRules.EnsureLinkedBillItemBelongsToBill(10, 11),
                nameof(LinkedReturnRejectsBillItemFromDifferentBill));
        }

        private static void LinkedReturnRejectsWrongProduct()
        {
            AssertThrows(
                () => ReturnValidationRules.EnsureLinkedProductMatchesInventoryItem(5, 6),
                nameof(LinkedReturnRejectsWrongProduct));
        }

        private static void LinkedReturnRejectsExcessCumulativeQuantity()
        {
            AssertThrows(
                () => ReturnValidationRules.EnsureReturnQuantityWithinSoldQuantity(2m, 1.5m, 0.6m),
                nameof(LinkedReturnRejectsExcessCumulativeQuantity));
        }

        private static void WinFormsProjectTargetsX86()
        {
            string projectText = File.ReadAllText(Path.Combine(FindRepoRoot(), "WindowsFormsApp1", "STC_POS.csproj"));
            if (!projectText.Contains("<PlatformTarget>x86</PlatformTarget>") ||
                !projectText.Contains("PrinterUtility") ||
                !projectText.Contains("processorArchitecture=x86"))
            {
                Fail(nameof(WinFormsProjectTargetsX86), "WinForms project is not pinned to x86 with the x86 printer dependency.");
                return;
            }
            Pass();
        }

        private static void Migration007HasDatabaseIdempotencyConstraints()
        {
            string migrationText = File.ReadAllText(Path.Combine(FindRepoRoot(), "Migrations", "007_inventory_returns_auth_runtime_schema.sql"));
            if (!migrationText.Contains("uq_bill_history_client_submission_id") ||
                !migrationText.Contains("uq_returns_local_transaction") ||
                !migrationText.Contains("schema_migrations") ||
                !migrationText.Contains("SIGNAL SQLSTATE"))
            {
                Fail(nameof(Migration007HasDatabaseIdempotencyConstraints), "Migration 007 is missing expected idempotency/preflight schema protection.");
                return;
            }
            Pass();
        }

        private static void PreflightScriptReportsMigrationBlockers()
        {
            string preflightText = File.ReadAllText(Path.Combine(FindRepoRoot(), "Migrations", "007_preflight_inventory_returns_auth_runtime_schema.sql"));
            if (!preflightText.Contains("duplicate bill_history.client_submission_id") ||
                !preflightText.Contains("unknown stock_movement.movement_type") ||
                !preflightText.Contains("column type review"))
            {
                Fail(nameof(PreflightScriptReportsMigrationBlockers), "Migration preflight script does not report expected blockers.");
                return;
            }
            Pass();
        }

        private static void FallbackSyncPersistsCreditAccountId()
        {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "WindowsFormsApp1", "FallbackBillLogger.cs"));
            if (!source.Contains("private const int MinColumns = 12") ||
                !source.Contains("creditAccountId.ToString(CultureInfo.InvariantCulture)") ||
                !source.Contains("BillHistoryManager.SaveBill(items, salesperson, totalAmount, discountAmount, clientSubmissionId, paymentMethod, creditAccountId, dateTime)"))
            {
                Fail(nameof(FallbackSyncPersistsCreditAccountId), "Fallback retry does not preserve credit account id into SaveBill.");
                return;
            }
            Pass();
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
                return;
            }
            Pass();
        }

        private static void AssertEqual(decimal expected, decimal actual, string testName)
        {
            if (expected != actual)
            {
                Fail(testName, "Expected " + expected + " but got " + actual + ".");
                return;
            }
            Pass();
        }

        private static void AssertThrows(Action action, string testName)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                Pass();
                return;
            }

            Fail(testName, "Expected InvalidOperationException.");
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "POS.sln")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
            }

            return dir.FullName;
        }

        private static void Fail(string testName, string message)
        {
            failures++;
            Console.Error.WriteLine(testName + ": " + message);
        }

        private static void Pass()
        {
            passed++;
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
