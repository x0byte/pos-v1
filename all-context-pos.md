# STC POS — Complete System Context

**Project:** Saman Trade Center Point of Sale (STC_POS)
**Framework:** .NET Framework 4.7.2 — Windows Forms
**Database:** MySQL
**Namespace:** WindowsFormsApp1
**Git Branch:** version3

---

## 1. Project File Map

| File | Role |
|------|------|
| Program.cs | Entry point |
| Form1.cs | Login screen |
| Home.cs | Main dashboard / menu |
| billing.cs | POS / checkout (core form) |
| item_scan.cs | Barcode scanning dialog |
| Inventory-management.cs | Product CRUD |
| BillHistory.cs | Completed-bill list view |
| BillHistoryManager.cs | All DB operations for bills + stock movements |
| BillDetailView.cs | Single-bill detail dialog with reprint |
| BillItem.cs | In-memory line-item model (RowId, ItemName, Rate, Amount, DiscountedPrice) |
| BillPersistenceModels.cs | Shared record models (BillLineRecord, StockMovementRecord, PausedCartRecord) |
| PendingBillsInbox.cs | Admin paused-bill inbox |
| PendingBillDetailView.cs | Single pending-bill detail dialog |
| emp_selection.cs | Cashier/salesperson picker modal |
| AppCache.cs | In-memory inventory + employee cache |
| DatabaseConfig.cs | Connection string + OverridePassword management |
| AppSettings.cs | Admin DB settings UI |
| UserSession.cs | Runtime session state (IsAdmin, Username) |
| PDFConverter.cs | Receipt / PDF generator (Code 39 barcode, GDI+) |
| FallbackBillLogger.cs | Offline CSV bill logger + sync retry |
| DesktopPosLocalAudit.cs | Local audit: override_log.csv + mobile_bill_snapshots/ diffs |
| KeywordGenerator.cs | Sinhala + English brand/product keyword engine |
| print_window.cs | Print preview window |

---

## 2. Database

**Database name:** `db_stc`
**Default connection:** `server=127.0.0.1;database=db_stc;uid=root;pwd=;`
**Config file:** `config.json` (Application.StartupPath)

### 2.1 Config File Format (config.json)
```json
{
  "Server": "127.0.0.1",
  "Port": "3306",
  "Database": "db_stc",
  "Uid": "root",
  "Pwd": "",
  "OverridePassword": "admin123"
}
```
Loaded at startup by `DatabaseConfig.Load()`. Missing file falls back to hardcoded defaults silently.
`OverridePassword = "admin123"` logs a console warning (it is the sample default).

---

### 2.2 Table: `users`
| Column | Type | Notes |
|--------|------|-------|
| username | VARCHAR | Primary key |
| password | VARCHAR | **Plain text — no hashing (critical vulnerability)** |
| isAdmin | INT | 0 = regular user, 1 = admin |

---

### 2.3 Table: `inventory`
| Column | Type | Notes |
|--------|------|-------|
| id | INT | PK, auto-increment |
| item_name | VARCHAR | Supports Sinhala Unicode |
| retail_price | DECIMAL | Selling price |
| amount | INT | Current stock quantity (**NOT auto-decremented on sale**) |
| added_by | VARCHAR | Username who added the item |
| keywords | VARCHAR | Searchable aliases (auto-enriched at runtime by KeywordGenerator) |
| barcode | VARCHAR | EAN / barcode string |
| cost | DECIMAL | NULLABLE — cost price for profitability check |
| created_at | TIMESTAMP | Auto-set on insert |
| updated_at | TIMESTAMP | Auto-updated on change |

---

### 2.4 Table: `employee`
| Column | Type | Notes |
|--------|------|-------|
| emp_code | VARCHAR | PK — unique cashier ID |
| emp_name | VARCHAR | NULLABLE — falls back to emp_code if null or missing column |

**Note:** AppCache has a fallback query `SELECT emp_code FROM employee` in case the `emp_name` column doesn't exist.

---

### 2.5 Table: `bill_history`
| Column | Type | Notes |
|--------|------|-------|
| bill_id | INT | PK, auto-increment |
| bill_code | VARCHAR | Format: `STC-XXXXX` (e.g. STC-00123). Inserted as `''` then updated. |
| date_time | DATETIME | Bill creation timestamp |
| salesperson | VARCHAR | Employee `emp_code` — never emp_name |
| total_amount | DECIMAL | Sum of (rate × qty) before discounts |
| discount_amount | DECIMAL | Total discount given |
| grand_total | DECIMAL | total_amount − discount_amount |
| item_count | INT | Number of line items |
| client_submission_id | VARCHAR | NULLABLE — GUID generated per billing session for idempotency |

**bill_code generation:** INSERT with `bill_code = ''` → `LastInsertedId` → `UPDATE bill_code = "STC-" + id.ToString("D5")`.
**Idempotency:** Before inserting, code checks `SELECT bill_id, bill_code FROM bill_history WHERE client_submission_id = @sid LIMIT 1`. If found, returns existing bill_code (prevents double-posting on retry).

---

