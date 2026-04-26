using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace FinalProject
{
    public partial class frmAIChat : MaterialForm
    {
        private string attachedImagePath = null;

        // 🌟 التهيئة الصحيحة: تم وضع = new هنا مباشرة لضمان عدم نسيانها
        private GroqLlamaClient aiClient = new GroqLlamaClient();

        private string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        public frmAIChat()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);


            txtChatHistory.RightToLeft = RightToLeft.Yes;
            txtQuestion.RightToLeft = RightToLeft.Yes;
            lblStatus.RightToLeft = RightToLeft.Yes;

            // تحسين نوع وحجم الخط ليكون مريحاً وعصرياً
            txtChatHistory.Font = new Font("Tahoma", 12F, FontStyle.Regular);
            txtQuestion.Font = new Font("Tahoma", 12F, FontStyle.Regular);

            // إضافة رسالة ترحيبية أنيقة عند فتح الشاشة
            txtChatHistory.Text = $"🤖 المستشار الذكي [{DateTime.Now.ToString("hh:mm tt")}]:\r\nأهلاً بك أيها المدير. أنا متصل الآن بقاعدة بيانات النظام بالكامل. اسألني عن المبيعات الكلية، أو ارفق لي صورة منتج لأحلله لك.\r\n" + new string('━', 40) + "\r\n\r\n";
        }

        private async Task<string> GetBusinessContextAsync()
        {
            StringBuilder context = new StringBuilder();

            // =========================================================
            // 🚀 توجيهات نظام صارمة لترويض الذكاء الاصطناعي
            // =========================================================
            context.AppendLine("=== توجيهات نظام صارمة لك (System Instructions) ===");
            context.AppendLine("1. أنت مستشار ذكي لمدير نظام المستودعات. أجب على سؤاله باختصار شديد ووضوح تام.");
            context.AppendLine("2. أجب عن المطلوب فقط، ولا تقم أبداً بسرد أو ذكر أي معلومات من التقرير أدناه ما لم يطلبها المستخدم صراحة.");
            context.AppendLine("3. تجنب المقدمات الطويلة، الجمل الترحيبية المكررة، أو الخواتيم الزائدة.");
            context.AppendLine("===================================================\n");

            context.AppendLine("=== تقرير الحالة الشامل للنظام (ERP Data) ===");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // ---------------------------------------------------------
                // القسم الأول: الأداء المالي والمبيعات
                // ---------------------------------------------------------
                context.AppendLine("\n[1. المبيعات والأداء المالي]");
                string qSales = @"
    SELECT 
        ISNULL((SELECT SUM(TotalAmount) FROM SalesInvoices), 0) AS TotalSales,
        ISNULL((SELECT SUM(TotalAmount) FROM SalesInvoices WHERE MONTH(InvoiceDate) = MONTH(GETDATE()) AND YEAR(InvoiceDate) = YEAR(GETDATE())), 0) AS MonthSales,
        ISNULL((SELECT SUM(TotalAmount) FROM SalesInvoices WHERE CAST(InvoiceDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TodaySales";

                using (SqlCommand cmd = new SqlCommand(qSales, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        context.AppendLine($"- المبيعات الكلية التاريخية: {reader["TotalSales"]} دينار.");
                        context.AppendLine($"- مبيعات الشهر الحالي: {reader["MonthSales"]} دينار.");
                        context.AppendLine($"- مبيعات اليوم: {reader["TodaySales"]} دينار.");
                    }
                }

                // ---------------------------------------------------------
                // القسم الثاني: حركة المنتجات التفصيلية (SKU-Level Data)
                // ---------------------------------------------------------
                context.AppendLine("\n[2. حركة المنتجات الفردية والمخزون (SKU Data)]");

                string qSkuData = @"
    SELECT 
        p.ProductName,
        ISNULL((SELECT SUM(sid.Quantity) 
                FROM SalesInvoiceDetails sid 
                JOIN SalesInvoices si ON sid.InvoiceID = si.InvoiceID 
                WHERE sid.ProductID = p.ProductID AND si.InvoiceDate >= DATEADD(day, -30, GETDATE())), 0) AS Sold30Days,
        ISNULL((SELECT SUM(Quantity) FROM InventoryBatches WHERE ProductID = p.ProductID), 0) AS CurrentStock
    FROM Products p";

                using (SqlCommand cmd = new SqlCommand(qSkuData, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    bool hasProducts = false;
                    context.AppendLine("فيما يلي تقرير بكل منتج (المبيعات الشهرية / الرصيد الحالي):");

                    while (await reader.ReadAsync())
                    {
                        string pName = reader["ProductName"].ToString();
                        int sold30 = Convert.ToInt32(reader["Sold30Days"]);
                        int stock = Convert.ToInt32(reader["CurrentStock"]);

                        context.AppendLine($"  - الصنف [{pName}]: تم بيع ({sold30} حبات) آخر 30 يوم | المخزون المتوفر الآن ({stock} حبات).");
                        hasProducts = true;
                    }
                    if (!hasProducts) context.AppendLine("  - لا توجد منتجات مسجلة في النظام.");
                }

                // ---------------------------------------------------------
                // القسم الثالث: أداء الموردين
                // ---------------------------------------------------------
                context.AppendLine("\n[3. أداء الموردين وتقييم الجودة (Supplier Evaluation)]");

                string qSuppliers = @"
    SELECT 
        s.SupplierName,
        s.AgreedLeadTime,
        COUNT(st.TransactionID) AS TotalOrders,
        ISNULL(AVG(st.DefectPercentage), 0) AS AvgDefectRate
    FROM Suppliers s
    JOIN SupplyTransactions st ON s.SupplierID = st.SupplierID
    GROUP BY s.SupplierName, s.AgreedLeadTime";

                try
                {
                    using (SqlCommand cmd = new SqlCommand(qSuppliers, conn))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        bool hasSuppliers = false;
                        while (await reader.ReadAsync())
                        {
                            string supName = reader["SupplierName"].ToString();
                            int leadTime = Convert.ToInt32(reader["AgreedLeadTime"]);
                            int totalOrders = Convert.ToInt32(reader["TotalOrders"]);
                            decimal defectRate = Convert.ToDecimal(reader["AvgDefectRate"]);

                            context.AppendLine($"- المورد [{supName}]: ورّد لنا ({totalOrders} طلبيات)، يحتاج ({leadTime} أيام) للتوصيل، ومتوسط نسبة التالف لديه هي ({defectRate:F1}%).");
                            hasSuppliers = true;
                        }
                        if (!hasSuppliers) context.AppendLine("- لا توجد حركات توريد سابقة لتقييم الموردين.");
                    }
                }
                catch (Exception ex)
                {
                    context.AppendLine($"- (تنبيه للذكاء الاصطناعي: تعذر جلب التقييمات. التفاصيل: {ex.Message})");
                }
            } // إغلاق الاتصال بقاعدة البيانات

            return context.ToString();
        }

        private async Task<string> GetForecastForProductAsync(int productId, string productName)
        {
            List<ProductSalesData> salesHistory = new List<ProductSalesData>();

            // 1. الاستعلام الموفر للرام: جلب المجاميع اليومية فقط لآخر 60 يوماً
            string query = @"
        SELECT 
            CAST(si.InvoiceDate AS DATE) as SaleDate, 
            SUM(sid.Quantity) as DailyQuantity
        FROM SalesInvoiceDetails sid
        JOIN SalesInvoices si ON sid.InvoiceID = si.InvoiceID
        WHERE sid.ProductID = @ProductID 
          AND si.InvoiceDate >= DATEADD(day, -60, GETDATE())
        GROUP BY CAST(si.InvoiceDate AS DATE)
        ORDER BY SaleDate ASC";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            salesHistory.Add(new ProductSalesData
                            {
                                Date = Convert.ToDateTime(reader["SaleDate"]),
                                Quantity = Convert.ToSingle(reader["DailyQuantity"])

                            });
                        }
                    }
                }

            }

            // 2. فحص ما إذا كانت البيانات كافية للخوارزمية (نحتاج 14 يوماً على الأقل تمت فيها عمليات بيع)
            if (salesHistory.Count < 14)
            {
                return $"[تنبيه للمستشار: لا توجد بيانات تاريخية كافية (أقل من 14 يوم مبيعات) لعمل تنبؤ دقيق لصنف {productName} حالياً].";
            }

            try
            {
                // 3. تشغيل محرك التنبؤ الإحصائي (ML.NET)
                SalesForecaster forecaster = new SalesForecaster();
                float[] next7Days = forecaster.PredictFutureSales(salesHistory, 7);

                // 4. جمع التوقعات
                float totalExpected = next7Days.Sum();
                return $"[معلومة مؤكدة من خوارزمية SARIMA/SSA: التنبؤ الإحصائي الدقيق لمبيعات صنف {productName} خلال الـ 7 أيام القادمة هو ({totalExpected}) حبات].";
            }
            catch (Exception ex)
            {
                return $"[خطأ في التنبؤ للصنف {productName}: {ex.Message}]";
            }
        }

        private void frmAIChat_Load(object sender, EventArgs e)
        {

        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string question = txtQuestion.Text.Trim();

            // يجب أن يكون هناك سؤال أو صورة على الأقل لإرسال الطلب
            if (string.IsNullOrEmpty(question) && attachedImagePath == null) return;

            string timeNow = DateTime.Now.ToString("hh:mm tt");

            // طباعة رسالة المدير على الشاشة
            string displayMessage = question;
            if (attachedImagePath != null)
            {
                displayMessage += "\n[مرفق صورة 📎]";
            }

            txtChatHistory.AppendText($"👤 أنت [{timeNow}]:\r\n{displayMessage}\r\n\r\n");
            SystemLogger.LogAction("ارسال رسالة", "تم إرسال رسالة للذكاء من قبل الأدمن");

            // تفريغ المربعات استعداداً للرسالة القادمة
            txtQuestion.Clear();

            btnSend.Enabled = false;
            lblStatus.Text = "⏳ المستشار يقرأ البيانات (والصورة إن وجدت) ليحلل الرد...";
            lblStatus.ForeColor = Color.Blue;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // جلب الداتا الشاملة العادية
                string systemData = await GetBusinessContextAsync();

                // =========================================================
                // 🚀 الإضافة الهندسية: النظام الديناميكي لاكتشاف التنبؤ (ML.NET)
                // =========================================================
                // سيعمل هذا الجزء فقط إذا كان السؤال يحتوي على كلمات تدل على طلب التنبؤ
                if (question.Contains("توقع") || question.Contains("تنبؤ") || question.Contains("مستقبل") || question.Contains("مبيعات"))
                {
                    int targetProductId = -1;
                    string targetProductName = "";

                    // جلب قائمة المنتجات من قاعدة البيانات للبحث فيها
                    string queryProducts = "SELECT ProductID, ProductName FROM Products";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        using (SqlCommand cmd = new SqlCommand(queryProducts, conn))
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string pName = reader["ProductName"].ToString();

                                // هل ذكر المستخدم اسم هذا المنتج في رسالته؟
                                // IndexOf هنا تبحث عن الاسم بغض النظر عن حالة الأحرف (كبيرة/صغيرة)
                                if (question.IndexOf(pName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    targetProductId = Convert.ToInt32(reader["ProductID"]);
                                    targetProductName = pName;
                                    break; // وجدنا المنتج المستهدف، نوقف البحث في القاعدة
                                }
                            }
                        }
                    }

                    // إذا وجدنا المنتج في رسالة المستخدم، نمرره لمحرك الرياضات ML.NET
                    if (targetProductId != -1)
                    {
                        // 🛑 إذا وضعت نقطة التوقف (Breakpoint) هنا، سيتوقف البرنامج لتتأكد من عمله
                        string mlForecast = await GetForecastForProductAsync(targetProductId, targetProductName);

                        // حقن التوقع الرياضي السري داخل عقل الذكاء الاصطناعي
                        systemData += "\n\n" + mlForecast;
                    }
                }
                // =========================================================

                // 🌟 إرسال السؤال + بيانات المستودع + مسار الصورة (إن وجد) + التنبؤ إن وجد
                string aiResponse = await aiClient.AskAIAsync(systemData, question, attachedImagePath);

                // طباعة رد الذكاء الاصطناعي
                txtChatHistory.AppendText($"🤖 المستشار الذكي [{DateTime.Now.ToString("hh:mm tt")}]:\r\n{aiResponse}\r\n");
                txtChatHistory.AppendText(new string('━', 40) + "\r\n\r\n");

                // تفريغ مسار الصورة بعد أن تم إرسالها بنجاح حتى لا ترسل مرة أخرى بالخطأ
                attachedImagePath = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ في الاتصال: " + ex.Message);
            }
            finally
            {
                btnSend.Enabled = true;
                lblStatus.Text = "✅ متصل وجاهز للمساعدة";
                lblStatus.ForeColor = Color.Green;
                this.Cursor = Cursors.Default;

                // التمرير التلقائي لأسفل المحادثة
                txtChatHistory.SelectionStart = txtChatHistory.Text.Length;
                txtChatHistory.ScrollToCaret();
                txtQuestion.Focus();
            }
        }

        private void txtQuestion_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtQuestion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true; // منع نزول سطر جديد
                btnSend.PerformClick();
            }
        }

        private void btnAttachPicture_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.jfif";
            openFileDialog.Title = "اختر صورة المنتج أو الماركة";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // نحتفظ بمسار الصورة في المتغير لنستخدمه عند ضغط زر "إرسال"
                attachedImagePath = openFileDialog.FileName;

                // إبلاغ المستخدم أنه تم إرفاق صورة بنجاح
                lblStatus.Text = $"📎 تم إرفاق صورة: {System.IO.Path.GetFileName(attachedImagePath)}";
                lblStatus.ForeColor = Color.DarkOrange;
                SystemLogger.LogAction("ارفاق صورة", "تم إرفاق صورة للذكاء من قبل الأدمن");

            }
        }
    }
}