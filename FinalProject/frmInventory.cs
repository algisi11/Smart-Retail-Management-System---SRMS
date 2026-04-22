using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BarcodeLib;
using MaterialSkin;
using MaterialSkin.Controls;


namespace FinalProject
{
    public partial class frmInventory : MaterialForm

    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        int _labelsPrinted = 0;
        int _totalLabelsToPrint = 1;
        public frmInventory()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            InitializeToolTips();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblTimeNow.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        private void frmInventory_Load(object sender, EventArgs e)
        {
            if (SessionManager.Role == "Admin")
            {
                this.FormStyle = MaterialSkin.Controls.MaterialForm.FormStyles.StatusAndActionBar_None;
            }
            lblEmpName.Text = SessionManager.EmployeeName;
            LoadProductsData();
        }

        private void LoadProductsData()
        {
            string query = @"
SELECT 
    p.ProductID, 
    p.Barcode AS 'Barcode',
    p.ProductName AS 'Product Name',
    p.Size AS 'Size',
    p.Color AS 'Color',
    p.SellingPrice AS 'Price', 
    ISNULL(SUM(ib.Quantity), 0) AS 'Quantity',
    p.ReorderPoint AS 'ReOrder Point',
    p.ABC_Class AS 'Quality ABC'
FROM Products p
LEFT JOIN InventoryBatches ib ON p.ProductID = ib.ProductID
GROUP BY p.ProductID, p.Barcode, p.ProductName, p.Size, p.Color, p.SellingPrice, p.ReorderPoint, p.ABC_Class";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvProducts.DataSource = dt;

                    // إخفاء الأعمدة حسب الصلاحيات
                    if (dgvProducts.Columns.Contains("ProductID"))
                        dgvProducts.Columns["ProductID"].Visible = false;

                    if (SessionManager.Role != "Admin")
                    {
                        if (dgvProducts.Columns.Contains("Price"))
                            dgvProducts.Columns["Price"].Visible = false;
                    }

                    dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    // ------------------------------------------------------------------
                    // 🌟 السحر البصري: تلوين الصفوف بناءً على الكمية وحد الطلب
                    // ------------------------------------------------------------------
                   
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ تقني: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
       

        private void txtAdd_Click(object sender, EventArgs e)
        {
            AddProduct addForm = new AddProduct();
            addForm.ShowDialog();
            LoadProductsData();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            SearchProducts(textBox1.Text);
        }

        private void SearchProducts(string searchText)
        {
            string query = @"
            SELECT 
                p.ProductID,
                p.Barcode AS 'Barcode',
                p.ProductName AS 'Product Name',
                p.Size AS 'Size',
                p.Color AS 'Color',
                ISNULL(SUM(ib.Quantity), 0) AS 'Quantity',
                p.ReorderPoint AS 'ReOrder Point',
                p.ABC_Class AS 'Quality ABC'
            FROM 
                Products p
            LEFT JOIN 
                InventoryBatches ib ON p.ProductID = ib.ProductID
            WHERE 
                p.ProductName LIKE @search OR p.Barcode LIKE @search
            GROUP BY 
                p.ProductID, p.Barcode, p.ProductName, p.Size, p.Color, p.ReorderPoint, p.ABC_Class";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@search", "%" + searchText + "%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvProducts.DataSource = dt;

                    if (dgvProducts.Columns.Contains("ProductID"))
                        dgvProducts.Columns["ProductID"].Visible = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                try
                {
                    ProductDTO selectedProduct = new ProductDTO
                    {
                        ProductID = Convert.ToInt32(dgvProducts.CurrentRow.Cells["ProductID"].Value),
                        Barcode = dgvProducts.CurrentRow.Cells["Barcode"].Value.ToString(),
                        ProductName = dgvProducts.CurrentRow.Cells["Product Name"].Value.ToString(),
                        Size = dgvProducts.CurrentRow.Cells["Size"].Value.ToString(),
                        Color = dgvProducts.CurrentRow.Cells["Color"].Value.ToString(),
                        ReorderPoint = Convert.ToInt32(dgvProducts.CurrentRow.Cells["ReOrder Point"].Value),
                        ABC_Class = dgvProducts.CurrentRow.Cells["Quality ABC"].Value.ToString(),
                        SellingPrice = Convert.ToDecimal(dgvProducts.CurrentRow.Cells["Price"].Value)

                    };
                    

                    frmEditProduct editForm = new frmEditProduct(selectedProduct);
                    editForm.ShowDialog();
                    LoadProductsData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في قراءة بيانات السطر المختار: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار منتج من الجدول أولاً");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                int id = Convert.ToInt32(dgvProducts.CurrentRow.Cells["ProductID"].Value);
                string name = dgvProducts.CurrentRow.Cells["Product Name"].Value.ToString();

                if (MessageBox.Show($"هل أنت متأكد من حذف المنتج ({name}) نهائياً؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    string deleteQuery = "DELETE FROM InventoryBatches WHERE ProductID=@id; DELETE FROM Products WHERE ProductID=@id;";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        try
                        {
                            SqlCommand cmd = new SqlCommand(deleteQuery, conn);
                            cmd.Parameters.AddWithValue("@id", id);
                            conn.Open();
                            cmd.ExecuteNonQuery();

                            MessageBox.Show("تم الحذف بنجاح");
                            LoadProductsData();
                            SystemLogger.LogAction("حذف منتج", "تمت عملية حذف منتج");

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("حدث خطأ أثناء الحذف: " + ex.Message);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار منتج لحذفه");
            }
        }

        private void btnPrintBarcode_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                Form prompt = new Form()
                {
                    Width = 350,
                    Height = 160,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = "تحديد عدد الملصقات",
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                Label textLabel = new Label() { Left = 20, Top = 20, Width = 300, Text = "أدخل عدد الملصقات المراد طباعتها لهذا المنتج:" };

                NumericUpDown nudCount = new NumericUpDown() { Left = 20, Top = 50, Width = 290, Minimum = 1, Maximum = 1000, Value = 1 };

                Button confirmation = new Button() { Text = "موافق وطباعة", Left = 210, Width = 100, Top = 80, DialogResult = DialogResult.OK };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(nudCount);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation; 

                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    _totalLabelsToPrint = (int)nudCount.Value;

                    _labelsPrinted = 0;

                    printPreviewDialog1.Document = printDocument1;
                    printPreviewDialog1.WindowState = FormWindowState.Maximized;
                    printPreviewDialog1.ShowDialog();
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار منتج من الجدول لطباعة الباركود الخاص به", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            string barcodeText = dgvProducts.CurrentRow.Cells["Barcode"].Value.ToString();
            string productName = dgvProducts.CurrentRow.Cells["Product Name"].Value.ToString();
            string productSize = dgvProducts.CurrentRow.Cells["Size"].Value.ToString();

            BarcodeLib.Barcode barcode = new BarcodeLib.Barcode();
            Image barcodeImage = barcode.Encode(BarcodeLib.TYPE.CODE128, barcodeText, Color.Black, Color.White, 250, 80);

          
            Font titleFont = new Font("Arial", 14, FontStyle.Bold);
            Font detailFont = new Font("Arial", 10, FontStyle.Regular);
            Brush brush = Brushes.Black;

            e.Graphics.DrawString(productName, titleFont, brush, new Point(20, 20));
            e.Graphics.DrawString("Size: " + productSize, detailFont, brush, new Point(20, 45));
            e.Graphics.DrawImage(barcodeImage, new Point(20, 70));

           

            _labelsPrinted++; 

            if (_labelsPrinted < _totalLabelsToPrint)
            {
                e.HasMorePages = true;  
            }
            else
            {
                e.HasMorePages = false; 
            }
            SystemLogger.LogAction("طباعة باركود", "تمت عملية طباعة باركود منتج");

        }
        // ====================================================
        // 🌟 إعداد التلميحات (Hints) للوقوف المطول
        // ====================================================
        private void InitializeToolTips()
        {
            ToolTip hints = new ToolTip();

            // إعدادات وقت الظهور
            hints.InitialDelay = 800;
            hints.AutoPopDelay = 5000;
            hints.ReshowDelay = 200;
            hints.ShowAlways = true;

            // ربط التلميحات بالأزرار
            hints.SetToolTip(txtAdd, "إضافة منتج جديد للمستودع (Ctrl + N)");
            hints.SetToolTip(btnEdit, "تعديل بيانات المنتج المحدد (Ctrl + E)");
            hints.SetToolTip(btnDelete, "حذف المنتج المحدد نهائياً (Delete)");
            hints.SetToolTip(btnPrintBarcode, "طباعة ملصق باركود للمنتج المحدد (Ctrl + P)");
            hints.SetToolTip(textBox1, "مربع البحث السريع (Ctrl + F)");
            hints.SetToolTip(btnClose, "إغلاق الشاشة (Esc)");
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();

        }

        // ====================================================
        // 🌟 محرك الاختصارات (Shortcuts) لشاشة المستودع
        // ====================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. إضافة منتج جديد (Ctrl + N)
            if (keyData == (Keys.Control | Keys.N))
            {
                txtAdd.PerformClick(); // افترضنا أن اسم الزر لديك هو txtAdd بناءً على الكود المرسل
                return true;
            }

            // 2. تعديل المنتج (Ctrl + E)
            else if (keyData == (Keys.Control | Keys.E))
            {
                btnEdit.PerformClick();
                return true;
            }

            // 3. طباعة الباركود (Ctrl + P)
            else if (keyData == (Keys.Control | Keys.P))
            {
                btnPrintBarcode.PerformClick();
                return true;
            }

            // 4. حذف المنتج (زر Delete في الكيبورد)
            else if (keyData == Keys.Delete)
            {
                // نتحقق أولاً أن المستخدم لا يكتب داخل مربع البحث لتجنب الحذف بالخطأ
                if (!textBox1.Focused)
                {
                    btnDelete.PerformClick();
                    return true;
                }
            }

            // 5. البحث السريع (Ctrl + F) - ينقل المؤشر فوراً لمربع البحث
            else if (keyData == (Keys.Control | Keys.F))
            {
                textBox1.Focus();
                textBox1.SelectAll(); // تظليل النص القديم ليسهل الكتابة فوقه
                return true;
            }

            // 6. الإغلاق (Esc)
            else if (keyData == Keys.Escape)
            {
                btnClose.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void frmInventory_FormClosed(object sender, FormClosedEventArgs e)
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

        private void frmInventory_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void dgvProducts_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // هذا الكود سيعمل تلقائياً كلما تم تحديث البيانات أو ترتيبها
            foreach (DataGridViewRow row in dgvProducts.Rows)
            {
                if (!row.IsNewRow)
                {
                    // حماية إضافية: التأكد من أن الخلايا ليست فارغة (Null) لتجنب انهيار البرنامج
                    if (row.Cells["Quantity"].Value != DBNull.Value && row.Cells["ReOrder Point"].Value != DBNull.Value)
                    {
                        int qty = Convert.ToInt32(row.Cells["Quantity"].Value);
                        int reorderPoint = Convert.ToInt32(row.Cells["ReOrder Point"].Value);

                        // الحالة الأولى: تخطت حد الطلب (خطر - أحمر فاتح)
                        if (qty < reorderPoint)
                        {
                            row.DefaultCellStyle.BackColor = Color.LightCoral;
                        }
                        // الحالة الثانية: اقتربت من حد الطلب (تحذير - أصفر فاتح)
                        else if (qty >= reorderPoint && qty <= reorderPoint + 3)
                        {
                            row.DefaultCellStyle.BackColor = Color.LemonChiffon;
                        }
                        // الحالة الطبيعية (أبيض)
                        else
                        {
                            row.DefaultCellStyle.BackColor = Color.White;
                        }
                    }
                }
            }
        }
    }
}