### 2.6 Table: `bill_history_items`
| Column | Type | Notes |
|--------|------|-------|
| id | INT | PK, auto-increment |
| bill_id | INT | FK → bill_history.bill_id |
| item_name | VARCHAR | Product name at time of sale (**snapshot** — not a FK to inventory) |
| rate | DECIMAL | Unit price at time of sale |
| amount | DECIMAL | Quantity (kg or pieces) |
| discounted_price | DECIMAL | Line total after all discounts |

---

### 2.7 Table: `stock_movement`
| Column | Type | Notes |
|--------|------|-------|
| item_id | INT | FK → inventory.id (looked up by exact item_name match in AppCache) |
| item_name | VARCHAR | Snapshot of item name at time of movement |
| movement_type | VARCHAR | `'sale'` for checkout, custom for manual adjustments |
| qty_delta | DECIMAL | **Negative** for sales (e.g. `-2.5`) |
| reference_type | VARCHAR | `'bill_history'` for sales |
| reference_id | INT | bill_history.bill_id |
| occurred_at | DATETIME | When the sale happened (passed from checkout timestamp) |
| created_at | DATETIME | `NOW()` at insert time |
| created_by_user_id | INT | NULL from desktop POS (reserved for web/mobile) |
| created_by_username | VARCHAR | `UserSession.Username` or `"desktop-pos"` if not set |
| note | VARCHAR | `"Desktop POS sale"` for all POS checkouts |

**Important:** Written inside the same transaction as `bill_history` + `bill_history_items`. If no exact `inventory.item_name` match is found in `AppCache.Inventory`, the movement is skipped and logged to `stc_fallback_bills.csv` as a `STOCK_MOVEMENT_SKIPPED` row.

---

### 2.8 Table: `pending_bill`
| Column | Type | Notes |
|--------|------|-------|
| id | INT | PK, auto-increment |
| session_id | VARCHAR | NULLABLE — external session identifier |
| cashier_code | VARCHAR | Employee code |
| created_at | DATETIME | When bill was paused |
| updated_at | DATETIME | Last modification time |
| status | VARCHAR | NULLABLE — lifecycle state (detected dynamically) |
| note | VARCHAR | NULLABLE — admin note |

**Status values (dynamically detected at runtime by `DetectStatusConventions()`):**

Waiting (shown in inbox):
- NULL / empty → shown
- `pending`, `waiting`, `awaiting`, `awaiting_admin`, `new` → shown if present in DB

Terminal (hidden from inbox):
- `cancelled`, `canceled`, `inactive`, `completed`, `complete`, `done`, `printed`, `closed`, `in_review`, `claimed`, `loaded`, `loaded_for_edit`

Cancelled status preference: `cancelled` → `canceled` → `inactive` → defaults to `"cancelled"`.

---

### 2.9 Table: `pending_bill_items`
| Column | Type | Notes |
|--------|------|-------|
| id | INT | PK, auto-increment |
| pending_bill_id | INT | FK → pending_bill.id |
| item_name | VARCHAR | Product name |
| rate | DECIMAL | Unit price |
| amount | DECIMAL | Quantity |
| discounted_price | DECIMAL | Line total after discounts |

---

## 3. All SQL Queries

### Authentication
```sql
SELECT isAdmin FROM users
WHERE username = @username AND password = @password LIMIT 1
```

### Inventory — Read All (AppCache)
```sql
SELECT id, item_name, retail_price, cost, barcode, keywords FROM inventory
```

### Inventory — Read All (Inventory Management form)
```sql
SELECT * FROM inventory
```

### Inventory — Insert
```sql
INSERT INTO inventory (item_name, retail_price, amount, added_by, keywords, barcode, cost)
VALUES (@name, @price, @amount, @added_by, @keywords, @barcode, @cost)
```

### Inventory — Update
```sql
UPDATE inventory
SET item_name=@item_name, retail_price=@price, amount=@amount,
    added_by=@added_by, keywords=@keywords, barcode=@barcode, cost=@cost
WHERE id = @id
```

### Inventory — Delete
```sql
DELETE FROM inventory WHERE id = @id
```

### Employees — Read
```sql
SELECT emp_code, emp_name FROM employee
-- Fallback if emp_name column missing (caught by try/catch):
SELECT emp_code FROM employee
```

### Bill History — Idempotency Check
```sql
SELECT bill_id, bill_code FROM bill_history WHERE client_submission_id = @sid LIMIT 1
```

### Bill History — Insert Header
```sql
INSERT INTO bill_history
  (bill_code, date_time, salesperson, total_amount, discount_amount, grand_total, item_count, client_submission_id)
VALUES ('', @date_time, @salesperson, @total_amount, @discount_amount, @grand_total, @item_count, @client_submission_id)
```

### Bill History — Update Bill Code
```sql
UPDATE bill_history SET bill_code = @bill_code WHERE bill_id = @bill_id
```

### Bill History — Insert Line Items
```sql
INSERT INTO bill_history_items (bill_id, item_name, rate, amount, discounted_price)
VALUES (@bill_id, @item_name, @rate, @amount, @discounted_price)
```

