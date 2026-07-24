using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class ReturnProcessingForm : Form
    {
        private readonly TabControl tabs = new TabControl();
        private readonly TextBox txtBillCode = new TextBox();
        private readonly Button btnFindBill = new Button();
        private readonly DataGridView gridLinked = new DataGridView();
        private readonly Button btnProcessLinked = new Button();
        private readonly TextBox txtLinkedCustomer = new TextBox();
        private readonly TextBox txtLinkedPhone = new TextBox();
        private readonly ComboBox cboLinkedRefund = new ComboBox();
        private readonly TextBox txtLinkedCreditAccount = new TextBox();
        private DataRow linkedBillHeader;

        private readonly TextBox txtProductSearch = new TextBox();
        private readonly ListBox lstProducts = new ListBox();
        private readonly TextBox txtQty = new TextBox();
        private readonly TextBox txtUnitRefund = new TextBox();
        private readonly ComboBox cboCondition = new ComboBox();
        private readonly CheckBox chkRestock = new CheckBox();
        private readonly TextBox txtReason = new TextBox();
        private readonly TextBox txtNotes = new TextBox();
        private readonly ComboBox cboRefund = new ComboBox();
        private readonly TextBox txtCreditAccount = new TextBox();
        private readonly TextBox txtCustomer = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly DataGridView gridUnlinked = new DataGridView();
        private readonly DataTable unlinkedTable = new DataTable();

        public ReturnProcessingForm()
        {
            Text = "Process Return";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 720);
            BuildUi();
        }

        private void BuildUi()
        {
            tabs.Dock = DockStyle.Fill;
            tabs.TabPages.Add(BuildLinkedTab());
            tabs.TabPages.Add(BuildUnlinkedTab());
            Controls.Add(tabs);
        }

        private TabPage BuildLinkedTab()
        {
            TabPage page = new TabPage("Find original bill");
            Label lbl = MakeLabel("Bill reference", 20, 20);
            txtBillCode.SetBounds(140, 16, 220, 28);
            btnFindBill.Text = "Find";
            btnFindBill.SetBounds(375, 15, 90, 30);
            btnFindBill.Click += BtnFindBill_Click;

            AddCommonReturnHeader(page, txtLinkedCustomer, txtLinkedPhone, cboLinkedRefund, txtLinkedCreditAccount, 20, 58);

            gridLinked.SetBounds(20, 145, 1030, 430);
            gridLinked.AllowUserToAddRows = false;
            gridLinked.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridLinked.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            btnProcessLinked.Text = "Process Linked Return";
            btnProcessLinked.SetBounds(820, 590, 230, 45);
            btnProcessLinked.Click += BtnProcessLinked_Click;

            page.Controls.AddRange(new Control[] { lbl, txtBillCode, btnFindBill, gridLinked, btnProcessLinked });
            return page;
        }

        private TabPage BuildUnlinkedTab()
        {
            TabPage page = new TabPage("Continue without bill");
            AddCommonReturnHeader(page, txtCustomer, txtPhone, cboRefund, txtCreditAccount, 20, 16);

            page.Controls.Add(MakeLabel("Product", 20, 108));
            txtProductSearch.SetBounds(120, 104, 360, 28);
            txtProductSearch.TextChanged += TxtProductSearch_TextChanged;
            lstProducts.SetBounds(120, 136, 360, 95);
            lstProducts.Visible = false;
            lstProducts.Click += (s, e) => SelectProduct();

            page.Controls.Add(MakeLabel("Qty", 500, 108));
            txtQty.SetBounds(555, 104, 80, 28);
            page.Controls.Add(MakeLabel("Unit refund", 650, 108));
            txtUnitRefund.SetBounds(760, 104, 100, 28);
            page.Controls.Add(MakeLabel("Condition", 20, 245));
            cboCondition.SetBounds(120, 241, 160, 28);
            AddConditions(cboCondition);
            chkRestock.Text = "Restock to sellable inventory";
            chkRestock.SetBounds(300, 241, 240, 28);
            page.Controls.Add(MakeLabel("Reason", 555, 245));
            txtReason.SetBounds(635, 241, 280, 28);
            page.Controls.Add(MakeLabel("Notes", 20, 282));
            txtNotes.SetBounds(120, 278, 795, 28);

            Button btnAdd = new Button { Text = "Add Item" };
            btnAdd.SetBounds(930, 238, 120, 68);
            btnAdd.Click += BtnAddUnlinked_Click;

            BuildUnlinkedTable();
            gridUnlinked.DataSource = unlinkedTable;
            gridUnlinked.SetBounds(20, 325, 1030, 250);
            gridUnlinked.AllowUserToAddRows = false;
            gridUnlinked.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Button btnProcess = new Button { Text = "Process Unlinked Return" };
            btnProcess.SetBounds(790, 590, 260, 45);
            btnProcess.Click += BtnProcessUnlinked_Click;

            page.Controls.AddRange(new Control[] { txtProductSearch, lstProducts, txtQty, txtUnitRefund, cboCondition, chkRestock, txtReason, txtNotes, btnAdd, gridUnlinked, btnProcess });
            return page;
        }

        private void AddCommonReturnHeader(Control page, TextBox customer, TextBox phone, ComboBox refund, TextBox creditAccount, int x, int y)
        {
            page.Controls.Add(MakeLabel("Customer", x, y));
            customer.SetBounds(x + 100, y - 4, 220, 28);
            page.Controls.Add(customer);
            page.Controls.Add(MakeLabel("Phone", x + 340, y));
            phone.SetBounds(x + 405, y - 4, 160, 28);
            page.Controls.Add(phone);
            page.Controls.Add(MakeLabel("Refund", x + 585, y));
            refund.SetBounds(x + 660, y - 4, 150, 28);
            AddRefundMethods(refund);
            page.Controls.Add(refund);
            page.Controls.Add(MakeLabel("Credit acct ID", x + 825, y));
            creditAccount.SetBounds(x + 935, y - 4, 90, 28);
            page.Controls.Add(creditAccount);
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true };
        }

        private static void AddRefundMethods(ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.AddRange(new object[] { "CASH", "CARD", "CREDIT", "STORE_CREDIT", "EXCHANGE", "NO_REFUND" });
            combo.SelectedIndex = 0;
        }

        private static void AddConditions(ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.AddRange(new object[] { "Sellable", "Opened", "Damaged", "Defective", "Expired", "Other" });
            combo.SelectedIndex = 0;
        }

        private void BtnFindBill_Click(object sender, EventArgs e)
        {
            try
            {
                OriginalBillLookupResult result = ReturnManager.FindOriginalBill(txtBillCode.Text);
                if (result == null || result.Header == null)
                {
                    MessageBox.Show("Bill was not found.", "Return", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                linkedBillHeader = result.Header;
                DataTable table = result.Items;
                table.Columns.Add("return_quantity", typeof(decimal));
                table.Columns.Add("entered_unit_refund", typeof(decimal));
                table.Columns.Add("refund_amount", typeof(decimal));
                table.Columns.Add("item_condition", typeof(string));
                table.Columns.Add("restock", typeof(bool));
                table.Columns.Add("reason", typeof(string));
                table.Columns.Add("notes", typeof(string));
                foreach (DataRow row in table.Rows)
                {
                    decimal sold = Convert.ToDecimal(row["sold_quantity"]);
                    decimal line = Convert.ToDecimal(row["discounted_price"]);
                    decimal unit = sold == 0m ? 0m : line / sold;
                    row["return_quantity"] = 0m;
                    row["entered_unit_refund"] = unit;
                    row["refund_amount"] = 0m;
                    row["item_condition"] = "Sellable";
                    row["restock"] = true;
                    row["reason"] = "";
                    row["notes"] = "";
                }

                gridLinked.DataSource = table;
                MessageBox.Show("Bill loaded. Enter return quantities and reasons.", "Return", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load bill: " + ex.Message, "Return", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnProcessLinked_Click(object sender, EventArgs e)
        {
            try
            {
                if (linkedBillHeader == null)
                {
                    MessageBox.Show("Find an original bill first.", "Return", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ReturnRequest request = BuildLinkedRequest();
                CompleteReturn(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Return failed: " + ex.Message, "Return", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private ReturnRequest BuildLinkedRequest()
        {
            var request = new ReturnRequest
            {
                OriginalBillId = Convert.ToInt32(linkedBillHeader["bill_id"]),
                Cashier = UserSession.Username ?? "desktop-pos",
                RefundMethod = cboLinkedRefund.Text,
                CreditAccountId = ParseOptionalInt(txtLinkedCreditAccount.Text),
                CustomerName = txtLinkedCustomer.Text,
                CustomerPhone = txtLinkedPhone.Text,
                LocalTransactionId = Guid.NewGuid().ToString("N")
            };

            foreach (DataGridViewRow gridRow in gridLinked.Rows)
            {
                if (gridRow.IsNewRow) continue;
                decimal qty;
                if (!PosNumberParser.TryParseQuantity(Convert.ToString(gridRow.Cells["return_quantity"].Value), out qty, allowZero: true) || qty == 0m)
                {
                    continue;
                }

                decimal unitRefund = PosNumberParser.ParseRequiredMoney(Convert.ToString(gridRow.Cells["entered_unit_refund"].Value), "Unit refund");
                decimal refund = PosNumberParser.ParseRequiredMoney(Convert.ToString(gridRow.Cells["refund_amount"].Value), "Refund amount");
                string reason = Convert.ToString(gridRow.Cells["reason"].Value);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new InvalidOperationException("Reason is required for every returned item.");
                }

                request.Items.Add(new ReturnItemRequest
                {
                    OriginalBillItemId = Convert.ToInt32(gridRow.Cells["original_bill_item_id"].Value),
                    ProductId = Convert.ToInt32(gridRow.Cells["product_id"].Value),
                    ItemName = Convert.ToString(gridRow.Cells["item_name"].Value),
                    Quantity = qty,
                    OriginalUnitPrice = Convert.ToDecimal(gridRow.Cells["rate"].Value),
                    EnteredUnitRefund = unitRefund,
                    RefundAmount = refund,
                    Condition = Convert.ToString(gridRow.Cells["item_condition"].Value),
                    Restock = Convert.ToBoolean(gridRow.Cells["restock"].Value),
                    Notes = Convert.ToString(gridRow.Cells["notes"].Value)
                });
                request.Reason = string.IsNullOrWhiteSpace(request.Reason) ? reason : request.Reason + "; " + reason;
            }

            return request;
        }

        private void BuildUnlinkedTable()
        {
            unlinkedTable.Columns.Add("product_id", typeof(int));
            unlinkedTable.Columns.Add("item_name", typeof(string));
            unlinkedTable.Columns.Add("quantity", typeof(decimal));
            unlinkedTable.Columns.Add("entered_unit_refund", typeof(decimal));
            unlinkedTable.Columns.Add("refund_amount", typeof(decimal));
            unlinkedTable.Columns.Add("item_condition", typeof(string));
            unlinkedTable.Columns.Add("restock", typeof(bool));
            unlinkedTable.Columns.Add("reason", typeof(string));
            unlinkedTable.Columns.Add("notes", typeof(string));
        }

        private void TxtProductSearch_TextChanged(object sender, EventArgs e)
        {
            lstProducts.Items.Clear();
            string query = txtProductSearch.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                lstProducts.Visible = false;
                return;
            }

            foreach (InventoryItem item in InventorySearch.GetMatches(query, 20))
            {
                lstProducts.Items.Add(item.Id + " | " + item.ItemName);
            }
            lstProducts.Visible = lstProducts.Items.Count > 0;
        }

        private void SelectProduct()
        {
            if (lstProducts.SelectedItem == null) return;
            string selected = lstProducts.SelectedItem.ToString();
            int bar = selected.IndexOf('|');
            txtProductSearch.Text = bar >= 0 ? selected.Substring(bar + 1).Trim() : selected;
            lstProducts.Visible = false;
        }

        private void BtnAddUnlinked_Click(object sender, EventArgs e)
        {
            try
            {
                InventoryItem item = AppCache.Inventory.FirstOrDefault(x => string.Equals(x.ItemName, txtProductSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    throw new InvalidOperationException("Select an inventory product.");
                }

                decimal qty = PosNumberParser.ParseRequiredQuantity(txtQty.Text, "Quantity");
                decimal unitRefund = PosNumberParser.ParseRequiredMoney(txtUnitRefund.Text, "Unit refund");
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    throw new InvalidOperationException("Reason is required.");
                }

                unlinkedTable.Rows.Add(item.Id, item.ItemName, qty, unitRefund, qty * unitRefund,
                    cboCondition.Text, chkRestock.Checked, txtReason.Text.Trim(), txtNotes.Text.Trim());
                txtProductSearch.Clear();
                txtQty.Clear();
                txtUnitRefund.Clear();
                txtReason.Clear();
                txtNotes.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Return", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnProcessUnlinked_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Confirm this customer return has no original bill available?", "Unlinked Return",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                string approvedBy = GetApproval();
                if (string.IsNullOrWhiteSpace(approvedBy))
                {
                    return;
                }

                ReturnRequest request = new ReturnRequest
                {
                    Cashier = UserSession.Username ?? "desktop-pos",
                    ApprovedBy = approvedBy,
                    RefundMethod = cboRefund.Text,
                    CreditAccountId = ParseOptionalInt(txtCreditAccount.Text),
                    CustomerName = txtCustomer.Text,
                    CustomerPhone = txtPhone.Text,
                    LocalTransactionId = Guid.NewGuid().ToString("N")
                };

                foreach (DataRow row in unlinkedTable.Rows)
                {
                    request.Items.Add(new ReturnItemRequest
                    {
                        ProductId = Convert.ToInt32(row["product_id"]),
                        ItemName = Convert.ToString(row["item_name"]),
                        Quantity = Convert.ToDecimal(row["quantity"]),
                        EnteredUnitRefund = Convert.ToDecimal(row["entered_unit_refund"]),
                        RefundAmount = Convert.ToDecimal(row["refund_amount"]),
                        Condition = Convert.ToString(row["item_condition"]),
                        Restock = Convert.ToBoolean(row["restock"]),
                        Notes = Convert.ToString(row["notes"])
                    });
                    string reason = Convert.ToString(row["reason"]);
                    request.Reason = string.IsNullOrWhiteSpace(request.Reason) ? reason : request.Reason + "; " + reason;
                }

                CompleteReturn(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Return failed: " + ex.Message, "Return", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CompleteReturn(ReturnRequest request)
        {
            ReturnResult result = ReturnManager.ProcessReturn(request);
            ReturnReceiptPrinter.ShowReceipt(result.ReturnReference, request);
            MessageBox.Show("Return saved: " + result.ReturnReference, "Return", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GetApproval()
        {
            if (UserSession.IsAdmin)
            {
                return UserSession.Username ?? "admin";
            }

            using (Form prompt = new Form())
            using (TextBox txtUser = new TextBox())
            using (TextBox txt = new TextBox())
            using (Button ok = new Button())
            {
                prompt.Text = "Supervisor Approval";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.ClientSize = new Size(350, 170);
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Controls.Add(new Label { Text = "Supervisor username:", Left = 18, Top = 18, AutoSize = true });
                txtUser.Left = 18;
                txtUser.Top = 40;
                txtUser.Width = 300;
                prompt.Controls.Add(txtUser);

                prompt.Controls.Add(new Label { Text = "Supervisor password:", Left = 18, Top = 72, AutoSize = true });
                txt.Left = 18;
                txt.Top = 94;
                txt.Width = 300;
                txt.UseSystemPasswordChar = true;
                ok.Text = "Approve";
                ok.Left = 220;
                ok.Top = 128;
                ok.DialogResult = DialogResult.OK;
                prompt.Controls.Add(txt);
                prompt.Controls.Add(ok);
                prompt.AcceptButton = ok;
                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    string supervisorUsername = txtUser.Text?.Trim();
                    if (string.Equals(supervisorUsername, UserSession.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("A cashier cannot approve their own bill-less return.", "Return", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }

                    AuthenticationResult approval = AuthenticationService.ValidateLogin(supervisorUsername, txt.Text);
                    if (approval.Success && approval.IsAdmin)
                    {
                        return supervisorUsername;
                    }
                }
            }

            MessageBox.Show("Approval failed. Bill-less returns require an administrator account.", "Return", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        private static int ParseOptionalInt(string text)
        {
            int value;
            return int.TryParse((text ?? string.Empty).Trim(), out value) ? value : 0;
        }
    }
}
