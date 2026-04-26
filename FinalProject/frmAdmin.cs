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
using System.Diagnostics;
using MaterialSkin;
using MaterialSkin.Controls;


namespace FinalProject
{
    public partial class frmAdmin : MaterialForm
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        GeminiAIClient aiClient = new GeminiAIClient();
        private List<string> orderedProductsList = new List<string>();
        string urgentProduct = "";
        int recommendedQty = 0;
        private string currentSupplierPhone = ""; // متغير لتخزين الرقم المجلوب
        private GroqLlamaClient groqClient = new GroqLlamaClient();
        string currentSupplierName = "";
        string currentSupplierEmail = "";
        public frmAdmin()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);

        }
        // متغير لحفظ الشاشة المفتوحة حالياً لتسهيل إغلاقها لاحقاً
        private Dictionary<System.Type, Form> formCache = new Dictionary<System.Type, Form>();
        private Form activeForm = null;

        // الدالة السحرية التي تحقن الشاشات
        private void LoadChildForm(Form childForm)
        {
            // معرفة نوع الشاشة المطلوبة (مثلاً: frmSales أو frmCashier)
            System.Type formType = childForm.GetType();

            // 1. إخفاء الشاشة الحالية بدلاً من إغلاقها وتدميرها (لكي لا نفقد التغييرات)
            if (activeForm != null)
            {
                activeForm.Hide();
            }

            // 2. التحقق مما إذا كانت الشاشة قد تم فتحها مسبقاً وموجودة في الـ Cache
            if (formCache.ContainsKey(formType))
            {
                // نعم، الشاشة موجودة مسبقاً.. استرجعها كما تركها المستخدم بالضبط!
                activeForm = formCache[formType];
            }
            else
            {
                // لا، هذه أول مرة يفتح فيها المستخدم هذه الشاشة في هذه الجلسة
                activeForm = childForm;

                // نزع "هوية" النافذة المستقلة عن الشاشة الجديدة
                activeForm.TopLevel = false;
                activeForm.FormBorderStyle = FormBorderStyle.None;
                activeForm.Dock = DockStyle.Fill;

                // حقن الشاشة داخل الـ Panel
                pnlContainer.Controls.Add(activeForm);
                ThemeManager.ApplyThemeToChildForm(activeForm);

                pnlContainer.Tag = activeForm;

                // 💡 حفظ هذه الشاشة في القاموس لكي لا يتم إنشاؤها مرة أخرى
                formCache.Add(formType, activeForm);
            }

            // 3. إظهار الشاشة النشطة سواء كانت جديدة أو مسترجعة
            activeForm.BringToFront();
            activeForm.Show();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // frmInventory inventoryForm = new frmInventory();
            //inventoryForm.ShowDialog();
            LoadChildForm(new frmInventory());
          

        }

        private void btnEmployees_Click(object sender, EventArgs e)
        {
            //AddEmp empForm = new AddEmp();
            //empForm.ShowDialog();
            LoadChildForm(new AddEmp());
            
        }
        private void RunPredictiveAnalysis()
        {
            // استعلام ذكي جداً يبحث في حركات التوريد السابقة لجلب رقم أحدث مورد
            string query = @"
    WITH ProductVelocity AS (
        SELECT 
            p.ProductID, 
            p.ProductName,
            ISNULL((SELECT SUM(sid.Quantity) 
                    FROM SalesInvoiceDetails sid 
                    JOIN SalesInvoices si ON sid.InvoiceID = si.InvoiceID 
                    WHERE sid.ProductID = p.ProductID AND si.InvoiceDate >= DATEADD(day, -30, GETDATE())), 0) / 30.0 AS DailyBurnRate,
            ISNULL((SELECT SUM(Quantity) FROM InventoryBatches WHERE ProductID = p.ProductID), 0) AS CurrentStock
        FROM Products p
    )
    SELECT TOP 1
        pv.ProductName, 
        pv.CurrentStock, 
        pv.DailyBurnRate,
        ISNULL(LatestSupplier.Phone, '962790000000') AS SupplierPhone, -- جلبنا عمود Phone من المورد
        CASE WHEN pv.DailyBurnRate > 0 THEN pv.CurrentStock / pv.DailyBurnRate ELSE 999 END AS DaysUntilOut
    FROM ProductVelocity pv
    -- السحر هنا: نبحث عن أحدث فاتورة شراء لهذا المنتج لنجلب رقم المورد
    OUTER APPLY (
        SELECT TOP 1 s.Phone
        FROM SupplyTransactions st
        JOIN Suppliers s ON st.SupplierID = s.SupplierID
        WHERE st.ProductID = pv.ProductID
        ORDER BY st.SupplyDate DESC -- ترتيب تنازلي لنجلب أحدث مورد
    ) AS LatestSupplier
    WHERE pv.DailyBurnRate > 0 AND (pv.CurrentStock / pv.DailyBurnRate) <= 365 
    ORDER BY pv.ProductID DESC;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            urgentProduct = reader["ProductName"].ToString();
                            currentSupplierPhone = reader["SupplierPhone"].ToString();
                            int daysLeft = Convert.ToInt32(reader["DaysUntilOut"]);

                            lblAIAlert.Text = $"⚠️ تنبيه: صنف [{urgentProduct}] سينفد خلال ({daysLeft}) أيام!\nالمورد المعتمد: {currentSupplierPhone}";
                            pnlAICommand.Visible = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في محرك التنبؤ: " + ex.Message, "تشخيص");
                }
            }
        }
        private void frmAdmin_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Restart();


        }
       
        private void frmAdmin_Load(object sender, EventArgs e)
        {
            pnlAICommand.Visible = false;
            RunPredictiveAnalysis();



        }



        private void btnSuppliers_Click(object sender, EventArgs e)
        {
            //frmSuppliers addSupplier = new frmSuppliers();
           // addSupplier.ShowDialog();
           LoadChildForm(new frmSuppliers());
        }

        private void btnShipment_Click(object sender, EventArgs e)
        {
            //frmSupplyTransactions newShipment = new frmSupplyTransactions();
            //newShipment.ShowDialog();
            LoadChildForm(new frmSupplyTransactions());
        }

        private void btnCashier_Click(object sender, EventArgs e)
        {
            // frmCashier newCash = new frmCashier();
            // newCash.ShowDialog();
            LoadChildForm(new frmCashier());
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            //frmAdminDashboard NewDashboard = new frmAdminDashboard();
             //NewDashboard.ShowDialog();
             LoadChildForm (new frmAdminDashboard());
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        // تأكد من إضافة هذه المكتبات في أعلى الصفحة إذا لم تكن موجودة
        // using System.Diagnostics;
        // using System.Linq;

        private void LoadNextUrgentProduct()
        {
            try
            {
                string excludedProducts = "''";
                if (orderedProductsList.Count > 0)
                {
                    var safeList = orderedProductsList.Select(p => p.Replace("'", "''"));
                    excludedProducts = "'" + string.Join("','", safeList) + "'";
                }

                // الاستعلام المصحح 100%
                string query = $@"
DECLARE @BestSupplierID INT;

-- تحديد أفضل مورد في النظام
SELECT TOP 1 @BestSupplierID = s.SupplierID
FROM Suppliers s
LEFT JOIN SupplyTransactions st ON s.SupplierID = st.SupplierID
GROUP BY s.SupplierID, s.AgreedLeadTime
ORDER BY s.AgreedLeadTime ASC, ISNULL(AVG(st.DefectPercentage), 0) ASC;

-- الاستعلام الرئيسي
SELECT TOP 1 
    p.ProductName, 
    (10 - ISNULL(SUM(b.Quantity), 0)) as RecommendedQty,
    
    ISNULL(
        (SELECT TOP 1 s.SupplierName FROM SupplyTransactions st JOIN Suppliers s ON st.SupplierID = s.SupplierID WHERE st.ProductID = p.ProductID ORDER BY st.SupplyDate DESC),
        (SELECT SupplierName FROM Suppliers WHERE SupplierID = @BestSupplierID)
    ) AS SupName,
    
    ISNULL(
        (SELECT TOP 1 s.Phone FROM SupplyTransactions st JOIN Suppliers s ON st.SupplierID = s.SupplierID WHERE st.ProductID = p.ProductID ORDER BY st.SupplyDate DESC),
        (SELECT Phone FROM Suppliers WHERE SupplierID = @BestSupplierID)
    ) AS SupPhone,
    
    ISNULL(
        (SELECT TOP 1 s.Email FROM SupplyTransactions st JOIN Suppliers s ON st.SupplierID = s.SupplierID WHERE st.ProductID = p.ProductID ORDER BY st.SupplyDate DESC),
        (SELECT Email FROM Suppliers WHERE SupplierID = @BestSupplierID)
    ) AS SupEmail,

    -- 🌟 تم تصحيح حرف الـ st هنا
    CASE 
        WHEN (SELECT TOP 1 st.SupplierID FROM SupplyTransactions st WHERE st.ProductID = p.ProductID) IS NULL THEN 1 
        ELSE 0 
    END AS IsSmartAlternative

FROM Products p
LEFT JOIN InventoryBatches b ON p.ProductID = b.ProductID
WHERE p.ProductName NOT IN ({excludedProducts})
GROUP BY p.ProductID, p.ProductName
HAVING ISNULL(SUM(b.Quantity), 0) <= 5";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            urgentProduct = reader["ProductName"].ToString();
                            currentSupplierName = reader["SupName"]?.ToString() ?? "غير معروف";
                            currentSupplierPhone = reader["SupPhone"]?.ToString() ?? "";
                            currentSupplierEmail = reader["SupEmail"]?.ToString() ?? "";
                            recommendedQty = reader["RecommendedQty"] != DBNull.Value ? Convert.ToInt32(reader["RecommendedQty"]) : 10;

                            bool isSmartAlternative = Convert.ToInt32(reader["IsSmartAlternative"]) == 1;

                            string alertMessage = $"⚠️ تنبيه: صنف [{urgentProduct}] سينفد!\n";

                            if (isSmartAlternative)
                            {
                                alertMessage += $"🌟 المقترح (الأسرع والأجود): {currentSupplierName}\n";
                            }
                            else
                            {
                                alertMessage += $"المورد المعتمد: {currentSupplierName}\n";
                            }

                            alertMessage += $"الهاتف: {currentSupplierPhone}\n" +
                                            $"الإيميل: {(string.IsNullOrEmpty(currentSupplierEmail) ? "❌ غير مسجل" : currentSupplierEmail)}";

                            lblAIAlert.Text = alertMessage;
                            lblRecommendedQty.Text = $"الكمية المطلوبة: {recommendedQty}";
                            pnlAICommand.Visible = true;
                        }
                        else
                        {
                            MessageBox.Show("تمت معالجة جميع النواقص بنجاح!", "اكتملت المهمة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            pnlAICommand.Visible = false;
                            orderedProductsList.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ تقني: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void btnAutoRestock_Click(object sender, EventArgs e)
        {
            btnAutoRestock.Enabled = false;
            btnAutoRestock.Text = "⏳ جاري صياغة الطلب عبر Groq...";

            try
            {
                int orderQty = recommendedQty > 0 ? recommendedQty : 50;

                string prompt = $"قم بصياغة رسالة توريد تجارية قصيرة ومباشرة للمورد، أطلب فيها توريد كمية ({orderQty}) حبة من صنف ({urgentProduct}). ابدأ بـ 'مرحبا' وانهِ الرسالة بشكر. لا تضع أي ردود أو مقدمات، أريد نص الرسالة فقط لنسخها.";

                string systemMessage = "تجاهل بيانات النظام، أنت مساعد ذكي خبير في صياغة رسائل المشتريات المباشرة.";
                string aiDraft = await groqClient.AskAIAsync(systemMessage, prompt);

                DialogResult dialogResult = MessageBox.Show(
                    "قام الذكاء الاصطناعي بصياغة طلب التوريد التالي:\n\n" + aiDraft + "\n\nكيف تود إرسال هذا الطلب للمورد؟\nاختر [Yes] للواتساب، أو [No] للإيميل.",
                    "خيارات الإرسال",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Cancel)
                {
                    return;
                }

                // تشفير الرسالة لتكون متوافقة مع الروابط (URL Encoding)
                string encodedMessage = Uri.EscapeDataString(aiDraft);

                if (dialogResult == DialogResult.Yes)
                {
                    // ================== مسار الواتساب ==================
                    string cleanPhone = "";
                    if (!string.IsNullOrEmpty(currentSupplierPhone))
                    {
                        cleanPhone = new string(currentSupplierPhone.Where(char.IsDigit).ToArray());
                        if (cleanPhone.StartsWith("0")) cleanPhone = "962" + cleanPhone.Substring(1);
                    }

                    if (string.IsNullOrEmpty(cleanPhone))
                    {
                        Clipboard.SetText(aiDraft);
                        MessageBox.Show("لا يوجد رقم مورد مسجل لهذا الصنف (أو لم يتم توريده سابقاً).\n\nتم نسخ رسالة الذكاء الاصطناعي إلى الحافظة (Clipboard) لتتمكن من إرسالها يدوياً.", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string whatsappUrl = $"https://api.whatsapp.com/send?phone={cleanPhone}&text={encodedMessage}";
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = whatsappUrl, UseShellExecute = true }); }
                        catch { System.Diagnostics.Process.Start("explorer.exe", $"\"{whatsappUrl}\""); }
                    }
                }
                else if (dialogResult == DialogResult.No)
                {
                    // ================== مسار الإيميل (Gmail المباشر) ==================
                    if (string.IsNullOrEmpty(currentSupplierEmail) || currentSupplierEmail.Contains("غير مسجل"))
                    {
                        Clipboard.SetText(aiDraft);
                        MessageBox.Show("البريد الإلكتروني للمورد غير مسجل لهذا الصنف.\n\nتم نسخ رسالة الذكاء الاصطناعي إلى الحافظة (Clipboard) لتتمكن من إرسالها يدوياً من إيميلك.", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // تشفير عنوان الرسالة أيضاً
                        string subject = Uri.EscapeDataString($"طلب توريد بضاعة: {urgentProduct}");

                        // 🌟 الرابط السحري الخاص بـ Gmail (يقوم بفتح نافذة إنشاء رسالة جديدة وتعبئة الحقول)
                        string gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&to={currentSupplierEmail}&su={subject}&body={encodedMessage}";

                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = gmailUrl, UseShellExecute = true });
                        }
                        catch
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"\"{gmailUrl}\"");
                        }
                    }
                }

                // تحديث القائمة وجلب المنتج التالي
                orderedProductsList.Add(urgentProduct);
                LoadNextUrgentProduct();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الاتصال: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAutoRestock.Text = "💡 تنفيذ طلب توريد";
                btnAutoRestock.Enabled = true;
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnDarkMode_Click(object sender, EventArgs e)
        {
            ThemeManager.ToggleDarkMode(this);
        }
    }
}
    