### Stock Movement — Insert (per line item, inside checkout transaction)
```sql
INSERT INTO stock_movement
  (item_id, item_name, movement_type, qty_delta, reference_type, reference_id,
   occurred_at, created_at, created_by_user_id, created_by_username, note)
VALUES
  (@item_id, @item_name, 'sale', @qty_delta, 'bill_history', @reference_id,
   @occurred_at, NOW(), NULL, @created_by_username, 'Desktop POS sale')
```
`qty_delta` = `−item.Amount` (negative decimal).

### Bill History — Fetch All
```sql
SELECT bill_id, bill_code, date_time, salesperson, item_count, grand_total, total_amount, discount_amount
FROM bill_history ORDER BY date_time DESC
```

### Bill History — Search with Filters
```sql
SELECT bill_id, bill_code, date_time, salesperson, item_count, grand_total, total_amount, discount_amount
FROM bill_history
WHERE date_time BETWEEN @fromDate AND @toDate
  [AND (bill_code LIKE @search OR salesperson LIKE @search)]
ORDER BY date_time DESC
```
`toDate` is padded to end-of-day: `toDate.Date.AddDays(1).AddSeconds(-1)`.

### Bill Detail — Header
```sql
SELECT * FROM bill_history WHERE bill_id = @bill_id
```

### Bill Detail — Items
```sql
SELECT id, item_name, rate, amount, discounted_price
FROM bill_history_items WHERE bill_id = @bill_id ORDER BY id
```

### Pending Bills — Detect Status Conventions
```sql
SELECT DISTINCT LOWER(TRIM(CAST(status AS CHAR))) AS normalized_status
FROM pending_bill WHERE status IS NOT NULL
```

### Pending Bills — Load Inbox (primary)
```sql
SELECT pb.id, pb.session_id, pb.cashier_code, pb.created_at,
       COALESCE(COUNT(pbi.id), 0) AS items_count, pb.note
FROM pending_bill pb
LEFT JOIN pending_bill_items pbi ON pbi.pending_bill_id = pb.id
WHERE (pb.status IS NULL
    OR TRIM(CAST(pb.status AS CHAR)) = ''
    OR LOWER(TRIM(CAST(pb.status AS CHAR))) IN (@status0, @status1, ...))
GROUP BY pb.id, pb.session_id, pb.cashier_code, pb.created_at, pb.note
ORDER BY pb.created_at DESC
```

### Pending Bills — Load Inbox (fallback when primary returns 0 rows)
Same query but uses `fallbackVisibleStatuses` (any non-terminal status in the DB) instead.

### Pending Bills — Load Items
```sql
SELECT item_name, rate, amount, discounted_price
FROM pending_bill_items WHERE pending_bill_id = @id ORDER BY id ASC
```

### Pending Bills — Cancel
```sql
UPDATE pending_bill SET status = @status, updated_at = NOW() WHERE id = @id
```

### Pending Bills — Delete (in transaction)
```sql
DELETE FROM pending_bill_items WHERE pending_bill_id = @id;
DELETE FROM pending_bill WHERE id = @id;
```

---

## 4. Features

### 4.1 Login (Form1.cs)
- Accepts username + password.
- Parameterized query (safe from SQL injection); password compared plain-text.
- Sets `UserSession.IsAdmin` and `UserSession.Username` (trimmed).
- On startup, runs `DatabaseConfig.Load()` then `AppCache.Load()` synchronously.
- Concurrently in background: `FallbackBillLogger.RetryUnsynced()` → shows popup if bills were synced.

### 4.2 Home Dashboard (Home.cs)
| Button | Visible To | Opens |
|--------|-----------|-------|
| Start Billing | All | billing.cs |
| View Bill History | All | BillHistory.cs |
| Inventory Management | All | Inventory-management.cs |
| Settings | Admin only | AppSettings.cs |
| Pending Bills | Admin only | PendingBillsInbox.cs |
| Logout | All | Form1.cs |

### 4.3 Billing / POS (billing.cs)
**Core checkout form — stateless in-memory cart.**

#### Item entry
- Text search: type in `txtItemName` → autocomplete from `AppCache.Inventory` (item name + keywords, max 20 suggestions, space-collapsed matching).
- Barcode scan: `item_scan.cs` → exact barcode field match in cache.
- Clicking a suggestion or pressing Enter populates `txtRetailPrice` and `lblCost`.

#### Discounts
- `txtDisEach` = per-unit discount (applied × qty).
- `txtDisWhole` = flat whole-line discount.
- Empty fields default to `0`.

#### Per-item profitability check (`isTheSaleProfitable()`)
- `minimum_price = cost × qty`
- `billed_price = (retailPrice − each_discount) × qty − whole_discount`
- If `minimum_price > 0 AND billed_price < minimum_price`:
  - If `OverridePassword` not configured → block sale.
  - If configured → prompt password dialog.
  - On correct password → `DesktopPosLocalAudit.AppendOverrideLog(...)` → allow.
  - On wrong password → block sale.
- If `cost` is null → check skipped.

