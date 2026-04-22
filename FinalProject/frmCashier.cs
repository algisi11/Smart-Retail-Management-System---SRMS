using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace FinalProject
{
    public partial class frmCashier : MaterialForm
    {
        private DataTable _reprintTable;
        private string r_InvNo, r_Date, r_Cashier, r_Total;
        DataTable _cartTable;
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        public frmCashier()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            AdjustBottomPanelSpacing();
            ToolTip btnToolTip = new ToolTip();

            // 2. إعدادات الوقت (أهم جزء لتطبيق فكرة "الوقوف المطول")
            btnToolTip.InitialDelay = 800;  // الانتظار 800 جزء من الثانية (أقل من ثانية بقليل) قبل ظهور التلميح
            btnToolTip.AutoPopDelay = 5000; // مدة بقاء التلميح ظاهراً (5 ثوانٍ)
            btnToolTip.ReshowDelay = 200;   // سرعة ظهوره عند الانتقال لزر آخر مباشرة
            btnToolTip.ShowAlways = true;   // إظهاره حتى لو لم تكن الشاشة هي النشطة تماماً

            // 3. ربط التلميحات بالأزرار
            // (قم بتغيير أسماء الأزرار لأسماء أزرارك الحقيقية)
            btnToolTip.SetToolTip(btnCheckout, "حفظ الفاتورة (Ctrl + S)");
            btnToolTip.SetToolTip(btnReprint, "طباعة الفاتورة (Ctrl + P)");
            btnToolTip.SetToolTip(btnCancelOrder, "تفريغ الشاشة لطلب جديد (Ctrl + N)");
            btnToolTip.SetToolTip(btnCheckout, "إتمام الدفع (F12)");
            btnToolTip.SetToolTip(btnRemoveItem, "حذف منتج واحد (Ctrl + D)");

        }

        private void StartNewInvoice()
        {
            _cartTable.Clear();
            lblTotal.Text = "0.00";
            lblInvoiceNo.Text = GetNextInvoiceNumber();
            txtBarcode.Clear();
            txtBarcode.Focus();
        }
        SmartRecommender aiRecommender = new SmartRecommender();
        private string GetNextInvoiceNumber()
        {
            string nextNumber = "INV-0001";
            string query = "SELECT ISNULL(MAX(InvoiceID), 0) + 1 FROM SalesInvoices";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    int nextId = Convert.ToInt32(cmd.ExecuteScalar());
                    nextNumber = "INV-" + nextId.ToString("D4");
                }
                catch (Exception)
                {
                }
            }
            return nextNumber;
        }
        private void CheckDayStatus()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    // نسأل: كم عدد الشفتات المفتوحة حالياً؟
                    string query = "SELECT COUNT(*) FROM DailyClosures WHERE IsClosed = 0";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    int openShifts = Convert.ToInt32(cmd.ExecuteScalar());

                    if (openShifts > 0)
                    {
                        // يوجد كاش مفتوح -> تفعيل البيع
                        btnCheckout.Enabled = true;
                        btnOpenDa.Enabled = false;
                        btnCloseDay.Enabled = true;
                        txtBarcode.Enabled = true;

                        // إذا كان لديك Label يعرض الحالة
                        // lblStatus.Text = "الكاش مفتوح"; 
                    }
                    else
                    {
                        // لا يوجد كاش مفتوح -> تعطيل البيع
                        btnCheckout.Enabled = false;
                        btnOpenDa.Enabled = true;
                        btnCloseDay.Enabled = false;
                        txtBarcode.Enabled = false;

                        // lblStatus.Text = "الكاش مغلق";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error checking day status: " + ex.Message);
                }
            }
        }
        private async void frmCashier_Load(object sender, EventArgs e)
        {
            if (SessionManager.Role == "Admin")
            {
               this.FormStyle = MaterialSkin.Controls.MaterialForm.FormStyles.StatusAndActionBar_None;
            }
            await System.Threading.Tasks.Task.Run(() => aiRecommender.TrainModel());
            bool isAdmin = (SessionManager.Role == "Admin");
            btnOpenDa.Visible = isAdmin;
            btnCloseDay.Visible = isAdmin;
            CheckDayStatus();

            lblEmp.Text = SessionManager.EmployeeName;

            _cartTable = new DataTable();
            _cartTable.Columns.Add("ProductID", typeof(int));
            _cartTable.Columns.Add("Barcode", typeof(string));
            _cartTable.Columns.Add("Product Name", typeof(string));
            _cartTable.Columns.Add("Price", typeof(decimal));
            _cartTable.Columns.Add("Quantity", typeof(int));
            _cartTable.Columns.Add("Total", typeof(decimal));

            dgvCart.DataSource = _cartTable;
            if (dgvCart.Columns.Contains("ProductID")) dgvCart.Columns["ProductID"].Visible = false;
            dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            StartNewInvoice();
        }

        private void timerClock_Tick(object sender, EventArgs e)
        {
            lblDateTime.Text = DateTime.Now.ToString("yyyy/MM/dd  hh:mm:ss tt");
        }
        private async void ShowAIRecommendation(int currentProductId)
        {
            try
            {
                // 1. طلب التوصية من محرك الذكاء الاصطناعي في الخلفية
                int recommendedId = await Task.Run(() => aiRecommender.GetBestRecommendation(currentProductId));

                // 2. إذا وجد الذكاء الاصطناعي منتجاً مرتبطة بقوة
                if (recommendedId != -1)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        string q = "SELECT ProductName FROM Products WHERE ProductID = @id";
                        SqlCommand cmdRec = new SqlCommand(q, conn);
                        cmdRec.Parameters.AddWithValue("@id", recommendedId);
                        await conn.OpenAsync();
                        object recName = await cmdRec.ExecuteScalarAsync();

                        if (recName != null)
                        {
                            // تحديث النص ليظهر الاقتراح للكاشير
                        }
                    }
                }
                else
                {
                    // في حال لا توجد توصية قوية لهذا المنتج حالياً
                }
            }
            catch
            {
                // نتجاهل الأخطاء هنا لكي لا يتوقف نظام البيع إذا تعطل الذكاء الاصطناعي
            }
        }



        private void AdjustBottomPanelSpacing()
        {
            // 1. إعدادات نقطة البداية والمسافات
            int startX = 20; // البداية من أقصى اليسار
            int standardSpacing = 15; // المسافة الطبيعية بين العناصر
            int smallSpacing = 5; // المسافة بين الكلمة والرقم التابع لها

            // الارتفاع العمودي للأدوات (عدل هذا الرقم إذا أردت رفعهم أو تنزيلهم بالكامل)
            int currentY = 5;

            // 2. ترتيب العناصر بالتسلسل الهندسي الصحيح من اليسار لليمين

            // كلمة "Discount" الأولى
            label2.Location = new Point(startX, currentY);
            startX += label2.Width + standardSpacing;

            // مربع إدخال الخصم (مرفوع قليلاً ليتوازى خطياً مع النص)
            nudDiscount.Location = new Point(startX, currentY - 2);
            startX += nudDiscount.Width + standardSpacing;

            // خيار "Percent %"
            rbPercent.Location = new Point(startX, currentY);
            startX += rbPercent.Width + standardSpacing;

            // خيار "Amount $"
            rbAmount.Location = new Point(startX, currentY);
            startX += rbAmount.Width + (standardSpacing * 3); // 🌟 مسافة ثلاثية لفصل قسم إدخال الخصم عن المجاميع

            // نص "Discount" (الخاص بعرض القيمة المخصومة)
            label5.Location = new Point(startX, currentY);
            startX += label5.Width + smallSpacing;

            // رقم الخصم الفعلي (مثلاً 0.00)
            lblDiscountAmount.Location = new Point(startX, currentY);
            startX += lblDiscountAmount.Width + standardSpacing;

            // نص "Sub Total"
            label6.Location = new Point(startX, currentY);
            startX += label6.Width + smallSpacing;

            // رقم المجموع الفرعي (مثلاً 0.00)
            lblSubTotal.Location = new Point(startX, currentY);
            startX += lblSubTotal.Width + (standardSpacing * 4); // 🌟 مسافة كبيرة جداً لفصل المجموع النهائي الكبير

            // الرقم النهائي الكبير (الإجمالي)
            lblTotal.Location = new Point(startX, currentY - 5); // تم رفعه قليلاً في حال كان خطه كبيراً جداً
        }



        private async void TriggerSmartRecommendation()
        {
            try
            {
                List<int> currentCart = new List<int>();
                foreach (DataRow row in _cartTable.Rows)
                {
                    currentCart.Add(Convert.ToInt32(row["ProductID"]));
                }

                if (currentCart.Count == 0) return;

                // العمل بالخلفية لكي لا يتجمد الكاشير أبداً
                List<int> recommendedIds = await Task.Run(() => aiRecommender.GetCartRecommendations(currentCart, 1));

                if (recommendedIds.Count > 0)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string qName = "SELECT ProductName FROM Products WHERE ProductID = @PID";
                        using (SqlCommand cmd = new SqlCommand(qName, conn))
                        {
                            cmd.Parameters.AddWithValue("@PID", recommendedIds[0]);
                            object result = cmd.ExecuteScalar();

                            if (result != null)
                            {
                                string suggestionMessage = "💡 اقتراح ذكي: جرب عرض (" + result.ToString() + ")";

                                // عرض الإشعار المنزلق الأنيق
                                frmToast toast = new frmToast(suggestionMessage);
                                toast.Show();
                            }
                        }
                    }
                }
            }
            catch { } // نتجاهل الأخطاء بصمت
        }
        private void AddToCart(string barcode)
        {
            // 🛡️ درع 1: تجاهل إذا كان مربع النص فارغاً لتخفيف العبء
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            // 🛡️ درع 2: منع الانهيار إذا نسي المبرمج تهيئة الجدول
            if (_cartTable == null)
            {
                MessageBox.Show("خطأ تقني: جدول السلة غير مهيأ. يرجى التأكد من بناء _cartTable في الـ Form_Load.", "تنبيه هندسي", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool itemFound = false;

            foreach (DataRow row in _cartTable.Rows)
            {
                // 🛡️ درع 3: استخدام Convert.ToString بدلاً من .ToString() لأنها آمنة ضد الـ Null
                if (Convert.ToString(row["Barcode"]) == barcode)
                {
                    int newQty = Convert.ToInt32(row["Quantity"]) + 1;
                    decimal price = Convert.ToDecimal(row["Price"]);
                    row["Quantity"] = newQty;
                    row["Total"] = newQty * price;
                    itemFound = true;
                    break;
                }
            }

            if (!itemFound)
            {
                string query = "SELECT ProductID, ProductName, SellingPrice FROM Products WHERE Barcode = @Barcode";

                // 🛡️ يفضل دائماً استخدام using مع الـ SqlConnection لضمان إغلاق الاتصال حتى لو حدث خطأ
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int id = Convert.ToInt32(reader["ProductID"]);
                                string name = reader["ProductName"].ToString();
                                decimal price = Convert.ToDecimal(reader["SellingPrice"]);

                                // إضافة الصف الجديد
                                _cartTable.Rows.Add(id, barcode, name, price, 1, price);
                            }
                            else
                            {
                                MessageBox.Show("هذا الباركود غير مسجل في النظام!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                ResetBarcodeScanner();
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("حدث خطأ أثناء الاتصال بقاعدة البيانات: \n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetBarcodeScanner();
                        return;
                    }
                } // سيتم إغلاق الـ connection تلقائياً هنا بفضل الـ using
            }

            // تحديث السعر وتفريغ مربع النص
            UpdateGrandTotal();
            ResetBarcodeScanner();

            // إطلاق الذكاء الاصطناعي لفحص السلة كاملة وعرض الـ Toast
            TriggerSmartRecommendation();
        }
        private void UpdateGrandTotal()
        {
            decimal subTotal = 0;

            // 1. حساب المجموع الفرعي (قبل الخصم) من جدول السلة
            foreach (DataRow row in _cartTable.Rows)
            {
                subTotal += Convert.ToDecimal(row["Total"]);
            }

            // عرض المجموع الفرعي
            lblSubTotal.Text = subTotal.ToString("0.00");

            // 2. حساب قيمة الخصم بناءً على اختيار الكاشير
            decimal discountInputValue = nudDiscount.Value;
            decimal discountAmount = 0;

            if (rbPercent.Checked) // حالة: خصم نسبة مئوية
            {
                discountAmount = subTotal * (discountInputValue / 100);
            }
            else // حالة: خصم مبلغ ثابت
            {
                discountAmount = discountInputValue;
            }

            // 🛡️ حماية النظام: منع الكاشير من عمل خصم أكبر من قيمة الفاتورة
            if (discountAmount > subTotal)
            {
                discountAmount = subTotal;
                nudDiscount.Value = subTotal; // إعادة الرقم في العداد للحد الأقصى المسموح
                MessageBox.Show("لا يمكن أن تكون قيمة الخصم أكبر من إجمالي الفاتورة!", "تنبيه أمني", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // 3. حساب الصافي النهائي بعد الخصم
            decimal finalTotal = subTotal - discountAmount;

            // 4. عرض النتائج النهائية على الشاشة
            lblDiscountAmount.Text = discountAmount.ToString("0.00");
            lblTotal.Text = finalTotal.ToString("0.00"); // 👈 هذا هو الرقم الذي سيُحفظ في الداتا بيس
        }

        private void ResetBarcodeScanner()
        {
            txtBarcode.Clear();
            txtBarcode.Focus();
        }

        private void txtBarcode_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string scannedBarcode = txtBarcode.Text.Trim();

                if (!string.IsNullOrEmpty(scannedBarcode))
                {
                    AddToCart(scannedBarcode);
                }
            }

        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            btnCheckout.Enabled = false;

            if (_cartTable.Rows.Count == 0 || !decimal.TryParse(lblTotal.Text, out decimal grandTotal) || grandTotal < 0)
            {
                MessageBox.Show("The invoice is empty or total is invalid. Please scan products first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnCheckout.Enabled = true;
                return;
            }

            Form popup = new Form()
            {
                Width = 450,
                Height = 400, // 👈 زيادة ارتفاع الشاشة لتتسع للحقل الجديد
                Text = "Checkout - " + lblInvoiceNo.Text,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.WhiteSmoke
            };

            decimal.TryParse(lblDiscountAmount.Text, out decimal discountAmount);

            Label lblDiscountInfo = new Label() { Text = "Discount Applied: " + discountAmount.ToString("0.00") + " JD", Location = new Point(20, 15), AutoSize = true, Font = new Font("Arial", 12, FontStyle.Italic), ForeColor = Color.DarkOrange };
            Label lblTotalReq = new Label() { Text = "Net Total: " + grandTotal.ToString("0.00") + " JD", Location = new Point(20, 45), AutoSize = true, Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.Blue };

            Label lblType = new Label() { Text = "Payment Method:", Location = new Point(20, 95), AutoSize = true, Font = new Font("Arial", 12) };
            ComboBox cmbType = new ComboBox() { Location = new Point(170, 93), Width = 220, Font = new Font("Arial", 12), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.Add("Cash");
            cmbType.Items.Add("Card (Visa / Mastercard)");
            cmbType.Items.Add("Split (Cash & Card)"); // 👈 إضافة الخيار الثالث
            cmbType.SelectedIndex = 0;

            Label lblPaid = new Label() { Text = "Cash Received:", Location = new Point(20, 145), AutoSize = true, Font = new Font("Arial", 12) };
            TextBox txtPaid = new TextBox() { Location = new Point(170, 143), Width = 220, Font = new Font("Arial", 14) };

            // 👈 إضافة حقل جديد للفيزا (يُحسب تلقائياً)
            Label lblCard = new Label() { Text = "Card Amount:", Location = new Point(20, 195), AutoSize = true, Font = new Font("Arial", 12) };
            TextBox txtCard = new TextBox() { Location = new Point(170, 193), Width = 220, Font = new Font("Arial", 14), ReadOnly = true, BackColor = Color.LightGray };

            Label lblChange = new Label() { Text = "Change: 0.00", Location = new Point(20, 245), AutoSize = true, Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.ForestGreen };

            Button btnConfirm = new Button() { Text = "Confirm & Print", Location = new Point(75, 300), Width = 280, Height = 50, Font = new Font("Arial", 14, FontStyle.Bold), BackColor = Color.LightGreen, Enabled = false, DialogResult = DialogResult.OK };

            // هندسة الأحداث (المنطق الرياضي)
            txtPaid.TextChanged += (s, ev) => {
                if (!decimal.TryParse(txtPaid.Text, out decimal cashAmount)) cashAmount = 0;

                if (cmbType.SelectedIndex == 0) // حالة الدفع كاش فقط
                {
                    decimal change = cashAmount - grandTotal;
                    lblChange.Text = change >= 0 ? "Change: " + change.ToString("0.00") + " JD" : "Insufficient amount!";
                    lblChange.ForeColor = change >= 0 ? Color.ForestGreen : Color.Red;
                    btnConfirm.Enabled = change >= 0;
                }
                else if (cmbType.SelectedIndex == 2) // حالة الدفع المقسم
                {
                    decimal cardNeeded = grandTotal - cashAmount;

                    if (cashAmount > grandTotal) // منع إدخال كاش أكبر من الفاتورة في حالة التقسيم
                    {
                        txtCard.Text = "0.00";
                        lblChange.Text = "Invalid Split!";
                        lblChange.ForeColor = Color.Red;
                        btnConfirm.Enabled = false;
                    }
                    else
                    {
                        txtCard.Text = cardNeeded.ToString("0.00");
                        lblChange.Text = "Balance to Card";
                        lblChange.ForeColor = Color.Blue;
                        btnConfirm.Enabled = true;
                    }
                }
            };

            cmbType.SelectedIndexChanged += (s, ev) => {
                if (cmbType.SelectedIndex == 0) // Cash
                {
                    txtPaid.Enabled = true; txtPaid.Text = ""; txtCard.Text = "0.00"; lblChange.Visible = true; btnConfirm.Enabled = false;
                }
                else if (cmbType.SelectedIndex == 1) // Card
                {
                    txtPaid.Enabled = false; txtPaid.Text = "0.00"; txtCard.Text = grandTotal.ToString("0.00");
                    lblChange.Text = "Paid via Card"; lblChange.ForeColor = Color.ForestGreen; btnConfirm.Enabled = true;
                }
                else // Split
                {
                    txtPaid.Enabled = true; txtPaid.Text = ""; txtCard.Text = grandTotal.ToString("0.00");
                    lblChange.Text = "Enter Cash Amount"; lblChange.ForeColor = Color.DarkOrange; btnConfirm.Enabled = false;
                }
            };

            popup.Controls.AddRange(new Control[] { lblDiscountInfo, lblTotalReq, lblType, cmbType, lblPaid, txtPaid, lblCard, txtCard, lblChange, btnConfirm });

            if (popup.ShowDialog() == DialogResult.OK)
            {
                // 👈 صياغة طريقة الدفع النهائية للحفظ في الداتا بيس
                string finalPaymentMethod = cmbType.Text;
                if (cmbType.SelectedIndex == 2)
                {
                    // إذا كان مقسماً، سيحفظ القيمة هكذا: Split (Cash: 20, Card: 30)
                    finalPaymentMethod = $"Split (Cash: {txtPaid.Text}, Card: {txtCard.Text})";
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        string insertInvoiceQuery = "INSERT INTO SalesInvoices (InvoiceNo, CashierName, TotalAmount, PaymentMethod) VALUES (@InvNo, @Emp, @Total, @PayMethod); SELECT SCOPE_IDENTITY();";
                        SqlCommand cmdInvoice = new SqlCommand(insertInvoiceQuery, conn, transaction);
                        cmdInvoice.Parameters.AddWithValue("@InvNo", lblInvoiceNo.Text);
                        cmdInvoice.Parameters.AddWithValue("@Emp", lblEmp.Text);
                        cmdInvoice.Parameters.AddWithValue("@Total", grandTotal);
                        cmdInvoice.Parameters.AddWithValue("@PayMethod", finalPaymentMethod); // استخدام النص المدمج هنا
                        int newInvoiceId = Convert.ToInt32(cmdInvoice.ExecuteScalar());

                        foreach (DataRow row in _cartTable.Rows)
                        {
                            int productId = Convert.ToInt32(row["ProductID"]);
                            int qtyToDeduct = Convert.ToInt32(row["Quantity"]);

                            string insertDetailsQuery = "INSERT INTO SalesInvoiceDetails (InvoiceID, ProductID, Quantity, Price, Total) VALUES (@InvID, @ProdID, @Qty, @Price, @Total)";
                            SqlCommand cmdDetails = new SqlCommand(insertDetailsQuery, conn, transaction);
                            cmdDetails.Parameters.AddWithValue("@InvID", newInvoiceId);
                            cmdDetails.Parameters.AddWithValue("@ProdID", productId);
                            cmdDetails.Parameters.AddWithValue("@Qty", qtyToDeduct);
                            cmdDetails.Parameters.AddWithValue("@Price", row["Price"]);
                            cmdDetails.Parameters.AddWithValue("@Total", row["Total"]);
                            cmdDetails.ExecuteNonQuery();

                            string getBatchesQuery = "SELECT BatchID, Quantity FROM InventoryBatches WHERE ProductID = @ProdID AND Quantity > 0 ORDER BY ReceiveDate ASC";
                            SqlCommand cmdGetBatches = new SqlCommand(getBatchesQuery, conn, transaction);
                            cmdGetBatches.Parameters.AddWithValue("@ProdID", productId);

                            DataTable dtBatches = new DataTable();
                            using (SqlDataAdapter da = new SqlDataAdapter(cmdGetBatches)) da.Fill(dtBatches);

                            foreach (DataRow batchRow in dtBatches.Rows)
                            {
                                if (qtyToDeduct <= 0) break;

                                int batchId = Convert.ToInt32(batchRow["BatchID"]);
                                int currentBatchQty = Convert.ToInt32(batchRow["Quantity"]);

                                if (currentBatchQty <= qtyToDeduct)
                                {
                                    qtyToDeduct -= currentBatchQty;
                                    string updateBatch = "UPDATE InventoryBatches SET Quantity = 0 WHERE BatchID = @BID";
                                    SqlCommand cmdUpd = new SqlCommand(updateBatch, conn, transaction);
                                    cmdUpd.Parameters.AddWithValue("@BID", batchId);
                                    cmdUpd.ExecuteNonQuery();
                                }
                                else
                                {
                                    string updateBatch = "UPDATE InventoryBatches SET Quantity = Quantity - @Deduct WHERE BatchID = @BID";
                                    SqlCommand cmdUpd = new SqlCommand(updateBatch, conn, transaction);
                                    cmdUpd.Parameters.AddWithValue("@Deduct", qtyToDeduct);
                                    cmdUpd.Parameters.AddWithValue("@BID", batchId);
                                    cmdUpd.ExecuteNonQuery();
                                    qtyToDeduct = 0;
                                }
                            }
                            if (qtyToDeduct > 0) throw new Exception($"Not enough stock in batches for Product ID: {productId}");
                        }

                        transaction.Commit();

                        printPreviewDialog1.Document = printDocument1;
                        printPreviewDialog1.WindowState = FormWindowState.Maximized;
                        printPreviewDialog1.ShowDialog();

                        MessageBox.Show($"Payment successful!\nInvoice No: {lblInvoiceNo.Text}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        StartNewInvoice();
                        SystemLogger.LogAction("دفع وحفظ فاتورة مبيعات", "تثبيت الدفع والبيع لفاتورة جديدة");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Error saving invoice: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            btnCheckout.Enabled = true;
        }

        private void btnReprint_Click(object sender, EventArgs e)
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Reprint Invoice",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 25, Text = "Invoice No:" };

            TextBox textBox = new TextBox() { Left = 100, Top = 22, Width = 200 };
            Button confirmation = new Button() { Text = "Search & Print", Left = 100, Width = 200, Top = 70, DialogResult = DialogResult.OK, BackColor = Color.LightBlue };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                string userInput = textBox.Text.Trim();
                if (string.IsNullOrEmpty(userInput)) return;

                userInput = userInput.ToUpper().Replace("INV-", "").Trim();

                string targetInv = "";
                if (int.TryParse(userInput, out int parsedNum))
                {
                    targetInv = "INV-" + parsedNum.ToString("D4");
                }
                else
                {
                    targetInv = textBox.Text.Trim();
                }

                string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        string queryHeader = "SELECT InvoiceID, InvoiceNo, InvoiceDate, CashierName, TotalAmount FROM SalesInvoices WHERE InvoiceNo = @InvNo";
                        SqlCommand cmdHeader = new SqlCommand(queryHeader, conn);
                        cmdHeader.Parameters.AddWithValue("@InvNo", targetInv);

                        int invoiceId = 0;
                        using (SqlDataReader reader = cmdHeader.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                invoiceId = Convert.ToInt32(reader["InvoiceID"]);
                                r_InvNo = reader["InvoiceNo"].ToString();
                                r_Date = Convert.ToDateTime(reader["InvoiceDate"]).ToString("yyyy/MM/dd HH:mm");
                                r_Cashier = reader["CashierName"].ToString();
                                r_Total = reader["TotalAmount"].ToString();
                            }
                        }

                        if (invoiceId == 0)
                        {
                            MessageBox.Show($"Invoice ({targetInv}) not found in the system!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string queryDetails = "SELECT p.ProductName, d.Quantity, d.Total FROM SalesInvoiceDetails d JOIN Products p ON d.ProductID = p.ProductID WHERE d.InvoiceID = @InvID";
                        SqlCommand cmdDetails = new SqlCommand(queryDetails, conn);
                        cmdDetails.Parameters.AddWithValue("@InvID", invoiceId);

                        SqlDataAdapter da = new SqlDataAdapter(cmdDetails);
                        _reprintTable = new DataTable();
                        da.Fill(_reprintTable);

                        System.Drawing.Printing.PrintDocument printDocReprint = new System.Drawing.Printing.PrintDocument();
                        printDocReprint.PrintPage += PrintDocReprint_PrintPage;

                        PrintPreviewDialog previewDialog = new PrintPreviewDialog();
                        previewDialog.Document = printDocReprint;
                        previewDialog.WindowState = FormWindowState.Maximized;
                        previewDialog.ShowDialog();
                        SystemLogger.LogAction("إعادة طباعة فاتورة", "إعادة طباعة فاتورة بنسخة جديدة");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error during reprint: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void PrintDocReprint_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;

            Font fontLogo = new Font("Arial", 24, FontStyle.Bold);
            Font fontTitle = new Font("Arial", 14, FontStyle.Bold);
            Font fontHeader = new Font("Arial", 11, FontStyle.Bold);
            Font fontRegular = new Font("Arial", 10, FontStyle.Regular);
            Brush brush = Brushes.Black;

            StringFormat formatCenter = new StringFormat() { Alignment = StringAlignment.Center };
            StringFormat formatRight = new StringFormat() { Alignment = StringAlignment.Far };

            int startX = 10;
            int startY = 10;
            int offset = 0;
            int printWidth = 280;
            Rectangle rectFull = new Rectangle(startX, startY, printWidth, 50);

            graphics.DrawString("SRMS", fontLogo, brush, rectFull, formatCenter);
            offset += 35;
            rectFull.Y = startY + offset;
            graphics.DrawString("Receipt (REPRINT)", fontTitle, brush, rectFull, formatCenter);
            offset += 30;

            graphics.DrawString("--------------------------------------------------", fontRegular, brush, startX, startY + offset);
            offset += 15;

            graphics.DrawString("Invoice No: " + r_InvNo, fontRegular, brush, startX, startY + offset);
            offset += 20;
            graphics.DrawString("Date: " + r_Date, fontRegular, brush, startX, startY + offset);
            offset += 20;
            graphics.DrawString("Cashier: " + r_Cashier, fontRegular, brush, startX, startY + offset);
            offset += 20;

            graphics.DrawString("--------------------------------------------------", fontRegular, brush, startX, startY + offset);
            offset += 15;

            graphics.DrawString("Item", fontHeader, brush, startX, startY + offset);
            graphics.DrawString("Qty", fontHeader, brush, new Rectangle(startX, startY + offset, 160, 20), formatRight);
            graphics.DrawString("Total", fontHeader, brush, new Rectangle(startX, startY + offset, 270, 20), formatRight);
            offset += 25;

            decimal reprintSubTotal = 0;
            foreach (DataRow row in _reprintTable.Rows)
            {
                string prodName = row["ProductName"].ToString();
                string qty = row["Quantity"].ToString();
                decimal itemTotal = Convert.ToDecimal(row["Total"]);
                reprintSubTotal += itemTotal;

                if (prodName.Length > 16) prodName = prodName.Substring(0, 16);

                graphics.DrawString(prodName, fontRegular, brush, startX, startY + offset);
                graphics.DrawString(qty, fontRegular, brush, new Rectangle(startX, startY + offset, 160, 20), formatRight);
                graphics.DrawString(itemTotal.ToString("0.00"), fontRegular, brush, new Rectangle(startX, startY + offset, 270, 20), formatRight);
                offset += 20;
            }

            graphics.DrawString("--------------------------------------------------", fontRegular, brush, startX, startY + offset);
            offset += 20;

            // استنتاج الخصم: (المجموع الفرعي لجميع العناصر) ناقص (الصافي المخزن في قاعدة البيانات)
            decimal.TryParse(r_Total, out decimal reprintNetTotal);
            decimal reprintDiscount = reprintSubTotal - reprintNetTotal;

            if (reprintDiscount > 0)
            {
                graphics.DrawString("Sub Total:", fontRegular, brush, startX + 50, startY + offset);
                graphics.DrawString(reprintSubTotal.ToString("0.00"), fontRegular, brush, new Rectangle(startX, startY + offset, 270, 20), formatRight);
                offset += 20;

                graphics.DrawString("Discount:", fontRegular, brush, startX + 50, startY + offset);
                graphics.DrawString("-" + reprintDiscount.ToString("0.00"), fontRegular, brush, new Rectangle(startX, startY + offset, 270, 20), formatRight);
                offset += 20;
            }

            graphics.DrawString("NET TOTAL:", fontTitle, brush, startX + 20, startY + offset);
            graphics.DrawString(reprintNetTotal.ToString("0.00"), fontTitle, brush, new Rectangle(startX, startY + offset, 270, 30), formatRight);
            offset += 40;

            rectFull.Y = startY + offset;
            graphics.DrawString("Thank you for visiting!", fontHeader, brush, rectFull, formatCenter);
        }

        private void frmCashier_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (SessionManager.Role == "Admin")
            {
                // إذا كان أدمن: لا تفعل شيئاً.
                // الشاشة ستُغلق طبيعياً وسيعود الأدمن تلقائياً إلى الشاشة الرئيسية (Dashboard) التي فتح منها هذه الشاشة.
            }
            else
            {
                // إذا كان موظف عادي (Cashier): نقوم بإعادة تشغيل التطبيق بالكامل
                // ليتم طرده بأمان والعودة إلى شاشة تسجيل الدخول (Login).
                Application.Restart();
            }

        }

        private void timerClock_Tick_1(object sender, EventArgs e)
        {
            lblDateTime.Text = DateTime.Now.ToString("yyyy/MM/dd  hh:mm:ss tt");
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;

            // 1. تعريف الخطوط العصرية (استخدام Segoe UI ليتماشى مع تصميم البرنامج الجديد)
            Font fontLogo = new Font("Segoe UI", 26, FontStyle.Bold);
            Font fontStoreName = new Font("Segoe UI", 11, FontStyle.Regular);
            Font fontHeader = new Font("Segoe UI", 10, FontStyle.Bold);
            Font fontRegular = new Font("Segoe UI", 9, FontStyle.Regular);
            Font fontTotal = new Font("Segoe UI", 16, FontStyle.Bold);

            // 2. تعريف الفراشي والأقلام العصرية
            Brush solidBlack = Brushes.Black;
            Brush darkGray = new SolidBrush(Color.FromArgb(90, 90, 90)); // رمادي داكن للنصوص الثانوية
            Pen dottedPen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }; // خط منقط للتقسيم
            Pen solidPen = new Pen(Color.Black, 2); // خط عريض سميك للإجمالي

            // 3. أدوات المحاذاة
            StringFormat formatCenter = new StringFormat() { Alignment = StringAlignment.Center };
            StringFormat formatRight = new StringFormat() { Alignment = StringAlignment.Far };

            int startX = 10;
            int startY = 10;
            int offset = 0;
            int printWidth = 270; // تم تقليل العرض قليلاً لتجنب قص الحواف في بعض الطابعات
            Rectangle rectFull = new Rectangle(startX, startY, printWidth, 50);

            // =================================================================
            // 1. الهيدر (الشعار واسم المحل)
            // =================================================================
            graphics.DrawString("S R M S", fontLogo, solidBlack, rectFull, formatCenter);
            offset += 40;
            rectFull.Y = startY + offset;
            graphics.DrawString("Smart Retail Management", fontStoreName, darkGray, rectFull, formatCenter);
            offset += 30;

            // رسم خط منقط احترافي
            graphics.DrawLine(dottedPen, startX, startY + offset, startX + printWidth, startY + offset);
            offset += 15;

            // =================================================================
            // 2. بيانات الفاتورة (موزعة لليسار واليمين بشكل أنيق)
            // =================================================================
            graphics.DrawString("Invoice No:", fontHeader, darkGray, startX, startY + offset);
            graphics.DrawString(lblInvoiceNo.Text, fontHeader, solidBlack, new Rectangle(startX, startY + offset, printWidth, 20), formatRight);
            offset += 20;

            graphics.DrawString("Date:", fontRegular, darkGray, startX, startY + offset);
            graphics.DrawString(DateTime.Now.ToString("dd MMM yyyy HH:mm"), fontRegular, solidBlack, new Rectangle(startX, startY + offset, printWidth, 20), formatRight);
            offset += 20;

            graphics.DrawString("Cashier:", fontRegular, darkGray, startX, startY + offset);
            graphics.DrawString(lblEmp.Text, fontRegular, solidBlack, new Rectangle(startX, startY + offset, printWidth, 20), formatRight);
            offset += 25;

            // =================================================================
            // 3. شريط عناوين الأعمدة (مستطيل أسود ونص أبيض) ✨
            // =================================================================
            Rectangle headerBgRect = new Rectangle(startX, startY + offset, printWidth, 25);
            graphics.FillRectangle(solidBlack, headerBgRect); // تعبئة المستطيل بالأسود

            graphics.DrawString(" Item", fontHeader, Brushes.White, startX, startY + offset + 3);
            graphics.DrawString("Qty", fontHeader, Brushes.White, new Rectangle(startX, startY + offset + 3, 170, 20), formatRight);
            graphics.DrawString("Total", fontHeader, Brushes.White, new Rectangle(startX, startY + offset + 3, printWidth - 5, 20), formatRight);
            offset += 35;

            // =================================================================
            // 4. طباعة المنتجات
            // =================================================================
            decimal subTotal = 0;
            foreach (DataRow row in _cartTable.Rows)
            {
                string prodName = row["Product Name"].ToString();
                string qty = row["Quantity"].ToString();
                decimal itemTotal = Convert.ToDecimal(row["Total"]);
                subTotal += itemTotal;

                if (prodName.Length > 16) prodName = prodName.Substring(0, 16) + "..";

                // رسم المنتجات بألوان متباينة
                graphics.DrawString(prodName, fontRegular, solidBlack, startX, startY + offset);
                graphics.DrawString(qty, fontRegular, darkGray, new Rectangle(startX, startY + offset, 170, 20), formatRight);
                graphics.DrawString(itemTotal.ToString("0.00"), fontHeader, solidBlack, new Rectangle(startX, startY + offset, printWidth, 20), formatRight);
                offset += 25;
            }

            // خط منقط قبل المجاميع
            graphics.DrawLine(dottedPen, startX, startY + offset, startX + printWidth, startY + offset);
            offset += 15;

            // =================================================================
            // 5. المجاميع والخصم
            // =================================================================
            decimal.TryParse(lblDiscountAmount.Text, out decimal discountAmount);

            if (discountAmount > 0)
            {
                graphics.DrawString("Sub Total:", fontRegular, darkGray, startX + 50, startY + offset);
                graphics.DrawString(subTotal.ToString("0.00"), fontRegular, solidBlack, new Rectangle(startX, startY + offset, printWidth - 50, 20), formatRight);
                offset += 20;

                graphics.DrawString("Discount:", fontRegular, darkGray, startX + 50, startY + offset);
                graphics.DrawString("-" + discountAmount.ToString("0.00"), fontRegular, solidBlack, new Rectangle(startX, startY + offset, printWidth - 50, 20), formatRight);
                offset += 20;
            }

            // رسم خط سميك أسود لفصل المجموع النهائي
            graphics.DrawLine(solidPen, startX, startY + offset, startX + printWidth, startY + offset);
            offset += 10;

            // الصافي النهائي (بخط كبير جداً)
            graphics.DrawString("NET TOTAL", fontHeader, darkGray, startX, startY + offset + 5);
            graphics.DrawString(lblTotal.Text, fontTotal, solidBlack, new Rectangle(startX, startY + offset, printWidth, 30), formatRight);
            offset += 40;

            // =================================================================
            // 6. رسالة الشكر وباركود الزينة
            // =================================================================
            rectFull.Y = startY + offset;
            graphics.DrawString("Thank you for shopping with us!", fontRegular, darkGray, rectFull, formatCenter);
            offset += 25;

            // باركود تجميلي مطبوع برمز الأنابيب (يعطي شكل الباركود الكلاسيكي)
            Font fontBarcode = new Font("Arial", 22, FontStyle.Regular);
            graphics.DrawString("|| ||| | ||| || || | | || | ||", fontBarcode, solidBlack, new Rectangle(startX, startY + offset, printWidth, 30), formatCenter);
        }

        private void btnOpenDay_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter Opening Cash (Petty Cash):", "Open Day", "0");
            if (string.IsNullOrEmpty(input) || !decimal.TryParse(input, out decimal openingCash)) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO DailyClosures (OpenedBy, OpeningCash, IsClosed) VALUES (@Admin, @Cash, 0)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Admin", SessionManager.EmployeeName);
                cmd.Parameters.AddWithValue("@Cash", openingCash);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Day Opened Successfully!");

            // هذا السطر هو الذي سيحدث الأزرار فوراً
            CheckDayStatus();
        }

        private void btnCloseDay_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // 1. جلب بيانات الشفت المفتوح حالياً (مبلغ البداية، ووقت الفتح، والـ ID)
                string openQuery = "SELECT TOP 1 ClosureID, OpeningCash, OpeningTime FROM DailyClosures WHERE IsClosed = 0 ORDER BY ClosureID DESC";
                SqlCommand cmdOpen = new SqlCommand(openQuery, conn);

                int closureId = 0;
                decimal openingCash = 0;
                DateTime openingTime = DateTime.MinValue;

                using (SqlDataReader reader = cmdOpen.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        closureId = Convert.ToInt32(reader["ClosureID"]);
                        openingCash = Convert.ToDecimal(reader["OpeningCash"]);

                        // التأكد من وجود وقت الفتح لتجنب الأخطاء
                        if (reader["OpeningTime"] != DBNull.Value)
                            openingTime = Convert.ToDateTime(reader["OpeningTime"]);
                        else
                            openingTime = DateTime.Today; // كإجراء احتياطي
                    }
                    else
                    {
                        MessageBox.Show("لا يوجد شفت مفتوح حالياً لإغلاقه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // 2. حساب المبيعات (نعتمد على مبيعات الكاش التي تمت *بعد* وقت فتح هذا الشفت فقط)
                string salesQuery = @"SELECT ISNULL(SUM(TotalAmount), 0) FROM SalesInvoices 
                              WHERE InvoiceDate >= @OpenTime 
                              AND PaymentMethod = 'Cash'";
                SqlCommand cmdSales = new SqlCommand(salesQuery, conn);
                cmdSales.Parameters.AddWithValue("@OpenTime", openingTime);
                decimal totalCashSales = Convert.ToDecimal(cmdSales.ExecuteScalar());

                decimal expectedTotal = openingCash + totalCashSales;

                // 3. إدخال النقد الفعلي (أضفت تفاصيل توضيحية داخل النافذة لتراقب الحسابات)
                string input = Microsoft.VisualBasic.Interaction.InputBox($"Opening Cash: {openingCash}\nCash Sales: {totalCashSales}\n--------------------\nExpected Total (System): {expectedTotal}\n\nEnter Actual Cash in Drawer:", "Closing Day", "0");
                if (string.IsNullOrEmpty(input) || !decimal.TryParse(input, out decimal actualCash)) return;

                decimal difference = actualCash - expectedTotal;

                // 4. تحديث هذا الشفت تحديداً (باستخدام ClosureID) ليصبح مغلقاً
                string updateQuery = @"UPDATE DailyClosures SET 
                               IsClosed = 1, 
                               ClosedBy = @Admin, 
                               ExpectedCash = @Expected, 
                               ActualCashInDrawer = @Actual, 
                               Difference = @Diff, 
                               ClosingTime = GETDATE() 
                               WHERE ClosureID = @ClosureID";

                SqlCommand cmdUpdate = new SqlCommand(updateQuery, conn);
                cmdUpdate.Parameters.AddWithValue("@Admin", SessionManager.EmployeeName);
                cmdUpdate.Parameters.AddWithValue("@Expected", expectedTotal);
                cmdUpdate.Parameters.AddWithValue("@Actual", actualCash);
                cmdUpdate.Parameters.AddWithValue("@Diff", difference);
                cmdUpdate.Parameters.AddWithValue("@ClosureID", closureId); // نغلق الشفت الصحيح

                cmdUpdate.ExecuteNonQuery();

                string msg = (difference == 0) ? "Perfect Balance!" : $"Balance Warning! Difference: {difference}";
                MessageBox.Show($"Day Closed!\n{msg}", "Z-Report Saved");

                // طباعة تقرير الإغلاق
                printPreviewDialog1.Document = printDocument1;
                printPreviewDialog1.ShowDialog();

                // تحديث حالة الأزرار
                CheckDayStatus();
            }
        }

        private void btnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvCart.CurrentRow != null && dgvCart.Rows.Count > 0)
            {
                // 2. أخذ رقم السطر الذي حدده الكاشير
                int rowIndex = dgvCart.CurrentRow.Index;

                // 3. حذف السطر من جدول البيانات الوهمي (الذي يغذي الشاشة)
                _cartTable.Rows.RemoveAt(rowIndex);

                // 4. إعادة حساب المجموع الكلي للفاتورة بعد الحذف
                UpdateGrandTotal();

                // 5. إعادة المؤشر فوراً لمربع الباركود ليكمل الكاشير عمله بدون لمس الماوس
                txtBarcode.Focus();
            }
            else
            {
                // تنبيه في حال ضغط الزر بالخطأ دون تحديد منتج
                MessageBox.Show("No Product Selection!", "No Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void txtBarcode_TextChanged(object sender, EventArgs e)
        {

        }

        private void nudDiscount_ValueChanged(object sender, EventArgs e)
        {

            UpdateGrandTotal();
        }

        private void rbPercent_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGrandTotal();

        }

        private void rbAmount_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGrandTotal();

        }

        private void lblDiscountAmount_Click(object sender, EventArgs e)
        {

        }

        private void lblAIRecommendation_Click(object sender, EventArgs e)
        {

        }

        private void btnRemoveItem_Click_1(object sender, EventArgs e)
        {
            if (dgvCart.CurrentRow != null && dgvCart.Rows.Count > 0)
            {
                // 2. أخذ رقم السطر الذي حدده الكاشير
                int rowIndex = dgvCart.CurrentRow.Index;

                // 3. حذف السطر من جدول البيانات الوهمي (الذي يغذي الشاشة)
                _cartTable.Rows.RemoveAt(rowIndex);

                // 4. إعادة حساب المجموع الكلي للفاتورة بعد الحذف
                UpdateGrandTotal();

                // 5. إعادة المؤشر فوراً لمربع الباركود ليكمل الكاشير عمله بدون لمس الماوس
                txtBarcode.Focus();
                SystemLogger.LogAction("إلغاء منتج من الفاتورة", "إلغاء منتج من فاتورة مبيعات");

            }
            else
            {
                // تنبيه في حال ضغط الزر بالخطأ دون تحديد منتج
                MessageBox.Show("No Product Selection!", "No Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnReprint_Click_1(object sender, EventArgs e)
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Reprint Invoice",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 25, Text = "Invoice No:" };

            TextBox textBox = new TextBox() { Left = 100, Top = 22, Width = 200 };
            Button confirmation = new Button() { Text = "Search & Print", Left = 100, Width = 200, Top = 70, DialogResult = DialogResult.OK, BackColor = Color.LightBlue };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                string userInput = textBox.Text.Trim();
                if (string.IsNullOrEmpty(userInput)) return;

                userInput = userInput.ToUpper().Replace("INV-", "").Trim();

                string targetInv = "";
                if (int.TryParse(userInput, out int parsedNum))
                {
                    targetInv = "INV-" + parsedNum.ToString("D4");
                }
                else
                {
                    targetInv = textBox.Text.Trim();
                }

                string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        string queryHeader = "SELECT InvoiceID, InvoiceNo, InvoiceDate, CashierName, TotalAmount FROM SalesInvoices WHERE InvoiceNo = @InvNo";
                        SqlCommand cmdHeader = new SqlCommand(queryHeader, conn);
                        cmdHeader.Parameters.AddWithValue("@InvNo", targetInv);

                        int invoiceId = 0;
                        using (SqlDataReader reader = cmdHeader.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                invoiceId = Convert.ToInt32(reader["InvoiceID"]);
                                r_InvNo = reader["InvoiceNo"].ToString();
                                r_Date = Convert.ToDateTime(reader["InvoiceDate"]).ToString("yyyy/MM/dd HH:mm");
                                r_Cashier = reader["CashierName"].ToString();
                                r_Total = reader["TotalAmount"].ToString();
                            }
                        }

                        if (invoiceId == 0)
                        {
                            MessageBox.Show($"Invoice ({targetInv}) not found in the system!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string queryDetails = "SELECT p.ProductName, d.Quantity, d.Total FROM SalesInvoiceDetails d JOIN Products p ON d.ProductID = p.ProductID WHERE d.InvoiceID = @InvID";
                        SqlCommand cmdDetails = new SqlCommand(queryDetails, conn);
                        cmdDetails.Parameters.AddWithValue("@InvID", invoiceId);

                        SqlDataAdapter da = new SqlDataAdapter(cmdDetails);
                        _reprintTable = new DataTable();
                        da.Fill(_reprintTable);

                        System.Drawing.Printing.PrintDocument printDocReprint = new System.Drawing.Printing.PrintDocument();
                        printDocReprint.PrintPage += PrintDocReprint_PrintPage;

                        PrintPreviewDialog previewDialog = new PrintPreviewDialog();
                        previewDialog.Document = printDocReprint;
                        previewDialog.WindowState = FormWindowState.Maximized;
                        previewDialog.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error during reprint: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnCloseDay_Click_1(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // 1. جلب بيانات الشفت المفتوح حالياً (مبلغ البداية، ووقت الفتح، والـ ID)
                string openQuery = "SELECT TOP 1 ClosureID, OpeningCash, OpeningTime FROM DailyClosures WHERE IsClosed = 0 ORDER BY ClosureID DESC";
                SqlCommand cmdOpen = new SqlCommand(openQuery, conn);

                int closureId = 0;
                decimal openingCash = 0;
                DateTime openingTime = DateTime.MinValue;

                using (SqlDataReader reader = cmdOpen.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        closureId = Convert.ToInt32(reader["ClosureID"]);
                        openingCash = Convert.ToDecimal(reader["OpeningCash"]);

                        // التأكد من وجود وقت الفتح لتجنب الأخطاء
                        if (reader["OpeningTime"] != DBNull.Value)
                            openingTime = Convert.ToDateTime(reader["OpeningTime"]);
                        else
                            openingTime = DateTime.Today; // كإجراء احتياطي
                    }
                    else
                    {
                        MessageBox.Show("لا يوجد شفت مفتوح حالياً لإغلاقه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // 2. حساب المبيعات (نعتمد على مبيعات الكاش التي تمت *بعد* وقت فتح هذا الشفت فقط)
                string salesQuery = @"SELECT ISNULL(SUM(TotalAmount), 0) FROM SalesInvoices 
                              WHERE InvoiceDate >= @OpenTime 
                              AND PaymentMethod = 'Cash'";
                SqlCommand cmdSales = new SqlCommand(salesQuery, conn);
                cmdSales.Parameters.AddWithValue("@OpenTime", openingTime);
                decimal totalCashSales = Convert.ToDecimal(cmdSales.ExecuteScalar());

                decimal expectedTotal = openingCash + totalCashSales;

                // 3. إدخال النقد الفعلي (أضفت تفاصيل توضيحية داخل النافذة لتراقب الحسابات)
                string input = Microsoft.VisualBasic.Interaction.InputBox($"Opening Cash: {openingCash}\nCash Sales: {totalCashSales}\n--------------------\nExpected Total (System): {expectedTotal}\n\nEnter Actual Cash in Drawer:", "Closing Day", "0");
                if (string.IsNullOrEmpty(input) || !decimal.TryParse(input, out decimal actualCash)) return;

                decimal difference = actualCash - expectedTotal;

                // 4. تحديث هذا الشفت تحديداً (باستخدام ClosureID) ليصبح مغلقاً
                string updateQuery = @"UPDATE DailyClosures SET 
                               IsClosed = 1, 
                               ClosedBy = @Admin, 
                               ExpectedCash = @Expected, 
                               ActualCashInDrawer = @Actual, 
                               Difference = @Diff, 
                               ClosingTime = GETDATE() 
                               WHERE ClosureID = @ClosureID";

                SqlCommand cmdUpdate = new SqlCommand(updateQuery, conn);
                cmdUpdate.Parameters.AddWithValue("@Admin", SessionManager.EmployeeName);
                cmdUpdate.Parameters.AddWithValue("@Expected", expectedTotal);
                cmdUpdate.Parameters.AddWithValue("@Actual", actualCash);
                cmdUpdate.Parameters.AddWithValue("@Diff", difference);
                cmdUpdate.Parameters.AddWithValue("@ClosureID", closureId); // نغلق الشفت الصحيح

                cmdUpdate.ExecuteNonQuery();

                string msg = (difference == 0) ? "Perfect Balance!" : $"Balance Warning! Difference: {difference}";
                MessageBox.Show($"Day Closed!\n{msg}", "Z-Report Saved");

                // طباعة تقرير الإغلاق
                printPreviewDialog1.Document = printDocument1;
                printPreviewDialog1.ShowDialog();

                // تحديث حالة الأزرار
                CheckDayStatus();
                SystemLogger.LogAction("إغلاق الكاش وإتمام البيع", "إغلاق الكاش من قبل الأدمن");

            }
        }

        private void btnOpenDay_Click_1(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter Opening Cash (Petty Cash):", "Open Day", "0");
            if (string.IsNullOrEmpty(input) || !decimal.TryParse(input, out decimal openingCash)) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO DailyClosures (OpenedBy, OpeningCash, IsClosed) VALUES (@Admin, @Cash, 0)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Admin", SessionManager.EmployeeName);
                cmd.Parameters.AddWithValue("@Cash", openingCash);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Day Opened Successfully!");

            // هذا السطر هو الذي سيحدث الأزرار فوراً
            CheckDayStatus();
            SystemLogger.LogAction("بدء يوم جديد وفتح الكاش", "فتح يوم جديد بالكاش من قبل الأدمن");

        }
        // 🌟 المحرك الصامت لالتقاط اختصارات لوحة المفاتيح
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. اختصار الحفظ (Ctrl + S)
            if (keyData == (Keys.Control | Keys.S))
            {
                // استدعاء الضغطة برمجياً كأن المستخدم ضغط بالماوس
                // قم بتغيير اسم الزر ليطابق الزر الموجود لديك
                btnCheckout.PerformClick();

                return true; // إخبار الويندوز أننا تعاملنا مع الضغطة، لا تفعل شيئاً آخر
            }

            // 2. اختصار الطباعة (Ctrl + P)
            else if (keyData == (Keys.Control | Keys.P))
            {
                btnReprint.PerformClick();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.D))
            {
                btnRemoveItem.PerformClick();
                return true;
            }
            // 3. اختصار مسح الحقول أو فتح فاتورة جديدة (Ctrl + N)
            else if (keyData == (Keys.Control | Keys.N))
            {
                btnCancelOrder.PerformClick();
                return true;
            }

            // 4. استخدام أزرار الـ Function (مثل F12 لإتمام الدفع في الكاشير)
            else if (keyData == Keys.F12)
            {
                btnCheckout.PerformClick();
                return true;
            }

            // 5. زر الهروب (ESC) لإغلاق الشاشة
            else if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }

            // إذا كانت الضغطة ليست من اختصاراتنا، اترك الويندوز يتعامل معها بشكل طبيعي
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void printPreviewDialog1_Load(object sender, EventArgs e)
        {

        }

        private void btnCancelOrder_Click(object sender, EventArgs e)
        {
            _cartTable.Clear();
            lblTotal.Text = "0.00";
            txtBarcode.Clear();
            txtBarcode.Focus();
            SystemLogger.LogAction("إلغاء تسجيل فاتورة", "إلغاء فاتورة بيع من قبل الكاشير");

        }


        public void DeductFromInventory(int productId, int quantityToSell)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // جلب كل الدفعات المتوفرة لهذا المنتج مرتبة من الأقدم للأحدث (FIFO)
                string query = "SELECT BatchID, Quantity FROM InventoryBatches WHERE ProductID = @PID AND Quantity > 0 ORDER BY ReceiveDate ASC";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PID", productId);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dtBatches = new DataTable();
                da.Fill(dtBatches);

                int remainingToDeduct = quantityToSell;

                foreach (DataRow row in dtBatches.Rows)
                {
                    if (remainingToDeduct <= 0) break;

                    int batchId = Convert.ToInt32(row["BatchID"]);
                    int batchQty = Convert.ToInt32(row["Quantity"]);

                    if (batchQty <= remainingToDeduct)
                    {
                        // الدفعة أقل من أو تساوي المطلوب: صفر الدفعة واطرح قيمتها من المطلوب
                        remainingToDeduct -= batchQty;
                        UpdateBatchQuantity(batchId, 0, conn);
                    }
                    else
                    {
                        // الدفعة أكبر من المطلوب: اخصم المطلوب من الدفعة وأنهِ العملية
                        UpdateBatchQuantity(batchId, batchQty - remainingToDeduct, conn);
                        remainingToDeduct = 0;
                    }
                }

                if (remainingToDeduct > 0)
                {
                    MessageBox.Show("Warning: Sold more than available in tracked batches!");
                }
            }
        }
        private void UpdateBatchQuantity(int batchId, int newQty, SqlConnection conn)
        {
            string updateQuery = "UPDATE InventoryBatches SET Quantity = @Qty WHERE BatchID = @BID";
            SqlCommand cmd = new SqlCommand(updateQuery, conn);
            cmd.Parameters.AddWithValue("@Qty", newQty);
            cmd.Parameters.AddWithValue("@BID", batchId);
            cmd.ExecuteNonQuery();
        }
    }

}