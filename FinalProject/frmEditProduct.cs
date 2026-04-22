using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using MaterialSkin;
using MaterialSkin.Controls;


namespace FinalProject
{
    public partial class frmEditProduct : MaterialForm
    {
        // متغير لحفظ بيانات المنتج القادم من الجدول
        ProductDTO _currentProduct;

        // تعديل الـ Constructor لاستقبال الكائن
        public frmEditProduct(ProductDTO product)
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            InitializeToolTips();

            _currentProduct = product;

            txtBarcode.Text = product.Barcode;
            txtName.Text = product.ProductName;
            txtSize.Text = product.Size;
            txtColor.Text = product.Color;
            nudReOrderPoint.Value = product.ReorderPoint;
            cmbABC.SelectedItem = product.ABC_Class;
            nudPrice.Value = product.SellingPrice; // تعبئة السعر

            if (SessionManager.Role != "Admin")
            {
                nudPrice.Visible = false;
                lblPrice.Visible = false;
            }

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

            // ربط التلميحات بالأزرار
            hints.SetToolTip(btnSave, "حفظ التعديلات (Ctrl + S)");
            hints.SetToolTip(btnCancel, "إلغاء التعديلات وإغلاق الشاشة (Esc)");
        }


        // ====================================================
        // 🌟 محرك الاختصارات (Shortcuts)
        // ====================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. اختصار الحفظ (Ctrl + S)
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave.PerformClick(); // محاكاة ضغطة زر الحفظ
                return true;
            }

            // 2. اختصار الإلغاء (Esc)
            else if (keyData == Keys.Escape)
            {
                btnCancel.PerformClick(); // محاكاة ضغطة زر الإلغاء
                return true;
            }

            // السماح للويندوز بالتعامل مع باقي الأزرار بشكل طبيعي
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void frmEditProduct_Load(object sender, EventArgs e)
        {
            // يمكنك تركها فارغة أو إضافة أي لمسات جمالية عند تحميل الشاشة
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // 1. تحقق سريع لضمان عدم ترك الحقول الأساسية فارغة
            if (string.IsNullOrWhiteSpace(txtBarcode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("الرجاء التأكد من إدخال الباركود واسم المنتج", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

            // 2. استعلام التحديث (نحدث كل الحقول بناءً على رقم المنتج)
            string query = @"
    UPDATE Products 
    SET Barcode=@b, ProductName=@n, Size=@s, Color=@c, ReorderPoint=@r, ABC_Class=@a, SellingPrice=@Price 
    WHERE ProductID=@id";


            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);

                    // 3. أخذ البيانات الجديدة من الشاشة
                    cmd.Parameters.AddWithValue("@Barcode", txtBarcode.Text);
                    cmd.Parameters.AddWithValue("@Name", txtName.Text);
                    cmd.Parameters.AddWithValue("@Size", txtSize.Text);
                    cmd.Parameters.AddWithValue("@Color", txtColor.Text);
                    cmd.Parameters.AddWithValue("@ReorderPoint", nudReOrderPoint.Value);
                    cmd.Parameters.AddWithValue("@Price", nudPrice.Value);
                    // التأكد من اختيار قيمة، وإذا لم يختر نضع C كافتراضي
                    cmd.Parameters.AddWithValue("@ABC", cmbABC.SelectedItem == null ? "C" : cmbABC.SelectedItem.ToString());

                    // 4. الأهم: استخدام رقم المنتج القديم لكي لا نحدث منتجاً بالخطأ!
                    cmd.Parameters.AddWithValue("@ProductID", _currentProduct.ProductID);

                    // 5. فتح الاتصال وتنفيذ التحديث
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("تم حفظ التعديلات بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // إغلاق الشاشة والعودة للمستودع
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء التعديل: " + ex.Message, "خطأ فني", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close(); // إغلاق بدون حفظ
        }

        private void btnCancel_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBarcode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("الرجاء التأكد من إدخال الباركود واسم المنتج", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

            string query = @"
    UPDATE Products 
    SET Barcode = @Barcode, 
        ProductName = @Name, 
        Size = @Size, 
        Color = @Color, 
        ReorderPoint = @ReorderPoint, 
        ABC_Class = @ABC,
        SellingPrice = @Price
    WHERE ProductID = @ProductID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@Barcode", txtBarcode.Text);
                    cmd.Parameters.AddWithValue("@Name", txtName.Text);
                    cmd.Parameters.AddWithValue("@Size", txtSize.Text);
                    cmd.Parameters.AddWithValue("@Color", txtColor.Text);
                    cmd.Parameters.AddWithValue("@ReorderPoint", nudReOrderPoint.Value);
                    cmd.Parameters.AddWithValue("@ABC", cmbABC.SelectedItem == null ? "C" : cmbABC.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@Price", nudPrice.Value);
                    cmd.Parameters.AddWithValue("@ProductID", _currentProduct.ProductID);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("تم حفظ التعديلات بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SystemLogger.LogAction("تعديل منتج", "تم تعديل منتج موجود من قبل الأدمن");

                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء التعديل: " + ex.Message, "خطأ فني", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}