#### Checkout sequence
1. User clicks Checkout → confirm dialog.
2. `EnsureCurrentSubmissionId()` → generates GUID if not set.
3. `ReorderBillingTable()` → renumbers RowIds 1..n.
4. `LoadBillingData()` → refreshes DataGridView from `billItems`.
5. `loadEmployeeCode(pendingSalespersonHint)` → shows `emp_selection` modal.
6. `CalculateGrandTotalFromMemory()` = SUM(rate × amount) for all items.
7. `BillHistoryManager.SaveBill(grid, cashierName, totalAmount, discountedAmount, clientSubmissionId)`.
8. On DB failure → user dialog → `FallbackBillLogger.LogFailedBill(...)` → `billCode = "LOCAL-yyyyMMddHHmmss"`.
9. `PDFConverter.ConvertPrintDocumentToPdf(grid, cashierName, totalAmount, discountedAmount, billCode, billCreatedAt)`.
10. If `pendingSnapshotSessionId` set → `DesktopPosLocalAudit.WritePendingDiff(snapshotSessionId, billItems)`.
11. Clear: `billItems.Clear()`, reset `nextRowId = 1`, `billCreatedAt = DateTime.Now`, null out IDs and hints.

#### Cancel bill
Clears `billItems` and resets all state without touching DB.

#### Multi-cart pause system
- Up to **5** labeled paused carts simultaneously (enforced in UI).
- User prompted for a short label string (`PromptForPauseLabel()`).
- Stored in `static Dictionary<string, PausedCartRecord> pausedBillItems` keyed by label.
- Persisted to `paused_carts.json` (`Application.StartupPath`) via `SavePausedCarts()`.
- Loaded once at app start (`EnsurePausedCartsLoaded()`); prompted on open if carts exist.
- Resume: `ShowPausedCartPicker()` → list box showing label + item count + pause time → load items back.
- Button text: "Pause this Bill" (orange) when cart has items; "Resume Paused Bill" (black) when paused carts exist and cart is empty.

#### Sync status labels
- `lblStockSync`: "Last synced: yyyy-MM-dd HH:mm:ss" or "never".
- `lblSyncStatus`:
  - Green: "All bills synced"
  - OrangeRed: "Unsynced bills pending"
  - Red: "Offline queue is large - database may be unreachable. Contact admin." (triggered when CSV > 10 MB or > 50,000 lines)
- Background timer fires `FallbackBillLogger.RetryUnsynced()` every **3 minutes**.

#### Context set from pending bill (PendingBillDetailView → "Move to Billing")
- `pendingSalespersonHint`: pre-selects cashier in emp_selection.
- `pendingSnapshotSessionId`: used to write diff file at checkout.

### 4.4 Bill History (BillHistory.cs)
- Default filter: last 30 days to today.
- Search: bill_code or salesperson LIKE match.
- Double-click → `BillDetailView.cs` with reprint.

### 4.5 Inventory Management (Inventory-management.cs)
- Admin: add, update, delete items.
- Client-side search validates input: `Regex.IsMatch(input, @"^[a-zA-Z0-9\s]*$")`.
- Keywords auto-merged with generated aliases via `KeywordGenerator.MergeWithGenerated()`.
- "Helakuru" button opens `https://www.helakuru.lk/keyboard` (Sinhala IME helper).

### 4.6 Pending Bills Inbox (PendingBillsInbox.cs) — Admin Only
- Auto-detects status conventions on open.
- Shows only non-terminal bills.
- Double-click → `PendingBillDetailView.cs`.
- If `NavigatedToBilling` → inbox closes itself.
- If `BillWasRemoved` → reloads grid.

### 4.7 Pending Bill Detail (PendingBillDetailView.cs) — Admin Only

**Summary recalculation (client-side):**
- Total = SUM(rate × qty)
- Grand Total = SUM(discounted_price)
- Discount = Total − Grand Total

**Print Now:**
- Prompts emp_selection if cashier_code blank.
- `BillHistoryManager.SaveBillFromDataTable(...)` → writes to `bill_history` + `bill_history_items` + `stock_movement`.
- On DB failure → user dialog → `FallbackBillLogger.LogFailedBill(...)` → `billCode = "LOCAL-..."`.
- `PDFConverter.ConvertPrintDocumentToPdf(...)`.
- `RemovePendingBill()` (DELETE items then bill in transaction).

**Move to Billing:**
- `DesktopPosLocalAudit.SavePendingSnapshot(sessionId, items)` → saves `{sessionId}.json`.
- `billingForm.ClearCurrentBillForPendingLoad()`.
- `billingForm.AddBillItem(...)` per item.
- `billingForm.SetPendingBillContext(cashierCode, snapshotSessionId)`.
- `RemovePendingBill()`.
- Sets `NavigatedToBilling = true`.

**Cancel Bill:**
- `UPDATE pending_bill SET status = @cancelledStatus, updated_at = NOW() WHERE id = @id`.

### 4.8 Employee Selection (emp_selection.cs)
- Modal picker, lists from `AppCache.Employees`.
- Returns `SelectedEmployee` (emp_code string).
- Accepts `preferredEmployeeCode` to pre-select.

