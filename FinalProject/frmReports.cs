using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FinalProject
{
    public partial class frmReports : MaterialForm
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        DataTable dtReport;
        int currentRowIndex = 0;

        // متغيرات لحفظ المجاميع للطباعة
        decimal totalAmount = 0;
        decimal totalProfit = 0;
        public frmReports()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);

        }

        private void frmReports_Load(object sender, EventArgs e)
        {
            cmbReportType.Items.Add("1. تقرير المبيعات التفصيلي");
            cmbReportType.Items.Add("2. تقرير الأرباح وحركة الأصناف");
            cmbReportType.Items.Add("3. تقرير جرد المستودع (القيمة المالية)");
            cmbReportType.Items.Add("4. تقرير النواقص وإعادة الطلب");
            cmbReportType.Items.Add("5. تقرير إغلاقات الصندوق (Z-Reports)");
            cmbReportType.SelectedIndex = 0;

            dtpFrom.Value = DateTime.Today;
            dtpTo.Value = DateTime.Today.AddDays(1).AddSeconds(-1);
        }

        private void ExportToExcel(DataTable dt, string reportName)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel CSV File|*.csv";
                sfd.FileName = reportName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
                sfd.Title = "حفظ التقرير";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();

                    // 1. كتابة أسماء الأعمدة (الهيدر)
                    string[] columnNames = new string[dt.Columns.Count];
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        columnNames[i] = dt.Columns[i].ColumnName;
                    }
                    sb.AppendLine(string.Join(",", columnNames));

                    // 2. كتابة البيانات (الصفوف)
                    foreach (DataRow row in dt.Rows)
                    {
                        string[] fields = new string[dt.Columns.Count];
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            // استبدال الفواصل العادية بفواصل منقوطة لكي لا يختل ترتيب الإكسل
                            fields[i] = row[i].ToString().Replace(",", ";");
                        }
                        sb.AppendLine(string.Join(",", fields));
                    }

                    // 3. حفظ الملف (استخدمنا UTF8 مع BOM ليتعرف الإكسل على الحروف العربية)
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);

                    MessageBox.Show("تم التصدير بنجاح!\nيمكنك فتح الملف الآن في برنامج Excel.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExportSales_Click(object sender, EventArgs e)
        {
            dtReport = new DataTable();
            string query = "";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                // اختيار الاستعلام بناءً على التقرير المطلوب
                switch (cmbReportType.SelectedIndex)
                {
                    case 0: // المبيعات
                        query = @"SELECT InvoiceNo AS 'رقم الفاتورة', InvoiceDate AS 'التاريخ', 
                                         CashierName AS 'الكاشير', PaymentMethod AS 'طريقة الدفع', TotalAmount AS 'الإجمالي'
                                  FROM SalesInvoices 
                                  WHERE InvoiceDate BETWEEN @From AND @To ORDER BY InvoiceDate DESC";
                        break;

                    case 1: // الأرباح
                        query = @"SELECT p.ProductName AS 'المنتج', SUM(d.Quantity) AS 'الكمية المباعة', 
                                         SUM(d.Total) AS 'إجمالي المبيعات', 
                                         SUM(d.Quantity * t.UnitCost) AS 'إجمالي التكلفة', 
                                         SUM(d.Total - (d.Quantity * t.UnitCost)) AS 'صافي الربح'
                                  FROM SalesInvoiceDetails d 
                                  JOIN Products p ON d.ProductID = p.ProductID 
                                  JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                                  JOIN SupplyTransactions t ON d.ProductID = t.ProductID 
                                  WHERE i.InvoiceDate BETWEEN @From AND @To 
                                  GROUP BY p.ProductName";
                        break;

                    case 2: // الجرد (لا يحتاج تاريخ)
                        query = @"SELECT p.Barcode AS 'الباركود', p.ProductName AS 'المنتج', 
                                         SUM(b.Quantity) AS 'الكمية الحالية', p.SellingPrice AS 'سعر البيع', 
                                         SUM(b.Quantity * p.SellingPrice) AS 'القيمة التقديرية (JD)'
                                  FROM InventoryBatches b 
                                  JOIN Products p ON b.ProductID = p.ProductID 
                                  GROUP BY p.Barcode, p.ProductName, p.SellingPrice";
                        break;

                    case 3: // النواقص (لا يحتاج تاريخ)
                        query = @"SELECT p.Barcode AS 'الباركود', p.ProductName AS 'المنتج', 
                                         SUM(b.Quantity) AS 'الرصيد المتبقي', 5 AS 'الحد الأدنى'
                                  FROM InventoryBatches b 
                                  JOIN Products p ON b.ProductID = p.ProductID 
                                  GROUP BY p.Barcode, p.ProductName 
                                  HAVING SUM(b.Quantity) <= 5";
                        break;

                    case 4: // إغلاقات الصندوق
                        query = @"SELECT ClosureDate AS 'التاريخ', OpenedBy AS 'فتح بواسطة', ClosedBy AS 'أغلق بواسطة', 
                                         ExpectedCash AS 'المتوقع بالنظام', ActualCashInDrawer AS 'الفعلي بالدرج', 
                                         Difference AS 'العجز/الزيادة'
                                  FROM DailyClosures 
                                  WHERE ClosureDate BETWEEN @From AND @To";
                        break;
                }

                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@From", dtpFrom.Value.Date);
                cmd.Parameters.AddWithValue("@To", dtpTo.Value.Date.AddDays(1).AddSeconds(-1)); // لنهاية يوم النهاية

                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtReport);
                    dgvReport.DataSource = dtReport;
                    dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ في جلب البيانات: " + ex.Message);
                }
            }
        }

        private void dgvReport_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (dtReport == null || dtReport.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها!"); return;
            }

            SaveFileDialog sfd = new SaveFileDialog() { Filter = "CSV File|*.csv", FileName = "Report.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();

                // إضافة أسماء الأعمدة
                string[] columnNames = new string[dtReport.Columns.Count];
                for (int i = 0; i < dtReport.Columns.Count; i++) columnNames[i] = dtReport.Columns[i].ColumnName;
                sb.AppendLine(string.Join(",", columnNames));

                // إضافة البيانات
                foreach (DataRow row in dtReport.Rows)
                {
                    string[] fields = new string[dtReport.Columns.Count];
                    for (int i = 0; i < dtReport.Columns.Count; i++) fields[i] = row[i].ToString().Replace(",", ";");
                    sb.AppendLine(string.Join(",", fields));
                }

                // حفظ الملف بترميز يدعم العربية
                File.WriteAllText(sfd.FileName, "\uFEFF" + sb.ToString(), Encoding.UTF8);
                MessageBox.Show("تم تصدير التقرير بنجاح!");
            }
        }

        // ------------------ كود الطباعة الورقية (A4) ------------------
        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (dtReport == null || dtReport.Rows.Count == 0) return;

            PrintPreviewDialog ppd = new PrintPreviewDialog();
            ppd.Document = printDocument1;
            ppd.WindowState = FormWindowState.Maximized;
            ppd.ShowDialog();
        }

       

        private void cmbReportType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dtpFrom_ValueChanged(object sender, EventArgs e)
        {

        }

        private void dtpTo_ValueChanged(object sender, EventArgs e)
        {

        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            // إعدادات الثيم البنفسجي العصري
            Color purpleColor = Color.FromArgb(160, 82, 229);
            Color altRowColor = Color.FromArgb(248, 249, 250); // أوف وايت لتبادل الألوان
            Color textColor = Color.FromArgb(70, 70, 70);

            Font fTitle = new Font("Segoe UI", 20, FontStyle.Bold);
            Font fHeader = new Font("Segoe UI", 11, FontStyle.Bold);
            Font fBody = new Font("Segoe UI", 10, FontStyle.Regular);
            Font fSummary = new Font("Segoe UI", 12, FontStyle.Bold);

            // إعداد محاذاة النص للوسط
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            int margin = 40;
            int y = margin;
            int printableWidth = e.PageBounds.Width - (margin * 2);

            // 1. طباعة الترويسة (فقط في أول صفحة)
            if (currentRowIndex == 0)
            {
                g.DrawString("نظام إدارة الموارد الذكي", fBody, new SolidBrush(Color.Gray), margin, y);
                y += 30;

                // عنوان التقرير بالوسط باللون البنفسجي
                g.DrawString(cmbReportType.Text.Substring(3), fTitle, new SolidBrush(purpleColor), new Rectangle(margin, y, printableWidth, 40), sf);
                y += 50;

                g.DrawString("تاريخ الإصدار: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm"), fBody, new SolidBrush(Color.Gray), margin, y);
                y += 30;

                g.DrawLine(new Pen(purpleColor, 2), margin, y, e.PageBounds.Width - margin, y);
                y += 20;
            }

            int colWidth = printableWidth / dtReport.Columns.Count;

            // 2. طباعة رأس الجدول (شريط بنفسجي)
            Rectangle headerRect = new Rectangle(margin, y, printableWidth, 35);
            g.FillRectangle(new SolidBrush(purpleColor), headerRect);

            int colX = margin;
            foreach (DataColumn col in dtReport.Columns)
            {
                Rectangle cellRect = new Rectangle(colX, y, colWidth, 35);
                g.DrawString(col.ColumnName, fHeader, Brushes.White, cellRect, sf);
                colX += colWidth;
            }
            y += 35;

            // 3. طباعة صفوف البيانات بحماية ضد اللوب اللانهائي
            while (currentRowIndex < dtReport.Rows.Count)
            {
                DataRow row = dtReport.Rows[currentRowIndex];
                colX = margin;

                // تلوين الأسطر بالتبادل (Zebra Striping)
                if (currentRowIndex % 2 != 0)
                {
                    g.FillRectangle(new SolidBrush(altRowColor), new Rectangle(margin, y, printableWidth, 30));
                }

                for (int i = 0; i < dtReport.Columns.Count; i++)
                {
                    Rectangle cellRect = new Rectangle(colX, y, colWidth, 30);
                    string text = row[i].ToString();
                    if (text.Length > 25) text = text.Substring(0, 22) + "..."; // قص آمن

                    g.DrawString(text, fBody, new SolidBrush(textColor), cellRect, sf);
                    g.DrawRectangle(Pens.LightGray, cellRect); // حدود خفيفة جداً
                    colX += colWidth;
                }

                y += 30;
                currentRowIndex++; // الزيادة هنا تمنع التكرار اللانهائي

                // فحص مساحة الورقة للنزول لصفحة جديدة
                if (y > e.PageBounds.Height - 120) // نترك مساحة كافية بالأسفل
                {
                    e.HasMorePages = true;
                    return; // نخرج لنطبع صفحة جديدة ونكمل من نفس currentRowIndex
                }
            }

            // 4. طباعة المجاميع المالية (فقط في نهاية التقرير بالكامل)
            y += 20;
            g.DrawLine(new Pen(purpleColor, 2), margin, y, e.PageBounds.Width - margin, y);
            y += 15;

            if (totalAmount > 0)
            {
                g.DrawString($"الإجمالي الكلي: {totalAmount:N2} JD", fSummary, new SolidBrush(textColor), margin, y);
                y += 30;
            }

            if (totalProfit > 0)
            {
                g.DrawString($"صافي الربح: {totalProfit:N2} JD", fSummary, new SolidBrush(Color.ForestGreen), margin, y);
            }

            // إنهاء عملية الطباعة
            e.HasMorePages = false;
        }

        private void frmReports_FormClosed(object sender, FormClosedEventArgs e)
        {
            this .Close();
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            dtReport = new DataTable();
            string query = "";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                // اختيار الاستعلام بناءً على التقرير المطلوب
                switch (cmbReportType.SelectedIndex)
                {
                    case 0: // المبيعات
                        query = @"SELECT InvoiceNo AS 'رقم الفاتورة', InvoiceDate AS 'التاريخ', 
                                         CashierName AS 'الكاشير', PaymentMethod AS 'طريقة الدفع', TotalAmount AS 'الإجمالي'
                                  FROM SalesInvoices 
                                  WHERE InvoiceDate BETWEEN @From AND @To ORDER BY InvoiceDate DESC";
                        break;

                    case 1: // الأرباح
                        query = @"SELECT p.ProductName AS 'المنتج', SUM(d.Quantity) AS 'الكمية المباعة', 
                                         SUM(d.Total) AS 'إجمالي المبيعات', 
                                         SUM(d.Quantity * t.UnitCost) AS 'إجمالي التكلفة', 
                                         SUM(d.Total - (d.Quantity * t.UnitCost)) AS 'صافي الربح'
                                  FROM SalesInvoiceDetails d 
                                  JOIN Products p ON d.ProductID = p.ProductID 
                                  JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                                  JOIN SupplyTransactions t ON d.ProductID = t.ProductID 
                                  WHERE i.InvoiceDate BETWEEN @From AND @To 
                                  GROUP BY p.ProductName";
                        break;

                    case 2: // الجرد (لا يحتاج تاريخ)
                        query = @"SELECT p.Barcode AS 'الباركود', p.ProductName AS 'المنتج', 
                                         SUM(b.Quantity) AS 'الكمية الحالية', p.SellingPrice AS 'سعر البيع', 
                                         SUM(b.Quantity * p.SellingPrice) AS 'القيمة التقديرية (JD)'
                                  FROM InventoryBatches b 
                                  JOIN Products p ON b.ProductID = p.ProductID 
                                  GROUP BY p.Barcode, p.ProductName, p.SellingPrice";
                        break;

                    case 3: // النواقص (لا يحتاج تاريخ)
                        query = @"SELECT p.Barcode AS 'الباركود', p.ProductName AS 'المنتج', 
                                         SUM(b.Quantity) AS 'الرصيد المتبقي', 5 AS 'الحد الأدنى'
                                  FROM InventoryBatches b 
                                  JOIN Products p ON b.ProductID = p.ProductID 
                                  GROUP BY p.Barcode, p.ProductName 
                                  HAVING SUM(b.Quantity) <= 5";
                        break;

                    case 4: // إغلاقات الصندوق
                        query = @"SELECT ClosureDate AS 'التاريخ', OpenedBy AS 'فتح بواسطة', ClosedBy AS 'أغلق بواسطة', 
                                         ExpectedCash AS 'المتوقع بالنظام', ActualCashInDrawer AS 'الفعلي بالدرج', 
                                         Difference AS 'العجز/الزيادة'
                                  FROM DailyClosures 
                                  WHERE ClosureDate BETWEEN @From AND @To";
                        break;
                }

                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@From", dtpFrom.Value.Date);
                cmd.Parameters.AddWithValue("@To", dtpTo.Value.Date.AddDays(1).AddSeconds(-1)); // لنهاية يوم النهاية

                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtReport);
                    dgvReport.DataSource = dtReport;
                    dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    SystemLogger.LogAction("توليد تقرير", "تمت عملية توليد تقرير من قبل الأدمن");

                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ في جلب البيانات: " + ex.Message);
                }
            }
        }

        private void materialButton1_Click_1(object sender, EventArgs e)
        {
            if (dtReport == null || dtReport.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها!"); return;
            }

            SaveFileDialog sfd = new SaveFileDialog() { Filter = "CSV File|*.csv", FileName = "Report.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();

                // إضافة أسماء الأعمدة
                string[] columnNames = new string[dtReport.Columns.Count];
                for (int i = 0; i < dtReport.Columns.Count; i++) columnNames[i] = dtReport.Columns[i].ColumnName;
                sb.AppendLine(string.Join(",", columnNames));

                // إضافة البيانات
                foreach (DataRow row in dtReport.Rows)
                {
                    string[] fields = new string[dtReport.Columns.Count];
                    for (int i = 0; i < dtReport.Columns.Count; i++) fields[i] = row[i].ToString().Replace(",", ";");
                    sb.AppendLine(string.Join(",", fields));
                }

                // حفظ الملف بترميز يدعم العربية
                File.WriteAllText(sfd.FileName, "\uFEFF" + sb.ToString(), Encoding.UTF8);
                MessageBox.Show("تم تصدير التقرير بنجاح!");
                SystemLogger.LogAction("تصدير تقرير", "تمت عملية تصدير تقرير كملف exel من قبل الأدمن");

            }
        }
        private void LoadReportData()
        {
            dtReport = new DataTable();
            string query = "";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                switch (cmbReportType.SelectedIndex)
                {
                    case 0: // المبيعات
                        query = @"SELECT InvoiceNo AS 'رقم الفاتورة', InvoiceDate AS 'التاريخ', 
                                         CashierName AS 'الكاشير', PaymentMethod AS 'طريقة الدفع', TotalAmount AS 'الإجمالي'
                                  FROM SalesInvoices 
                                  WHERE InvoiceDate BETWEEN @From AND @To ORDER BY InvoiceDate DESC";
                        break;
                    case 1: // الأرباح
                        query = @"SELECT p.ProductName AS 'المنتج', SUM(d.Quantity) AS 'الكمية المباعة', 
                                         SUM(d.Total) AS 'إجمالي المبيعات', 
                                         SUM(d.Quantity * t.UnitCost) AS 'إجمالي التكلفة', 
                                         SUM(d.Total - (d.Quantity * t.UnitCost)) AS 'صافي الربح'
                                  FROM SalesInvoiceDetails d 
                                  JOIN Products p ON d.ProductID = p.ProductID 
                                  JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                                  JOIN SupplyTransactions t ON d.ProductID = t.ProductID 
                                  WHERE i.InvoiceDate BETWEEN @From AND @To 
                                  GROUP BY p.ProductName";
                        break;
                    case 2: // الجرد
                        query = @"SELECT p.Barcode AS 'الباركود', p.ProductName AS 'المنتج', 
                                         SUM(b.Quantity) AS 'الكمية الحالية', p.SellingPrice AS 'سعر البيع', 
                                         SUM(b.Quantity * p.SellingPrice) AS 'القيمة التقديرية (JD)'
                                  FROM InventoryBatches b 
                                  JOIN Products p ON b.ProductID = p.ProductID 
                                  GROUP BY p.Barcode, p.ProductName, p.SellingPrice";
                        break;
                    case 3: // النواقص
                        query = @"SELECT p.Barcode AS 'الباركود', p.ProductName AS 'المنتج', 
                                         SUM(b.Quantity) AS 'الرصيد المتبقي', 5 AS 'الحد الأدنى'
                                  FROM InventoryBatches b 
                                  JOIN Products p ON b.ProductID = p.ProductID 
                                  GROUP BY p.Barcode, p.ProductName 
                                  HAVING SUM(b.Quantity) <= 5";
                        break;
                    case 4: // إغلاقات الصندوق
                        query = @"SELECT ClosureDate AS 'التاريخ', OpenedBy AS 'فتح بواسطة', ClosedBy AS 'أغلق بواسطة', 
                                         ExpectedCash AS 'المتوقع بالنظام', ActualCashInDrawer AS 'الفعلي بالدرج', 
                                         Difference AS 'العجز/الزيادة'
                                  FROM DailyClosures 
                                  WHERE ClosureDate BETWEEN @From AND @To";
                        break;
                }

                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@From", dtpFrom.Value.Date);
                cmd.Parameters.AddWithValue("@To", dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dtReport);
                    dgvReport.DataSource = dtReport;
                    dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ في جلب البيانات: " + ex.Message);
                }
            }
        }


        private bool ForceExportToCSV()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "ملف بيانات CSV|*.csv";
            sfd.Title = "إجراء أمني: احفظ نسخة من التقرير قبل الطباعة";
            sfd.FileName = "نسخة_تقرير_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();

                    // الهيدر
                    string[] columnNames = new string[dtReport.Columns.Count];
                    for (int i = 0; i < dtReport.Columns.Count; i++) columnNames[i] = dtReport.Columns[i].ColumnName;
                    sb.AppendLine(string.Join(",", columnNames));

                    // البيانات
                    foreach (DataRow row in dtReport.Rows)
                    {
                        string[] fields = new string[dtReport.Columns.Count];
                        for (int i = 0; i < dtReport.Columns.Count; i++) fields[i] = row[i].ToString().Replace(",", " - ");
                        sb.AppendLine(string.Join(",", fields));
                    }

                    File.WriteAllText(sfd.FileName, "\uFEFF" + sb.ToString(), Encoding.UTF8);
                    return true; // نجح الحفظ
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            MessageBox.Show("تم إلغاء الطباعة لأنك لم تقم بحفظ نسخة من التقرير.", "إلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        // حساب المجاميع ديناميكياً للتقرير
        private void CalculateReportTotals()
        {
            totalAmount = 0;
            totalProfit = 0;

            foreach (DataRow row in dtReport.Rows)
            {
                // إذا كان التقرير يحتوي على عمود الإجمالي أو القيمة
                if (dtReport.Columns.Contains("الإجمالي"))
                    totalAmount += Convert.ToDecimal(row["الإجمالي"] == DBNull.Value ? 0 : row["الإجمالي"]);
                else if (dtReport.Columns.Contains("القيمة التقديرية (JD)"))
                    totalAmount += Convert.ToDecimal(row["القيمة التقديرية (JD)"] == DBNull.Value ? 0 : row["القيمة التقديرية (JD)"]);

                // إذا كان التقرير يحتوي على عمود صافي الربح
                if (dtReport.Columns.Contains("صافي الربح"))
                    totalProfit += Convert.ToDecimal(row["صافي الربح"] == DBNull.Value ? 0 : row["صافي الربح"]);
            }
        }

        private void materialButton1_Click_2(object sender, EventArgs e)
        {
            if (dtReport == null || dtReport.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لطباعتها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. إجبار المستخدم على حفظ نسخة CSV أولاً كأرشيف
            if (!ForceExportToCSV())
            {
                // إذا ألغى المستخدم الحفظ، نوقف عملية الطباعة
                return;
            }

            // 2. حساب المجاميع للطباعة
            CalculateReportTotals();

            // 3. تصفير العداد وبدء الطباعة
            currentRowIndex = 0;
            PrintPreviewDialog ppd = new PrintPreviewDialog();
            ppd.Document = printDocument1;
            ppd.WindowState = FormWindowState.Maximized;
            ppd.ShowDialog();
            SystemLogger.LogAction("طباعة تقرير", "تمت عملية طباعة وحفظ تقرير من قبل الأدمن");

        }
    }
}
    

