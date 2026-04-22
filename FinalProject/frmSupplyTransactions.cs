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
    public partial class frmSupplyTransactions : MaterialForm
    {
        public frmSupplyTransactions()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            InitializeToolTips();

        }
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        int selectedTransactionId = 0;

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();  
        }

        private void frmSupplyTransactions_Load(object sender, EventArgs e)
        {
            LoadComboBoxes();
            LoadTransactions();
        }
        private void LoadComboBoxes()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter daSup = new SqlDataAdapter("SELECT SupplierID, SupplierName FROM Suppliers", conn);
                DataTable dtSup = new DataTable();
                daSup.Fill(dtSup);
                cmbSuppliers.DataSource = dtSup;
                cmbSuppliers.DisplayMember = "SupplierName";
                cmbSuppliers.ValueMember = "SupplierID";
                cmbSuppliers.SelectedIndex = -1;

                SqlDataAdapter daProd = new SqlDataAdapter("SELECT ProductID, ProductName FROM Products", conn);
                DataTable dtProd = new DataTable();
                daProd.Fill(dtProd);
                cmbProducts.DataSource = dtProd;
                cmbProducts.DisplayMember = "ProductName";
                cmbProducts.ValueMember = "ProductID";
                cmbProducts.SelectedIndex = -1;
            }
        }
        private void LoadTransactions()
        {
            string query = @"SELECT t.TransactionID, t.SupplierID, t.ProductID, 
                            s.SupplierName, p.ProductName, 
                            t.Quantity, t.UnitCost, t.DefectPercentage, t.SupplyDate 
                     FROM SupplyTransactions t
                     INNER JOIN Suppliers s ON t.SupplierID = s.SupplierID
                     INNER JOIN Products p ON t.ProductID = p.ProductID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvTransactions.DataSource = dt;

                if (dgvTransactions.Columns.Contains("TransactionID")) dgvTransactions.Columns["TransactionID"].Visible = false;
                if (dgvTransactions.Columns.Contains("SupplierID")) dgvTransactions.Columns["SupplierID"].Visible = false;
                if (dgvTransactions.Columns.Contains("ProductID")) dgvTransactions.Columns["ProductID"].Visible = false;

                dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvTransactions.ReadOnly = true;
                dgvTransactions.AllowUserToAddRows = false;
                dgvTransactions.AllowUserToDeleteRows = false;
            }
        }
        private void ClearFields()
        {
            cmbSuppliers.SelectedIndex = -1;
            cmbProducts.SelectedIndex = -1;
            nudQuantity.Value = 0;
            nudUnitCost.Value = 0;
            nudDefectPercentage.Value = 0;
            dtpSupplyDate.Value = DateTime.Now;
            selectedTransactionId = 0;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (cmbSuppliers.SelectedValue == null || cmbProducts.SelectedValue == null || nudQuantity.Value <= 0)
            {
                MessageBox.Show("Please select Supplier, Product, and enter a valid Quantity!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // بدء المعاملة لربط العمليتين
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // العملية الأولى: إدخال الفاتورة في سجل التوريد
                    string query1 = @"INSERT INTO SupplyTransactions (SupplierID, ProductID, Quantity, UnitCost, DefectPercentage, SupplyDate) 
                              VALUES (@SupID, @ProdID, @Qty, @Cost, @Defect, @Date)";
                    SqlCommand cmd1 = new SqlCommand(query1, conn, transaction);
                    cmd1.Parameters.AddWithValue("@SupID", cmbSuppliers.SelectedValue);
                    cmd1.Parameters.AddWithValue("@ProdID", cmbProducts.SelectedValue);
                    cmd1.Parameters.AddWithValue("@Qty", (int)nudQuantity.Value);
                    cmd1.Parameters.AddWithValue("@Cost", nudUnitCost.Value);
                    cmd1.Parameters.AddWithValue("@Defect", nudDefectPercentage.Value);
                    cmd1.Parameters.AddWithValue("@Date", dtpSupplyDate.Value);
                    cmd1.ExecuteNonQuery();

                    // العملية الثانية: ضخ البضاعة كدفعة جديدة في المستودع
                    string query2 = @"INSERT INTO InventoryBatches (ProductID, Quantity, ReceiveDate) 
                              VALUES (@ProdID, @Qty, @Date)";
                    SqlCommand cmd2 = new SqlCommand(query2, conn, transaction);
                    cmd2.Parameters.AddWithValue("@ProdID", cmbProducts.SelectedValue);
                    cmd2.Parameters.AddWithValue("@Qty", (int)nudQuantity.Value);
                    cmd2.Parameters.AddWithValue("@Date", dtpSupplyDate.Value); // نستخدم نفس تاريخ الفاتورة
                    cmd2.ExecuteNonQuery();

                    // اعتماد العمليتين في قاعدة البيانات
                    transaction.Commit();
                    SystemLogger.LogAction("إدخال بضاعة جديدة", "تمت عملية تزويد بضائع وتسجيل فاتورة من قبل الأدمن");

                }
                catch (Exception ex)
                {
                    // التراجع عن كل شيء في حال حدوث خطأ
                    transaction.Rollback();
                    MessageBox.Show("Error processing transaction: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            LoadTransactions();
            ClearFields();

            // رسالة تأكيد تريح المستخدم
            MessageBox.Show("Transaction saved and inventory updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        
            LoadTransactions();
            ClearFields();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (selectedTransactionId == 0)
            {
                MessageBox.Show("Please select a transaction from the list first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE SupplyTransactions SET SupplierID = @SupID, ProductID = @ProdID, Quantity = @Qty, UnitCost = @Cost, DefectPercentage = @Defect, SupplyDate = @Date WHERE TransactionID = @ID";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SupID", cmbSuppliers.SelectedValue);
                cmd.Parameters.AddWithValue("@ProdID", cmbProducts.SelectedValue);
                cmd.Parameters.AddWithValue("@Qty", (int)nudQuantity.Value);
                cmd.Parameters.AddWithValue("@Cost", nudUnitCost.Value);
                cmd.Parameters.AddWithValue("@Defect", nudDefectPercentage.Value);
                cmd.Parameters.AddWithValue("@Date", dtpSupplyDate.Value);
                cmd.Parameters.AddWithValue("@ID", selectedTransactionId);

                conn.Open();
                cmd.ExecuteNonQuery();
                SystemLogger.LogAction("تعديل فاتورة بضائع", "تمت عملية تعديل فاتورة بضائع من قبل الأدمن");

            }
            LoadTransactions();
            ClearFields();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedTransactionId == 0)
            {
                MessageBox.Show("Please select a transaction from the list first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this transaction?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string query = "DELETE FROM SupplyTransactions WHERE TransactionID = @ID";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ID", selectedTransactionId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    SystemLogger.LogAction("حذف فاتورة", "تمت عملية حذف فاتورة بضائع من قبل الأدمن");

                }
                LoadTransactions();
                ClearFields();
            }

            }

        private void dgvTransactions_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvTransactions.Rows[e.RowIndex];
                selectedTransactionId = Convert.ToInt32(row.Cells["TransactionID"].Value);

                cmbSuppliers.SelectedValue = row.Cells["SupplierID"].Value;
                cmbProducts.SelectedValue = row.Cells["ProductID"].Value;

                nudQuantity.Value = Convert.ToDecimal(row.Cells["Quantity"].Value);
                nudUnitCost.Value = Convert.ToDecimal(row.Cells["UnitCost"].Value);
                nudDefectPercentage.Value = Convert.ToDecimal(row.Cells["DefectPercentage"].Value);
                dtpSupplyDate.Value = Convert.ToDateTime(row.Cells["SupplyDate"].Value);
            }
        }
        // ====================================================
        // 🌟 محرك الاختصارات (Shortcuts) لشاشة التوريد
        // ====================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. إضافة/حفظ معاملة جديدة (Ctrl + S)
            if (keyData == (Keys.Control | Keys.S))
            {
                btnAdd.PerformClick();
                return true;
            }

            // 2. تعديل المعاملة (Ctrl + E)
            else if (keyData == (Keys.Control | Keys.E))
            {
                btnEdit.PerformClick();
                return true;
            }

            // 3. حذف المعاملة (زر Delete في الكيبورد)
            else if (keyData == Keys.Delete)
            {
                // 🛡️ حماية: التأكد من أن المستخدم لا يقوم بحذف أرقام داخل مربعات الإدخال
                if (!nudQuantity.Focused && !nudUnitCost.Focused && !nudDefectPercentage.Focused)
                {
                    btnDelete.PerformClick();
                    return true;
                }
            }

            // 4. الإغلاق والعودة (Esc)
            else if (keyData == Keys.Escape)
            {
                btnBack.PerformClick();
                return true;
            }

            // السماح للويندوز بالتعامل مع باقي الأزرار
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void btnBack_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }
        // ====================================================
        // 🌟 إعداد التلميحات (Hints) للوقوف المطول
        // ====================================================
        private void InitializeToolTips()
        {
            ToolTip hints = new ToolTip();

            // إعدادات التأخير لراحة العين
            hints.InitialDelay = 800;
            hints.AutoPopDelay = 5000;
            hints.ReshowDelay = 200;
            hints.ShowAlways = true;

            // ربط التلميحات بالأزرار
            hints.SetToolTip(btnAdd, "حفظ معاملة التوريد الجديدة (Ctrl + S)");
            hints.SetToolTip(btnEdit, "تعديل معاملة التوريد المحددة (Ctrl + E)");
            hints.SetToolTip(btnDelete, "حذف المعاملة المحددة نهائياً (Delete)");
            hints.SetToolTip(btnBack, "إغلاق الشاشة (Esc)");
        }
        private void frmSupplyTransactions_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this .Close(); 
        }

        private void frmSupplyTransactions_ForeColorChanged(object sender, EventArgs e)
        {

        }
    }
}