### 4.9 Settings (AppSettings.cs) — Admin Only
- Edit server, port, database, uid, pwd.
- "Test Connection" validates before save.
- Saves to `config.json`; existing `OverridePassword` is preserved across DB config saves.
- Takes effect immediately (updates `DatabaseConfig.ConnectionString`).

---

## 5. Algorithms & Business Logic

### 5.1 Pricing / Discount Formula
```
lineTotal = (retailPrice × qty) − (perUnitDiscount × qty) − flatLineDiscount
grandTotal = SUM(lineTotal)
discountAmount = SUM(retailPrice × qty) − grandTotal
```
All fields are `decimal` throughout (no float arithmetic in calculations).

### 5.2 Profitability Check (`isTheSaleProfitable()`)
```
minimum_price = cost × qty          // cost from lblCost (0 if null)
billed_price  = (retailPrice − each_discount) × qty − whole_discount

if minimum_price > 0 AND billed_price < minimum_price:
    if OverridePassword == "" → block
    else → prompt password → if correct: audit log + allow
                              if wrong: block
else: allow (cost null or >= billed price)
```

### 5.3 `CalculateGrandTotalFromMemory()` (Grand Total before discounts)
```
grandTotal = SUM(item.Rate × item.Amount) for all billItems
```
This is the "full price" total. The displayed total (`lblTotalPrice`) is `SUM(item.DiscountedPrice)`.
`discountedAmount = CalculateGrandTotalFromMemory() − decimal.Parse(lblTotalPrice.Text)`.

### 5.4 Bill Code Generation
```
INSERT bill_history with bill_code = ''
bill_id = LastInsertedId
bill_code = "STC-" + bill_id.ToString("D5")   // e.g. STC-00042
UPDATE bill_history SET bill_code WHERE bill_id
```
Offline fallback code: `"LOCAL-" + DateTime.Now.ToString("yyyyMMddHHmmss")`.

### 5.5 Idempotent Checkout (`client_submission_id`)
- A GUID is generated once per billing session via `EnsureCurrentSubmissionId()`.
- Stored as `currentClientSubmissionId` on the billing form.
- Passed to `BillHistoryManager.SaveBill()` and `FallbackBillLogger.LogFailedBill()`.
- Before any INSERT, the code queries `bill_history WHERE client_submission_id = @sid`.
- If found → returns existing `bill_code` without re-inserting.
- Same check done in `FallbackBillLogger.TrySyncBill()` during retry.

### 5.6 Stock Movement Recording
Called within the checkout transaction as `InsertStockMovements()`:
1. For each `BillLineRecord`, look up `AppCache.Inventory` by exact `item_name` (case-sensitive, `StringComparison.Ordinal`).
2. If not found → call `FallbackBillLogger.LogStockMovementSkipped(billCode, itemName, reason)` → skip.
3. If found → INSERT into `stock_movement` with `qty_delta = −amount`.

### 5.7 Offline Fallback (FallbackBillLogger.cs)
**CSV file:** `stc_fallback_bills.csv`  
**Temp file:** `stc_fallback_bills.csv.tmp`  
**Backup file:** `stc_fallback_bills.csv.bak`

**Row types (column 0):**
- `BILL` — bill header: `[BILL, billRef, timestamp(ISO), salesperson, totalAmount, discountAmount, grandTotal, itemCount, synced(0/1), clientSubmissionId]`
- `ITEM` — line item: `[ITEM, billRef, itemName, rate, amount, discountedPrice, "", "", synced(0/1)]`
- `MOVEMENT` — stock movement: `[MOVEMENT, billRef, itemName, qty_delta, movement_type, occurred_at(ISO), created_by_username, note, synced(0/1)]`
- `STOCK_MOVEMENT_SKIPPED` — audit skip: `[STOCK_MOVEMENT_SKIPPED, billRef, itemName, reason, "", "", "", "", synced(0/1)]`

**Retry (`RetryUnsynced()`):**
1. Read all CSV lines.
2. Find `BILL` rows where col[8] != "1".
3. For each unsynced bill, gather its `ITEM` and `MOVEMENT` rows by matching `billRef` (col[1]).
4. `TrySyncBill()` → open DB connection → idempotency check → INSERT header → UPDATE bill_code → INSERT items → INSERT movements → COMMIT.
5. On success → mark all rows for that `billRef` as col[8] = "1".
6. `AtomicRewrite()`: write to `.tmp` → `File.Replace(tmp, csv, .bak)`.

**Large queue thresholds:** >10 MB file size OR >50,000 lines → `IsQueueLarge()` returns true → red warning in UI.

### 5.8 Multi-Cart Pause / Resume
```
pausedBillItems = Dictionary<string (label), PausedCartRecord>
PausedCartRecord { Label, PausedAt, List<BillLineRecord> Items }
```
- Max 5 simultaneous paused carts.
- Label must be non-empty (user-entered).
- Persisted to `paused_carts.json` after every add/remove.
- On billing form open: `EnsurePausedCartsLoaded()` runs once (static flag).
- On resume: items re-added via `AddBillItem()`, label removed from dict, file rewritten.

