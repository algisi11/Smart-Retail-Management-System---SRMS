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
    public partial class AddProduct : MaterialForm
    {
        public AddProduct()
        {
            InitializeComponent();

            ThemeManager.ApplyGlobalTheme(this);

            if (SessionManager.Role != "Admin")
            {
                nudPrice.Visible = false;
                lblPrice.Visible = false;
            }
            InitializeToolTips();
        }

        private void InitializeToolTips()
        {
            ToolTip hints = new ToolTip();
            hints.InitialDelay = 800;  // الانتظار قليلاً قبل الظهور (تأثير الوقوف المطول)
            hints.AutoPopDelay = 5000;
            hints.ReshowDelay = 200;
            hints.ShowAlways = true;

            // ربط التلميحات بالأزرار
            hints.SetToolTip(btnSave, "حفظ المنتج الجديد وإضافته للمستودع (Ctrl + S)");
            hints.SetToolTip(btnCancel, "إلغاء العملية وإغلاق الشاشة (Esc)");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // اختصار الحفظ (Ctrl + S)
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave.PerformClick();
                return true;
            }
            // اختصار الإلغاء (Esc)
            else if (keyData == Keys.Escape)
            {
                btnCancel.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // 1. التحقق من الحقول الأساسية
            if (string.IsNullOrWhiteSpace(txtBarcode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("الرجاء تعبئة الباركود واسم القطعة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

            // 2. استعلام جدول المنتجات (أضفنا المقاس واللون للملابس)
            string query = @"
    INSERT INTO Products (Barcode, ProductName, Size, Color, ReorderPoint, ABC_Class, SellingPrice)
    VALUES (@Barcode, @Name, @Size, @Color, @ReorderPoint, @ABC, @Price);
    
SELECT SCOPE_IDENTITY();";


            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Barcode", txtBarcode.Text);
                    cmd.Parameters.AddWithValue("@Name", txtName.Text);
                    cmd.Parameters.AddWithValue("@Size", txtSize.Text);
                    cmd.Parameters.AddWithValue("@Color", txtColor.Text);
                    cmd.Parameters.AddWithValue("@Price", nudPrice.Value);
                    // --- هنا وضعنا التعديل الأول الخاص بنقطة إعادة الطلب ---
                    cmd.Parameters.AddWithValue("@ReorderPoint", nudReOrderPoint.Value);
                    // -----------------------------------------------------

                    cmd.Parameters.AddWithValue("@ABC", cmbABC.SelectedItem == null ? "C" : cmbABC.SelectedItem.ToString());

                    try
                    {
                        conn.Open();

                        // حفظ المنتج وجلب رقمه الجديد
                        int newProductID = Convert.ToInt32(cmd.ExecuteScalar());

                        // 3. استعلام جدول المستودع (تاريخ الاستلام يسجل تلقائياً)
                        string batchQuery = @"
                    INSERT INTO InventoryBatches (ProductID, Quantity, ReceiveDate)
                    VALUES (@ProductID, @Quantity, GETDATE())";

                        using (SqlCommand batchCmd = new SqlCommand(batchQuery, conn))
                        {
                            batchCmd.Parameters.AddWithValue("@ProductID", newProductID);

                            // --- هنا وضعنا التعديل الثاني الخاص بالكمية ---
                            batchCmd.Parameters.AddWithValue("@Quantity", nudQuantity.Value);
                            // ----------------------------------------------

                            batchCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("تمت إضافة قطعة الملابس للمستودع بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SystemLogger.LogAction("تمت إضافة منتج جديد", "إضافة منتج جديد من قبل الأدمن");

                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("حدث خطأ أثناء الحفظ: " + ex.Message, "خطأ تقني", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddProduct_Load(object sender, EventArgs e)
        {
            this.ActiveControl = txtBarcode;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddProduct_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();

        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtBarcode_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // منع صوت الرنين المزعج (Ding) في الويندوز
                e.SuppressKeyPress = true;

                // نقل المؤشر فوراً لمربع كتابة الاسم لتبدأ بإدخال بيانات القطعة
                txtName.Focus();
            }
        }
    }
}

