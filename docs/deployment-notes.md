# STC POS Deployment Notes

## Process Architecture

The WinForms application is built for `x86` in Debug and Release. `PrinterUtility.dll` is a 32-bit dependency, so running the POS as a 64-bit process can fail at receipt printing time. Keep installer and shortcut targets pointed at the x86 build output unless the printer integration is replaced with an AnyCPU/x64-compatible implementation.

## Runtime Data

Mutable local files are resolved through `RuntimePathProvider` under:

`%PROGRAMDATA%\Saman Trade Center\STC POS`

Configuration, fallback bills, paused carts, update logs, and local audit snapshots should live there instead of under the installed binary folder. Existing files found under `Application.StartupPath` are copied forward on first access so queued fallback bills and paused carts are not lost during upgrade.

Config secrets are protected with Windows DPAPI using `LocalMachine` scope. That matches the legacy assumption that a POS terminal's configuration and offline queue are machine-operational data, not per-Windows-user data. It allows another cashier Windows account on the same terminal to run the POS while keeping secrets out of plain JSON.

## Database Upgrade

Run migrations in numeric order before deploying the updated cashier application. Run `007_preflight_inventory_returns_auth_runtime_schema.sql` first and resolve any reported duplicate submission IDs or unknown stock movement types. Migration `007_inventory_returns_auth_runtime_schema.sql` is additive and idempotent: it conditionally creates missing baseline tables, adds required columns, expands stock movement types, adds return/refund schema, and records the migration in `schema_migrations` without dropping production data.

Do not reconcile inventory by directly editing `inventory.amount`. Use the administrator reconciliation screen to identify differences, then correct stock through an explicit stocktake or manual adjustment so a matching `stock_movement` row exists.