### 5.9 Keyword Search Engine (KeywordGenerator.cs)
Three-layer pipeline on every token of `item_name`:

**Layer 1 — Brand Aliases (English, case-insensitive):**
Pre-defined map: `edin→[edin, edinborough]`, `raigam→[raigam, rayigam]`, `zesta→[zesta, sesta]`, `diamond→[diamond, diamand, dayamand]`, `vitagen`, `maliban`, `milo`, `anchor`, `mdk`, `laoji`, `heladiva`, `watawala`, `steuart`, `harischandra`, `cargills`, `sunquick`, `cheris`, `sustagen`, `vijaya`, `melko`, `araliya`, `jayathilaka`, `mortin`, `ninja`, `nestomalt`, `ovaltine`, `diana`, `uswatta`, `minty`, `tulip`, etc.

**Layer 2 — Product Word Aliases (English, case-insensitive):**
Semantic map: `milk→[kiri, keeri, milk]`, `tea→[the, tea, kahata]`, `rice→[haal, rice]`, `sugar→[seeni, sugar]`, `oil→[thel, oil]`, `fish→[maalu, malu, fish]`, `flour→[piti, flour]`, `soap→[saban, soap]`, `biscuit→[biskat, bisket, biscuit]`, `chocolate→[choco, choklat, chokolat, choklet, chocolate]`, `mackerel→[mackerel, makaral, saman, seman]`, `diapers→[pampers, pampas, dayapers, diapers]`, `mayonnaise→[mayo, meyo, mayonnaise]`, `cordial→[codial, kodiyal, coordial, cordial]`, etc.

**Layer 3 — Sinhala:**
- First: exact Unicode word lookup in `SinhalaWordOverrides` dict (highest priority).
  - Examples: `සීනි→[seeni, sini, sugar]`, `කිරි→[kiri, keeri, milk]`, `හාල්→[haal, rice]`, `සහල්→[haal, rice]`, `ලූණු→[lunu, luunu, salt]`, `තෙල්→[thel, oil]`, `තේ→[the, tea]`, `ගම්මිරිස්→[gammiris, pepper]`, `මාළු→[maalu, malu, fish]`, etc. (100+ entries)
- Fallback: character-level abugida transliteration state machine.

**Transliteration state machine:**
- Consonant → emit phoneme + `'a'` (inherent vowel), set `pendingInherentVowel = true`.
- Vowel sign (matra) → remove pending `'a'`, append actual vowel string.
- Virama (්, U+0DCA) → remove pending `'a'` (joins consonant cluster).
- Anusvara (ං, U+0D82) → emit `'n'`.
- ZWJ/ZWNJ → skip.
- Stand-alone vowel letters → emit directly.

**Tokenization:** `Regex.Split(name, @"[\s\(\)\[\]\/\-\,\.&\+]+")`. Tokens starting with a digit are skipped (e.g. `400g`, `1kg`, `200ml`).

**MergeWithGenerated():** Appends generated aliases to existing DB keywords, skipping duplicates (case-insensitive). Called at `AppCache.Load()` — DB is not modified.

**Suggestion matching in billing.cs (`GetSuggestions()`):**
- item_name contains normalized query, OR
- item_name (spaces removed) contains query (spaces removed), OR
- keywords contain normalized query.
- Max 20 suggestions, deduplicated by item name.

### 5.10 Pending Bill Status Detection (`DetectStatusConventions()`)
1. Query distinct status values from `pending_bill`.
2. `waitingStatuses` = intersection of `{pending, waiting, awaiting_admin, awaiting, new}` with DB values. If empty → defaults to `["pending"]`.
3. `fallbackVisibleStatuses` = all DB statuses NOT in terminal list.
4. `cancelledStatus` = first of `cancelled`/`canceled`/`inactive` found, else `"cancelled"`.
5. If primary query returns 0 rows → retry with `fallbackVisibleStatuses`.

### 5.11 Desktop Audit (DesktopPosLocalAudit.cs)

**Override log (`override_log.csv`):**
Written every time a below-cost sale is approved.
Columns: `timestamp(ISO 8601), username, item_name, qty, retail_price, cost, attempted_line_total, reason_snippet`
CSV-escaped. Header written only on first row.

**Pending bill snapshot (`mobile_bill_snapshots/{sessionId}.json`):**
- Written by `SavePendingSnapshot()` when "Move to Billing" is chosen.
- Contains serialized `List<PendingBillSnapshotLine>` (ItemName, Rate, Amount, DiscountedPrice).
- Session ID sanitized (invalid filename chars replaced with `_`).

**Pending bill diff (`mobile_bill_snapshots/{sessionId}.diff.json`):**
- Written by `WritePendingDiff()` at checkout if `pendingSnapshotSessionId` is set.
- Compares original snapshot vs final bill items by index.
- Output: `{ session_id, generated_at, added: [...], removed: [...], modified: [...] }`.
- Used to detect what was changed between the pending bill arriving and the final checkout.

