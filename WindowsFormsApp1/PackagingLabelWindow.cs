using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class PackagingLabelWindow : Form
    {
        private ComboBox cboSourceItem;
        private ComboBox cboPackagedItem;
        private CheckBox chkUseExistingPackagedItem;
        private TextBox txtSourceSearch;
        private TextBox txtPackagedSearch;
        private TextBox txtSourceQty;
        private TextBox txtPacketSizeQty;
        private TextBox txtPacketSizeLabel;
        private TextBox txtPacketsCreated;
        private TextBox txtPackagedName;
        private TextBox txtRetailPrice;
        private TextBox txtBusinessRegistration;
        private TextBox txtNote;
        private DateTimePicker dtpExpiry;
        private CheckBox chkNoExpiry;
        private DataGridView gridBatches;
        private Label lblExpectedPackets;
        private Label lblSourceStock;
        private PictureBox picPreview;

        private DataTable inventoryTable;

        public PackagingLabelWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Packaging / Label Printing";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ClientSize = new Size(1106, 729);

            Label title = MakeLabel("Packaging / Label Printing", 24, 18, 360, 28, true);
            Controls.Add(title);

            Button btnBack = MakeButton("Back", 970, 18, 100, 40);
            btnBack.Click += BtnBack_Click;
            Controls.Add(btnBack);

            int left = 35;
            int y = 72;

            Controls.Add(MakeLabel("Search source", left, y, 150, 24, false));
            txtSourceSearch = MakeText(left, y + 27, 360, 40);
            txtSourceSearch.TextChanged += (sender, args) => FilterInventoryCombo(cboSourceItem, txtSourceSearch.Text);
            Controls.Add(txtSourceSearch);

            Controls.Add(MakeLabel("Source item", left, y + 118, 150, 24, false));
            cboSourceItem = MakeCombo(left, y + 73, 360, 40);
            cboSourceItem.SelectedIndexChanged += CboSourceItem_SelectedIndexChanged;
            Controls.Add(cboSourceItem);

            Controls.Add(MakeLabel("Source qty", left + 385, y, 120, 22, false));
            txtSourceQty = MakeText(left + 385, y + 27, 120, 40);
            txtSourceQty.TextChanged += RecalculateExpectedPackets;
            Controls.Add(txtSourceQty);

            Controls.Add(MakeLabel("Pack qty", left + 525, y, 100, 22, false));
            txtPacketSizeQty = MakeText(left + 525, y + 27, 120, 40);
            txtPacketSizeQty.TextChanged += RecalculateExpectedPackets;
            Controls.Add(txtPacketSizeQty);

            Controls.Add(MakeLabel("Label text", left + 665, y, 110, 22, false));
            txtPacketSizeLabel = MakeText(left + 665, y + 27, 130, 40);
            txtPacketSizeLabel.Text = "100g";
            txtPacketSizeLabel.TextChanged += UpdatePreview;
            Controls.Add(txtPacketSizeLabel);

            Controls.Add(MakeLabel("Labels", left + 815, y, 100, 22, false));
            txtPacketsCreated = MakeText(left + 815, y + 27, 110, 40);
            txtPacketsCreated.TextChanged += UpdatePreview;
            Controls.Add(txtPacketsCreated);

            lblExpectedPackets = MakeLabel("Expected: -", left, 190, 300, 24, false);
            Controls.Add(lblExpectedPackets);
            lblSourceStock = MakeLabel("Stock: -", left + 385, 190, 260, 24, false);
            Controls.Add(lblSourceStock);

            chkUseExistingPackagedItem = new CheckBox
            {
                Text = "Existing packaged item",
                Location = new Point(left, 226),
                Size = new Size(280, 32),
                Font = new Font("Microsoft Sans Serif", 12)
            };
            chkUseExistingPackagedItem.CheckedChanged += ChkUseExistingPackagedItem_CheckedChanged;
            Controls.Add(chkUseExistingPackagedItem);

            Controls.Add(MakeLabel("Search packaged", left, 264, 180, 24, false));
            txtPackagedSearch = MakeText(left, 291, 360, 40);
            txtPackagedSearch.Enabled = false;
            txtPackagedSearch.TextChanged += (sender, args) => FilterInventoryCombo(cboPackagedItem, txtPackagedSearch.Text);
            Controls.Add(txtPackagedSearch);

            Controls.Add(MakeLabel("Packaged item", left, 336, 180, 24, false));
            cboPackagedItem = MakeCombo(left, 363, 360, 40);
            cboPackagedItem.Enabled = false;
            cboPackagedItem.SelectedIndexChanged += UpdatePreview;
            Controls.Add(cboPackagedItem);

            Controls.Add(MakeLabel("New item name", left + 385, 226, 180, 24, false));
            txtPackagedName = MakeText(left + 385, 253, 310, 40);
            txtPackagedName.TextChanged += UpdatePreview;
            Controls.Add(txtPackagedName);

            Controls.Add(MakeLabel("Price", left + 715, 226, 80, 24, false));
            txtRetailPrice = MakeText(left + 715, 253, 120, 40);
            Controls.Add(txtRetailPrice);

            Controls.Add(MakeLabel("Expiry", left + 385, 316, 90, 24, false));
            dtpExpiry = new DateTimePicker
            {
                Location = new Point(left + 385, 343),
                Size = new Size(180, 40),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Microsoft Sans Serif", 12)
            };
            dtpExpiry.ValueChanged += UpdatePreview;
            Controls.Add(dtpExpiry);

            chkNoExpiry = new CheckBox
            {
                Text = "No expiry",
                Location = new Point(left + 580, 347),
                Size = new Size(130, 32),
                Font = new Font("Microsoft Sans Serif", 12)
            };
            chkNoExpiry.CheckedChanged += UpdatePreview;
            Controls.Add(chkNoExpiry);

            Controls.Add(MakeLabel("Business reg", left + 385, 402, 140, 24, false));
            txtBusinessRegistration = MakeText(left + 385, 429, 255, 40);
            txtBusinessRegistration.Text = "Business Reg: ";
            txtBusinessRegistration.TextChanged += UpdatePreview;
            Controls.Add(txtBusinessRegistration);

            Controls.Add(MakeLabel("Note", left + 660, 402, 90, 24, false));
            txtNote = MakeText(left + 660, 429, 200, 40);
            Controls.Add(txtNote);

            Button btnTest = MakeButton("Test Label", left + 880, 402, 110, 50);
            btnTest.Click += BtnTest_Click;
            Controls.Add(btnTest);

            Button btnCreate = MakeButton("Pack && Print", left + 880, 459, 150, 52);
            btnCreate.BackColor = Color.SeaGreen;
            btnCreate.ForeColor = Color.White;
            btnCreate.Click += BtnCreate_Click;
            Controls.Add(btnCreate);

            Controls.Add(MakeLabel("Preview", left, 430, 140, 26, true));
            picPreview = new PictureBox
            {
                Location = new Point(left, 462),
                Size = new Size(330, 155),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            Controls.Add(picPreview);

            Controls.Add(MakeLabel("Recent batches", left + 385, 522, 220, 26, true));
            gridBatches = new DataGridView
            {
                Location = new Point(left + 385, 554),
                Size = new Size(650, 100),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Controls.Add(gridBatches);

            Button btnRefresh = MakeButton("Refresh", left + 785, 665, 110, 46);
            btnRefresh.Click += (sender, args) => LoadData();
            Controls.Add(btnRefresh);

            Button btnReprint = MakeButton("Reprint", left + 915, 665, 110, 46);
            btnReprint.Click += BtnReprint_Click;
            Controls.Add(btnReprint);
        }

        private void LoadData()
        {
            try
            {
                inventoryTable = PackagingManager.LoadInventoryItems();
                BindInventoryCombo(cboSourceItem, inventoryTable.Copy());
                BindInventoryCombo(cboPackagedItem, inventoryTable.Copy());
                gridBatches.DataSource = PackagingManager.LoadRecentBatches();
                FormatBatchGrid();
                UpdateSourceStock();
                UpdatePreview(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load packaging data: " + ex.Message, "Packaging", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindInventoryCombo(ComboBox combo, DataTable table)
        {
            combo.DataSource = table;
            combo.DisplayMember = "item_name";
            combo.ValueMember = "id";
            combo.SelectedIndex = table.Rows.Count > 0 ? 0 : -1;
        }

        private void FilterInventoryCombo(ComboBox combo, string searchText)
        {
            if (inventoryTable == null || combo == null)
            {
                return;
            }

            string search = (searchText ?? string.Empty).Trim();
            if (search.Length == 0)
            {
                BindInventoryCombo(combo, inventoryTable.Copy());
                if (combo == cboSourceItem)
                {
                    UpdateSourceStock();
                }
                UpdatePreview(null, EventArgs.Empty);
                return;
            }

            DataTable filtered = inventoryTable.Clone();
            var rowsById = inventoryTable.Rows
                .Cast<DataRow>()
                .Where(row => row["id"] != DBNull.Value)
                .GroupBy(row => Convert.ToInt32(row["id"]))
                .ToDictionary(group => group.Key, group => group.First());

            foreach (InventoryItem item in InventorySearch.GetMatches(search, 20))
            {
                DataRow row;
                if (rowsById.TryGetValue(item.Id, out row))
                {
                    filtered.ImportRow(row);
                }
            }

            BindInventoryCombo(combo, filtered);
            if (combo == cboSourceItem)
            {
                UpdateSourceStock();
            }
            UpdatePreview(null, EventArgs.Empty);
        }

        private void CboSourceItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSourceStock();
        }

        private void UpdateSourceStock()
        {
            if (lblSourceStock == null || cboSourceItem == null || !(cboSourceItem.SelectedItem is DataRowView row))
            {
                return;
            }

            decimal amount = row["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["amount"]);
            lblSourceStock.Text = "Stock: " + amount.ToString("N4");
        }

        private void FormatBatchGrid()
        {
            if (gridBatches.Columns["id"] != null) gridBatches.Columns["id"].HeaderText = "Batch";
            if (gridBatches.Columns["packaged_item_name"] != null) gridBatches.Columns["packaged_item_name"].HeaderText = "Product";
            if (gridBatches.Columns["packet_size_label"] != null) gridBatches.Columns["packet_size_label"].HeaderText = "Qty";
            if (gridBatches.Columns["packets_created"] != null) gridBatches.Columns["packets_created"].HeaderText = "Labels";
            if (gridBatches.Columns["expiry_date"] != null) gridBatches.Columns["expiry_date"].HeaderText = "Expiry";
            if (gridBatches.Columns["barcode"] != null) gridBatches.Columns["barcode"].HeaderText = "Barcode";
            if (gridBatches.Columns["created_at"] != null) gridBatches.Columns["created_at"].HeaderText = "Created";
        }

        private void ChkUseExistingPackagedItem_CheckedChanged(object sender, EventArgs e)
        {
            bool useExisting = chkUseExistingPackagedItem.Checked;
            txtPackagedSearch.Enabled = useExisting;
            cboPackagedItem.Enabled = useExisting;
            txtPackagedName.Enabled = !useExisting;
            txtRetailPrice.Enabled = !useExisting;
            UpdatePreview(sender, e);
        }

        private void RecalculateExpectedPackets(object sender, EventArgs e)
        {
            decimal sourceQty;
            decimal packetSize;
            if (TryGetDecimal(txtSourceQty.Text, out sourceQty) && TryGetDecimal(txtPacketSizeQty.Text, out packetSize) && packetSize > 0)
            {
                decimal expected = sourceQty / packetSize;
                lblExpectedPackets.Text = "Expected: " + expected.ToString("N2") + " packets";
                if (expected > 0 && expected <= int.MaxValue && Math.Abs(expected - Math.Round(expected)) < 0.0001m)
                {
                    txtPacketsCreated.TextChanged -= UpdatePreview;
                    txtPacketsCreated.Text = ((int)Math.Round(expected)).ToString(CultureInfo.InvariantCulture);
                    txtPacketsCreated.TextChanged += UpdatePreview;
                }
            }
            else
            {
                lblExpectedPackets.Text = "Expected: -";
            }

            UpdatePreview(sender, e);
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            try
            {
                Image old = picPreview.Image;
                picPreview.Image = LabelPrinter.BuildLabelBitmap(BuildPreviewBatch());
                old?.Dispose();
            }
            catch
            {
                // Preview is best-effort while the user is typing.
            }
        }

        private PackagingBatchResult BuildPreviewBatch()
        {
            string productName = GetPreviewProductName();
            return new PackagingBatchResult
            {
                ProductName = string.IsNullOrWhiteSpace(productName) ? "PRODUCT NAME" : productName,
                PacketSizeLabel = string.IsNullOrWhiteSpace(txtPacketSizeLabel.Text) ? "Qty" : txtPacketSizeLabel.Text.Trim(),
                ExpiryDate = chkNoExpiry.Checked ? (DateTime?)null : dtpExpiry.Value.Date,
                Barcode = "PK00000000",
                PacketsCreated = 1,
                BusinessRegistrationText = txtBusinessRegistration.Text.Trim()
            };
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            LabelPrinter.ShowPreviewAndPrint(BuildPreviewBatch());
        }

        private async void BtnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                PackagingRequest request = BuildRequestFromInput();
                DialogResult confirm = MessageBox.Show(
                    "This will deduct source stock, add packaged stock, save a packaging batch, and then open label printing. Continue?",
                    "Confirm Packaging",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                PackagingBatchResult result = null;
                Enabled = false;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    result = PackagingManager.CreatePackagingBatch(request);
                });
                Enabled = true;

                LoadData();
                MessageBox.Show("Packaging batch " + result.BatchId + " saved.", "Packaging", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LabelPrinter.ShowPreviewAndPrint(result);
            }
            catch (Exception ex)
            {
                Enabled = true;
                MessageBox.Show("Packaging failed: " + ex.Message, "Packaging", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReprint_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridBatches.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Select a packaging batch first.", "Reprint Labels", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                long batchId = Convert.ToInt64(gridBatches.SelectedRows[0].Cells["id"].Value);
                PackagingBatchResult batch = PackagingManager.GetBatchForPrint(batchId);
                if (batch == null)
                {
                    MessageBox.Show("Packaging batch was not found.", "Reprint Labels", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LabelPrinter.ShowPreviewAndPrint(batch);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reprint failed: " + ex.Message, "Reprint Labels", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private PackagingRequest BuildRequestFromInput()
        {
            decimal sourceQty;
            decimal packetSize;
            int packetsCreated;
            decimal retailPrice = 0m;

            if (!TryGetDecimal(txtSourceQty.Text, out sourceQty) || sourceQty <= 0)
                throw new InvalidOperationException("Enter a valid source quantity.");
            if (!TryGetDecimal(txtPacketSizeQty.Text, out packetSize) || packetSize <= 0)
                throw new InvalidOperationException("Enter a valid packet size quantity.");
            if (!int.TryParse(txtPacketsCreated.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out packetsCreated) || packetsCreated <= 0)
                throw new InvalidOperationException("Enter a valid packets/labels count.");

            int? packagedItemId = null;
            string packagedName = null;

            if (chkUseExistingPackagedItem.Checked)
            {
                packagedItemId = GetSelectedId(cboPackagedItem);
                if (!packagedItemId.HasValue)
                    throw new InvalidOperationException("Select an existing packaged item.");
            }
            else
            {
                packagedName = txtPackagedName.Text.Trim();
                if (string.IsNullOrWhiteSpace(packagedName))
                    throw new InvalidOperationException("Enter a new packaged item name.");
                if (!TryGetDecimal(txtRetailPrice.Text, out retailPrice) || retailPrice < 0)
                    throw new InvalidOperationException("Enter a valid retail price.");
            }

            return new PackagingRequest
            {
                SourceItemId = GetSelectedId(cboSourceItem) ?? 0,
                SourceQtyUsed = sourceQty,
                PackagedItemId = packagedItemId,
                PackagedItemName = packagedName,
                PackagedRetailPrice = retailPrice,
                PacketSizeQty = packetSize,
                PacketSizeLabel = txtPacketSizeLabel.Text.Trim(),
                PacketsCreated = packetsCreated,
                ExpiryDate = chkNoExpiry.Checked ? (DateTime?)null : dtpExpiry.Value.Date,
                BusinessRegistrationText = txtBusinessRegistration.Text.Trim(),
                Note = txtNote.Text
            };
        }

        private string GetPreviewProductName()
        {
            if (chkUseExistingPackagedItem.Checked && cboPackagedItem.SelectedItem is DataRowView row)
            {
                return row["item_name"].ToString();
            }

            return txtPackagedName.Text;
        }

        private int? GetSelectedId(ComboBox combo)
        {
            if (combo.SelectedValue == null) return null;
            if (combo.SelectedValue is int) return (int)combo.SelectedValue;
            int id;
            return int.TryParse(combo.SelectedValue.ToString(), out id) ? (int?)id : null;
        }

        private bool TryGetDecimal(string text, out decimal value)
        {
            text = (text ?? string.Empty).Trim();
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            Home home = Application.OpenForms.OfType<Home>().FirstOrDefault() ?? new Home();
            home.Show();
            Hide();
        }

        private Label MakeLabel(string text, int x, int y, int width, int height, bool bold)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Microsoft Sans Serif", bold ? 14 : 12, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        private TextBox MakeText(int x, int y, int width, int height)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Microsoft Sans Serif", 12)
            };
        }

        private ComboBox MakeCombo(int x, int y, int width, int height)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Microsoft Sans Serif", 12),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
        }

        private Button MakeButton(string text, int x, int y, int width, int height)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Microsoft Sans Serif", 11, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };
        }
    }
}
