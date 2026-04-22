using MaterialSkin.Controls;
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

namespace FinalProject
{
    public partial class frmReturns : MaterialForm
    {

        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        public frmReturns()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            InitializeToolTips();
        }
        private void InitializeToolTips()
        {
            ToolTip hints = new ToolTip();
            hints.SetToolTip(txtInvoiceSearch, "البحث عن الفاتورة (Enter)");
            hints.SetToolTip(btnReturnItem, "إرجاع القطعة المحددة (Ctrl + R)");
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.R)) { btnReturnItem.PerformClick(); return true; }
            if (keyData == Keys.Escape) { this.Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void frmReturns_Load(object sender, EventArgs e)
        {

        }

        private void btnReturnItem_Click(object sender, EventArgs e)
        {
            if (dgvInvoiceDetails.CurrentRow == null)
            {
                MessageBox.Show("الرجاء تحديد المنتج المراد إرجاعه من الفاتورة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int productId = Convert.ToInt32(dgvInvoiceDetails.CurrentRow.Cells["ProductID"].Value);
            string productName = dgvInvoiceDetails.CurrentRow.Cells["المنتج"].Value.ToString();
            int soldQty = Convert.ToInt32(dgvInvoiceDetails.CurrentRow.Cells["الكمية المباعة"].Value);
            decimal unitPrice = Convert.ToDecimal(dgvInvoiceDetails.CurrentRow.Cells["سعر الوحدة"].Value);

            // طلب الكمية المراد إرجاعها
            string input = Microsoft.VisualBasic.Interaction.InputBox($"كم قطعة تود إرجاعها من ({productName})؟\nالكمية في الفاتورة: {soldQty}", "إرجاع بضاعة", "1");

            if (int.TryParse(input, out int returnQty) && returnQty > 0 && returnQty <= soldQty)
            {
                decimal refundAmount = returnQty * unitPrice;
                string currentUser = string.IsNullOrEmpty(SessionManager.EmployeeName) ? "Admin" : SessionManager.EmployeeName;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // أ. تسجيل حركة المرتجع
                        string q1 = "INSERT INTO ReturnTransactions (InvoiceNo, ProductName, ReturnedQty, RefundAmount, HandledBy) VALUES (@Inv, @Prod, @Qty, @Amt, @User)";
                        SqlCommand cmd1 = new SqlCommand(q1, conn, transaction);
                        cmd1.Parameters.AddWithValue("@Inv", txtInvoiceSearch.Text);
                        cmd1.Parameters.AddWithValue("@Prod", productName);
                        cmd1.Parameters.AddWithValue("@Qty", returnQty);
                        cmd1.Parameters.AddWithValue("@Amt", refundAmount);
                        cmd1.Parameters.AddWithValue("@User", currentUser);
                        cmd1.ExecuteNonQuery();

                        // ب. إعادة البضاعة للمستودع (إنشاء دفعة جديدة)
                        string q2 = "INSERT INTO InventoryBatches (ProductID, Quantity, ReceiveDate) VALUES (@ProdID, @Qty, GETDATE())";
                        SqlCommand cmd2 = new SqlCommand(q2, conn, transaction);
                        cmd2.Parameters.AddWithValue("@ProdID", productId);
                        cmd2.Parameters.AddWithValue("@Qty", returnQty);
                        cmd2.ExecuteNonQuery();

                        // ج. الاعتماد والتسجيل الأمني
                        transaction.Commit();
                        SystemLogger.LogAction("مرتجع مبيعات", $"تم استرجاع {returnQty} قطعة من {productName} لفاتورة رقم {txtInvoiceSearch.Text}");

                        MessageBox.Show($"تم استرجاع القطعة بنجاح.\nالمبلغ المسترد للعميل: {refundAmount:N2} JD", "عملية ناجحة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("خطأ أثناء الإرجاع: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("الكمية المدخلة غير صحيحة أو تتجاوز الكمية المباعة!", "خطأ إدخال", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        

        private void txtInvoiceSearch_TextChanged(object sender, EventArgs e)
        {
            // حماية: إذا كان المربع فارغاً، نفرغ الجدول ولا ننفذ البحث
            if (string.IsNullOrWhiteSpace(txtInvoiceSearch.Text))
            {
                dgvInvoiceDetails.DataSource = null;
                return;
            }

            // 🌟 التعديل هنا: استخدمنا (d.Total / d.Quantity) لحساب سعر الوحدة بدلاً من استدعاء عمود غير موجود
            string query = @"
        SELECT p.ProductID, p.ProductName AS 'المنتج', d.Quantity AS 'الكمية المباعة', 
               (d.Total / d.Quantity) AS 'سعر الوحدة', d.Total AS 'الإجمالي'
        FROM SalesInvoiceDetails d
        JOIN Products p ON d.ProductID = p.ProductID
        JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
        WHERE i.InvoiceNo LIKE @InvoiceNo + '%'"; // أضفنا LIKE لكي يبحث مع كل حرف يتم كتابته

            string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@InvoiceNo", txtInvoiceSearch.Text.Trim());
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgvInvoiceDetails.DataSource = dt;

                    // إخفاء عمود الـ ID لأنه برمجي ولا يهم الكاشير
                    if (dgvInvoiceDetails.Columns.Contains("ProductID"))
                        dgvInvoiceDetails.Columns["ProductID"].Visible = false;

                    dgvInvoiceDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
                catch (Exception ex)
                {
                    // إظهار الخطأ في حال وجود مشكلة أخرى في قاعدة البيانات
                    MessageBox.Show("حدث خطأ أثناء البحث: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void dgvInvoiceDetails_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