---

## 6. Security

### 6.1 Authentication
| Aspect | Implementation | Risk |
|--------|---------------|------|
| Password storage | Plain text in `users.password` | **Critical vulnerability** |
| Transport | Local MySQL loopback (127.0.0.1) | Low risk if LAN-only |
| SQL injection prevention | All queries use @param | Safe |
| Session | `UserSession.IsAdmin` + `UserSession.Username` (static, in-memory) | No token, no expiry |

### 6.2 Authorization
- Admin-only features gated in `Home_Load`, `btnSettings_Click`, `BtnPendingBills_Click`.
- Buttons hidden/disabled based on `UserSession.IsAdmin`.
- No server-side re-authorization — client-side flag only.

### 6.3 Below-Cost Sale Override
- `OverridePassword` from `config.json`.
- Prompted via inline `PromptForPassword()` dialog (password-masked TextBox).
- Every approved override written to `override_log.csv` (username, item, qty, prices, timestamp).
- If `OverridePassword` is empty → sale blocked with no override option.
- If still `"admin123"` (sample default) → console warning on startup.

### 6.4 Input Validation
- Inventory search: `Regex.IsMatch(input, @"^[a-zA-Z0-9\s]*$")` — rejects special chars.
- All SQL: parameterized `@param` queries (MySql.Data).
- CSV parsing: custom parser with RFC 4180-compatible quote escaping.
- Session ID sanitization: `Path.GetInvalidFileNameChars()` replaced with `_`.

---

## 7. In-Memory Cache (AppCache.cs)

```csharp
class InventoryItem {
    int Id;
    string ItemName;
    decimal RetailPrice;
    decimal? Cost;       // nullable — profitability check skipped if null
    string Barcode;
    string Keywords;     // enriched at load by KeywordGenerator.MergeWithGenerated()
}

class EmployeeItem {
    string EmpCode;
    string EmpName;      // equals EmpCode if emp_name is null/whitespace
}

static DateTime LastSynced;   // DateTime.MinValue until first load
```

**Loaded once at startup** (`Form1` constructor). **Refreshed** by Refresh Stock button → `AppCache.Refresh()` → `AppCache.Load()`.
Zero-row warning logged to console if inventory query returns empty.

---

## 8. Data Models (BillPersistenceModels.cs)

```csharp
sealed class BillLineRecord {
    string ItemName; decimal Rate; decimal Amount; decimal DiscountedPrice;
}

sealed class StockMovementRecord {  // defined, not yet wired to DB via this model
    string BillReference; string ItemName; decimal QtyDelta;
    DateTime OccurredAt; string CreatedByUsername; string Note;
}

sealed class PausedCartRecord {
    string Label; DateTime PausedAt; List<BillLineRecord> Items;
}
```

---

## 9. PDF Receipt Generation (PDFConverter.cs)

**Paper:** 2.85" × ~50" (thermal roll), 300 DPI.
**Library:** iText 8 + iTextSharp 5.

**Layout:**
1. Header — "Saman Trade Center" (Arial 18B), address, phone.
2. Transaction meta — date/time, salesperson name.
3. Items table — `# | Item Name (Rs.rate) | Rate | Qty (kg/pcs) | Price (Rs.)`. Multi-line item names with word wrap (`SplitText()`).
4. Totals — `Total Rs.`, `Discount Rs.` (bold, omitted if 0), `Grand Total Rs.` (bold/large).
5. Footer — "Thank you..." / returns policy / "POS System by BlackBox Technologies".
6. Code 39 barcode (bill_code).
7. Human-readable bill_code below barcode.

**Code 39 barcode (pure GDI+, no external library):**
- 9 elements per character (5 bars + 4 spaces), wide:narrow = 3:1.
- Start/stop `*` added automatically.
- Width formula: `availableWidth / (16×n − 1)` where n = total chars incl. start/stop.

**Print method:** `PrintReceipt()` (GDI+ / PrintDocument path) also exists as an alternative to the PDF path — currently commented-out from being called; PDF path is active.

---

## 10. File System Artifacts

| Path | Contents |
|------|---------|
| `{AppDir}/config.json` | DB connection + OverridePassword |
| `{AppDir}/paused_carts.json` | Serialized `List<PausedCartRecord>` — survives app restarts |
| `{AppDir}/stc_fallback_bills.csv` | Offline bill log (BILL/ITEM/MOVEMENT/STOCK_MOVEMENT_SKIPPED rows) |
| `{AppDir}/stc_fallback_bills.csv.tmp` | Temp file during atomic rewrite |
| `{AppDir}/stc_fallback_bills.csv.bak` | Backup of previous CSV before rewrite |
| `{AppDir}/override_log.csv` | Every below-cost sale override (append-only audit) |
| `{AppDir}/mobile_bill_snapshots/{sessionId}.json` | Original pending bill snapshot |
| `{AppDir}/mobile_bill_snapshots/{sessionId}.diff.json` | Diff between snapshot and final checkout |
| `Properties/Resources` | logo.png, Helakuru_logo.png |

---

