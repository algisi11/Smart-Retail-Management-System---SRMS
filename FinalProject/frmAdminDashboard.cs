using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;



namespace FinalProject

{


    public partial class frmAdminDashboard : MaterialForm
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        public frmAdminDashboard()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            InitializeToolTips();
        }

        private void frmAdminDashboard_Load(object sender, EventArgs e)
        {
            cmbTimeFilter.Items.Clear();
            cmbTimeFilter.Items.Add(" (Today)");
            cmbTimeFilter.Items.Add(" (This Week)");
            cmbTimeFilter.Items.Add(" (This Month)");
            cmbTimeFilter.Items.Add(" (This Year)");
            cmbTimeFilter.Items.Add(" (Lifetime)");

            cmbTimeFilter.SelectedIndex = 2; // يختار "هذا الشهر" كوضع افتراضي

            dashboardTimer.Start();
        }
        private void BindChartData(Chart chart, DataTable dt, string xMember, string yMember, SeriesChartType type)
        {

            if (chart == null)
                return;

            // 🛡️ الدرع الثاني: التأكد من أن جدول البيانات (DataTable) ليس فارغاً ويحتوي على أرقام
            if (dt == null || dt.Rows.Count == 0)
                return;

            // 🛡️ الدرع الثالث: إذا كان الشارت لا يحتوي على أي Series (تم حذفها من التصميم)، ننشئ واحدة برمجياً!
            if (chart.Series.Count == 0)
            {
                chart.Series.Add("Series1");
            }

            try
            {
                // الآن يمكننا رسم البيانات بأمان تام
                chart.Series[0].Points.Clear();
                chart.Series[0].ChartType = type;
                chart.Series[0].XValueMember = xMember;
                chart.Series[0].YValueMembers = yMember;
                chart.DataSource = dt;
                chart.DataBind();
            }
            catch (Exception ex)
            {
                // لكي لا ينهار البرنامج إذا كان هناك خطأ في أسماء الأعمدة
                MessageBox.Show("خطأ أثناء رسم المخطط البياني: " + ex.Message);
            }
        }
        private string GetDateFilter()
        {
            if (chkCustomRange.Checked)
            //    dtpStart.Enabled = true; dtpEnd.Enabled = true;
                return "i.InvoiceDate BETWEEN @Start AND @End";

            if (cmbTimeFilter.SelectedIndex == 0) return "CAST(i.InvoiceDate AS DATE) = CAST(GETDATE() AS DATE)";
            if (cmbTimeFilter.SelectedIndex == 1) return "DATEPART(week, i.InvoiceDate) = DATEPART(week, GETDATE()) AND YEAR(i.InvoiceDate) = YEAR(GETDATE())";
            if (cmbTimeFilter.SelectedIndex == 2) return "MONTH(i.InvoiceDate) = MONTH(GETDATE()) AND YEAR(i.InvoiceDate) = YEAR(GETDATE())";
            if (cmbTimeFilter.SelectedIndex == 3) return "YEAR(i.InvoiceDate) = YEAR(GETDATE())";

            return "1=1";
        }
        private async void RefreshDashboardData()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                bool isCustom = chkCustomRange.Checked;
                DateTime startD = dtpStart.Value.Date;
                DateTime endD = dtpEnd.Value.Date.AddDays(1).AddSeconds(-1);
                string dateFilter = GetDateFilter();

                string salesText = "0.00 JD", profitText = "0.00 JD";
                string ordersText = "0", stockText = "0", capitalText = "0.00 JD";
                DataTable dtTop = new DataTable();
                DataTable dtTurnover = new DataTable();

                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        Action<SqlCommand> InjectParams = (cmd) =>
                        {
                            if (isCustom)
                            {
                                cmd.Parameters.AddWithValue("@Start", startD);
                                cmd.Parameters.AddWithValue("@End", endD);
                            }
                        };

                        // المبيعات
                        string qSales = $"SELECT ISNULL(SUM(i.TotalAmount), 0) FROM SalesInvoices i WHERE {dateFilter}";
                        using (SqlCommand cmd = new SqlCommand(qSales, conn))
                        {
                            InjectParams(cmd);
                            salesText = Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2") + " JD";
                        }

                        // الأرباح
                        string qProfit = $@"SELECT ISNULL(SUM(d.Total - (d.Quantity * t.UnitCost)), 0) 
                            FROM SalesInvoiceDetails d 
                            JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                            JOIN SupplyTransactions t ON d.ProductID = t.ProductID 
                            WHERE {dateFilter}";
                        using (SqlCommand cmd = new SqlCommand(qProfit, conn))
                        {
                            InjectParams(cmd);
                            profitText = Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2") + " JD";
                        }

                        // عدد الفواتير
                        string qOrders = $"SELECT COUNT(*) FROM SalesInvoices i WHERE {dateFilter}";
                        using (SqlCommand cmd = new SqlCommand(qOrders, conn))
                        {
                            InjectParams(cmd);
                            ordersText = cmd.ExecuteScalar().ToString();
                        }

                        // النواقص
                        string qStock = @"SELECT COUNT(*) FROM (
                          SELECT ProductID, SUM(Quantity) as Qty 
                          FROM InventoryBatches 
                          GROUP BY ProductID 
                          HAVING SUM(Quantity) <= 5) as LowStock";
                        using (SqlCommand cmd = new SqlCommand(qStock, conn))
                        {
                            stockText = cmd.ExecuteScalar().ToString();
                        }

                        // رأس المال
                        string qCapital = @"SELECT ISNULL(SUM(b.Quantity * p.SellingPrice), 0) 
                            FROM InventoryBatches b 
                            JOIN Products p ON b.ProductID = p.ProductID";
                        using (SqlCommand cmd = new SqlCommand(qCapital, conn))
                        {
                            capitalText = Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2") + " JD";
                        }

                        // الأكثر مبيعاً
                        string qTop = $@"SELECT TOP 5 p.ProductName, SUM(d.Quantity) as TotalQty 
                         FROM SalesInvoiceDetails d 
                         JOIN Products p ON d.ProductID = p.ProductID 
                         JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                         WHERE {dateFilter} 
                         GROUP BY p.ProductName ORDER BY TotalQty DESC";
                        using (SqlCommand cmd = new SqlCommand(qTop, conn))
                        {
                            InjectParams(cmd);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                                da.Fill(dtTop);
                        }

                        // الأعلى تدويراً
                        string qTurnover = $@"SELECT TOP 5 p.ProductName, (SUM(d.Quantity) * 1.0 / NULLIF((SELECT SUM(Quantity) FROM InventoryBatches WHERE ProductID = p.ProductID), 0)) as TurnoverRate
                              FROM SalesInvoiceDetails d
                              JOIN Products p ON d.ProductID = p.ProductID
                              JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                              WHERE {dateFilter} 
                              GROUP BY p.ProductID, p.ProductName ORDER BY TurnoverRate DESC";
                        using (SqlCommand cmd = new SqlCommand(qTurnover, conn))
                        {
                            InjectParams(cmd);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                                da.Fill(dtTurnover);
                        }
                    }
                });

                // 🚀 السطر السحري: إذا تم إغلاق الشاشة أثناء تحميل البيانات، توقف فوراً ولا تكمل!
                if (this.IsDisposed) return;

                // تحديث واجهة المستخدم
                lblTodaySales.Text = salesText;
                lblTodayProfit.Text = profitText;
                lblInvoiceCount.Text = ordersText;
                lblTotalCapital.Text = capitalText;

                // رسم الشارتات (تم حذف النسخ المكررة)
                BindChartData(chartTopSelling, dtTop, "ProductName", "TotalQty", SeriesChartType.Pie);
                BindChartData(chartTurnover, dtTurnover, "ProductName", "TurnoverRate", SeriesChartType.Bar);
            }
            catch (Exception ex)
            {
                // تم إضافة شرط لتجاهل الخطأ إذا تم إغلاق الشاشة فعلاً
                if (!this.IsDisposed)
                {
                    MessageBox.Show("خطأ في تحديث البيانات: " + ex.Message);
                }
            }
            finally
            {
                if (!this.IsDisposed) this.Cursor = Cursors.Default;
            }
        }
        private void FillChart(Chart chart, string query, string xMember, string yMember, SeriesChartType type)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                chart.Series[0].Points.Clear();
                chart.Series[0].ChartType = type;
                chart.Series[0].XValueMember = xMember;
                chart.Series[0].YValueMembers = yMember;
                chart.DataSource = dt;
                chart.DataBind();
            }
        }

        private void dashboardTimer_Tick(object sender, EventArgs e)
        {
            dashboardTimer.Stop();
            RefreshDashboardData();
            // تشغيل التايمر مرة أخرى بعد انتهاء التحديث
            dashboardTimer.Start();
        }

        private void label3_Click(object sender, EventArgs e) { }
        private void label8_Click(object sender, EventArgs e) { }
        private void lblTodaySales_Click(object sender, EventArgs e) { }

        private void label8_Click_1(object sender, EventArgs e)
        {

        }

        private void cmbTimeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshDashboardData();
        }

        private void lblInvoiceCount_Click(object sender, EventArgs e)
        {

        }

        

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label4_Paint(object sender, PaintEventArgs e)
        {
           
        }

        

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmAdminDashboard_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.Close();

        }

        private void chartTurnover_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            frmAIChat chatForm = new frmAIChat();
            chatForm.Show();
            SystemLogger.LogAction("شاشة الذكاء", "تم فتح شاشة الذكاء الاصطناعي من قبل الأدمن");

        }

        private void btnReports_Click_1(object sender, EventArgs e)
        {
            frmReports Repports = new frmReports();
            Repports.ShowDialog();
            SystemLogger.LogAction("شاشة التقارير", "تم فتح شاشة التقارير من قبل الأدمن");

        }

        private void cmbTimeFilter_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
        // ====================================================
        // 🌟 إعداد التلميحات (Hints) للوقوف المطول
        // ====================================================
        private void InitializeToolTips()
        {
            ToolTip hints = new ToolTip();

            // إعدادات وقت الظهور (تأثير الوقوف المطول)
            hints.InitialDelay = 800;
            hints.AutoPopDelay = 5000;
            hints.ReshowDelay = 200;
            hints.ShowAlways = true;

            // ربط التلميحات بأزرار الداشبورد
            hints.SetToolTip(btnReports, "فتح شاشة التقارير الشاملة (Ctrl + R)");
            hints.SetToolTip(button4, "فتح مساعد الذكاء الاصطناعي (F1)"); // افترضت أن اسمه button4 بناءً على كودك
            hints.SetToolTip(btnLog, "فتح سجل مراقبة النظام (Ctrl + L)");
            hints.SetToolTip(btnClose, "إغلاق لوحة التحكم (Esc)");
        }

        // ====================================================
        // 🌟 محرك الاختصارات (Shortcuts) لشاشة الأدمن
        // ====================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. اختصار التقارير (Ctrl + R)
            if (keyData == (Keys.Control | Keys.R))
            {
                btnReports.PerformClick();
                return true;
            }

            // 2. اختصار سجل التتبع (Ctrl + L)
            else if (keyData == (Keys.Control | Keys.L))
            {
                btnLog.PerformClick();
                return true;
            }

            // 3. اختصار الذكاء الاصطناعي (F1) - متعارف عليه للمساعدة
            else if (keyData == Keys.F1)
            {
                button4.PerformClick();
                return true;
            }

            // 4. اختصار الإغلاق (Esc)
            else if (keyData == Keys.Escape)
            {
                btnClose.PerformClick();
                return true;
            }

            // السماح للويندوز بالتعامل مع باقي الأزرار بشكل طبيعي
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void chkCustomRange_CheckedChanged(object sender, EventArgs e)
        {
            dtpStart.Enabled = chkCustomRange.Checked;
            dtpEnd.Enabled = chkCustomRange.Checked;

            // تعطيل القائمة المنسدلة القديمة لكي لا يختلط الأمر على المستخدم
            cmbTimeFilter.Enabled = !chkCustomRange.Checked;

            // بمجرد أن يغير الأدمن رأيه، نقوم بتحديث الداشبورد فوراً!
            RefreshDashboardData();
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            frmAuditLog Log = new frmAuditLog ();
            Log.ShowDialog();
            SystemLogger.LogAction("شاشة التتبع", "تم فتح شاشة التتبع من قبل الأدمن");

        }

        private void btnReturns_Click(object sender, EventArgs e)
        {
            frmReturns EditInv = new frmReturns();
            EditInv.ShowDialog();
        }

        private void btnCashier_Click(object sender, EventArgs e)
        {

        }

        private void btnBackUp_Click(object sender, EventArgs e)
        {
            frmBackup Back = new frmBackup ();
            Back.ShowDialog();
        }

        private void btnAiForcaster_Click(object sender, EventArgs e)
        {
            // 1. إنشاء الشاشة المنبثقة (Modal)
            Form popup = new Form()
            {
                Width = 650,
                Height = 500,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "الذكاء الاصطناعي - توقعات الطلب للمستودع",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            // 2. تصميم الأدوات (العنوان، الزر، رسالة التحميل، والجدول)
            Label lblTitle = new Label() { Text = "تقرير التنبؤ باحتياجات المستودع للـ 7 أيام القادمة", Left = 20, Top = 15, Width = 500, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.DarkSlateBlue };

            Button btnRun = new Button() { Text = "🚀 بدء التحليل الذكي", Left = 20, Top = 50, Width = 200, Height = 35, BackColor = Color.MediumSlateBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            Label lblStatus = new Label() { Text = "⏳ جاري قراءة البيانات وتدريب النماذج... يرجى الانتظار", Left = 230, Top = 58, Width = 380, Font = new Font("Segoe UI", 10, FontStyle.Italic), ForeColor = Color.Gray, Visible = false };

            DataGridView dgvForecast = new DataGridView()
            {
                Left = 20,
                Top = 100,
                Width = 590,
                Height = 330,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                BackgroundColor = Color.WhiteSmoke,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };

            // 3. برمجة الحدث الموازي (Asynchronous) لتشغيل المحرك
            btnRun.Click += async (s, args) =>
            {
                btnRun.Enabled = false;
                lblStatus.Visible = true;
                lblStatus.Text = "⏳ جاري قراءة البيانات وتدريب النماذج... يرجى الانتظار";
                lblStatus.ForeColor = Color.Gray;
                dgvForecast.DataSource = null;

                try
                {
                    // 🌟 السطر السحري الجديد بدلاً من السطر القديم الذي حذفته
                    var report = await System.Threading.Tasks.Task.Run(() =>
                    {
                        // استخدام اسم الكلاس الخاص بك
                        SalesForecaster engine = new SalesForecaster();
                        return engine.RunInventoryForecast(); // تشغيل المحرك المجمع
                    });

                    // عرض النتيجة في الجدول
                    dgvForecast.DataSource = report;

                    // تنسيق أسماء الأعمدة
                    if (dgvForecast.Columns.Count > 0)
                    {
                        dgvForecast.Columns["ProductName"].HeaderText = "اسم المنتج";
                        dgvForecast.Columns["TotalPredictedNext7Days"].HeaderText = "الاحتياج المتوقع (7 أيام)";
                        dgvForecast.Columns["Status"].HeaderText = "حالة الطلب";
                    }

                    lblStatus.Text = "✅ اكتمل التحليل بنجاح!";
                    lblStatus.ForeColor = Color.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء التحليل: " + ex.Message, "خطأ تقني", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Visible = false;
                }
                finally
                {
                    btnRun.Enabled = true;
                    btnRun.Text = "🔄 إعادة التحليل";
                }
            };

            // 4. إضافة الأدوات وعرض الشاشة
            popup.Controls.Add(lblTitle);
            popup.Controls.Add(btnRun);
            popup.Controls.Add(lblStatus);
            popup.Controls.Add(dgvForecast);

            popup.ShowDialog();
        }

      
    }
}