## 11. Startup Sequence

```
Program.Main()
  → Form1 constructor
      → DatabaseConfig.Load()          // read config.json
      → AppCache.Load()                // load inventory + employees into memory
      → Task.Run(FallbackBillLogger.RetryUnsynced)  // background sync attempt
      → InitializeComponent()
  → User logs in → validateLogin()
      → UserSession.IsAdmin = ...
      → UserSession.Username = username.Trim()
      → Home form shown, Form1 hidden
```

---

## 12. Checkout Transaction (All-or-Nothing)

```
conn.Open()
  → idempotency check: SELECT WHERE client_submission_id (if set)
  → BeginTransaction()
  → INSERT bill_history (bill_code='')
  → LastInsertedId → format bill_code
  → UPDATE bill_history SET bill_code
  → INSERT bill_history_items × N rows
  → InsertStockMovements():
      → for each item: lookup AppCache.Inventory by exact item_name
          → if found: INSERT stock_movement (qty_delta = -amount)
          → if not found: FallbackBillLogger.LogStockMovementSkipped() (non-fatal)
  → Commit()
  [on any exception: Rollback() + rethrow → caller falls back to CSV]
```

---

## 13. Pending Bill Lifecycle

```
External system (web/mobile) → INSERT into pending_bill + pending_bill_items
                              → status = NULL or 'pending'

Admin opens PendingBillsInbox
  → DetectStatusConventions() from DB
  → LoadPendingBills() with status filter
  → Shows: id, session_id, cashier_code, created_at, items_count, note

Admin action:
  Print Now   → SaveBillFromDataTable() → checkout transaction → PDF → DELETE pending records
  Move to Billing → SavePendingSnapshot() → load items into billing.cs
                  → DELETE pending records → billing form takes over
                  → at checkout: WritePendingDiff() → diff.json written
  Cancel Bill → UPDATE status = 'cancelled'
  Close       → no action
```

---

## 14. Assumptions Baked Into the Code

| # | Assumption | Location |
|---|-----------|----------|
| 1 | Passwords plain-text; network assumed secure | Form1.cs, users table |
| 2 | Inventory `amount` NOT auto-decremented on sale; stock_movement tracks the delta instead | BillHistoryManager.cs |
| 3 | Up to 5 paused carts per app session; 6th attempt is blocked with a message | billing.cs |
| 4 | Paused carts survive app restarts (persisted to paused_carts.json) | billing.cs |
| 5 | Offline CSV fallback is for short-term blips; large queue (>10MB/>50K lines) signals admin intervention needed | FallbackBillLogger.cs |
| 6 | Keywords enrichment at runtime; DB `keywords` column stores only manually entered aliases | AppCache.cs |
| 7 | `emp_name` column may not exist; fallback query used if it causes an exception | AppCache.cs |
| 8 | `cost` field optional; profitability check skipped if NULL | billing.cs |
| 9 | Bill code format "STC-#####" is hard-coded; offline bills get "LOCAL-timestamp" codes | BillHistoryManager.cs |
| 10 | `pending_bill.status` values are installation-specific; detected dynamically | PendingBillsInbox.cs |
| 11 | Single user session per app instance; no concurrent user support | UserSession.cs |
| 12 | App runs on same machine or LAN as MySQL (loopback default) | DatabaseConfig.cs |
| 13 | Discounts are additive (per-unit + flat); no tiered or bundle pricing | billing.cs |
| 14 | Reprint uses saved DB data; item names are point-in-time snapshots, not FK references | BillDetailView.cs |
| 15 | Stock movement lookup uses exact case-sensitive `item_name` match; renamed items break the link | BillHistoryManager.cs |
| 16 | `salesperson` in bill_history stores `emp_code`, never `emp_name` (enforced by comments in code) | BillHistoryManager.cs, FallbackBillLogger.cs |
| 17 | `client_submission_id` is only set for checkouts from the desktop POS billing form (not from PendingBillDetailView "Print Now") | BillHistoryManager.cs |
| 18 | Pending bill snapshot/diff files are local only; no sync to external system | DesktopPosLocalAudit.cs |

---

## 15. Dependencies (NuGet)

| Package | Version | Purpose |
|---------|---------|---------|
| MySql.Data | 8.4.0 | MySQL connector |
| iText | 8.0.5 | PDF generation |
| iTextSharp | 5.5.13.4 | Legacy PDF support |
| Newtonsoft.Json | 13.0.1 | config.json + paused_carts.json parsing |
| BouncyCastle.Cryptography | 2.4.0 | iText dependency |
| PrinterUtility | 1.2.0 | Printer integration |

**Framework built-ins used:** System.Data, System.Windows.Forms, System.Drawing, System.Drawing.Printing, System.IO, System.Threading.Tasks, System.Text.RegularExpressions.

---

## 16. Hard-Coded Company Info (PDFConverter.cs)

```
Saman Trade Center
No.20, Matale road, Galewela
Tel: 066 22 89 468
Returns accepted within 7 days with the receipt
POS System by BlackBox Technologies
Developed and Maintained by BlackBox Computers™
```
