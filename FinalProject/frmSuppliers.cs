using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
namespace FinalProject
{
    public partial class frmSuppliers : MaterialForm
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        int selectedSupplierId = 0;

        public frmSuppliers()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            InitializeToolTips();
        }

        private void frmSuppliers_Load(object sender, EventArgs e)
        {
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            string query = "SELECT SupplierID, SupplierName, Phone, Email, AgreedLeadTime FROM Suppliers";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvSuppliers.DataSource = dt;

                if (dgvSuppliers.Columns.Contains("SupplierID"))
                    dgvSuppliers.Columns["SupplierID"].Visible = false;

                dgvSuppliers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvSuppliers.ReadOnly = true;
                dgvSuppliers.AllowUserToAddRows = false;
                dgvSuppliers.AllowUserToDeleteRows = false;
            }
        }

        private void ClearFields()
        {
            txtSupName.Clear();
            txtPhone.Clear();
            txtEmail.Clear(); // تفريغ الإيميل
            nudLeadTime.Value = 1;
            selectedSupplierId = 0;
            txtSupName.Focus();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSupName.Text) || string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Please fill the required fields (Name and Phone)!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string checkQuery = "SELECT COUNT(*) FROM Suppliers WHERE SupplierName = @Name";
                SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@Name", txtSupName.Text.Trim());

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    MessageBox.Show("This supplier already exists in the system!", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // إضافة الإيميل لجملة الحفظ
                string query = "INSERT INTO Suppliers (SupplierName, Phone, Email, AgreedLeadTime) VALUES (@Name, @Phone, @Email, @LeadTime)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", txtSupName.Text.Trim());
                cmd.Parameters.AddWithValue("@Phone", txtPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim()); // هنا
                cmd.Parameters.AddWithValue("@LeadTime", (int)nudLeadTime.Value);

                cmd.ExecuteNonQuery();
            }
            LoadSuppliers();
            ClearFields();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (selectedSupplierId == 0) return;

            // إضافة الإيميل لجملة التعديل
            string query = "UPDATE Suppliers SET SupplierName = @Name, Phone = @Phone, Email = @Email, AgreedLeadTime = @LeadTime WHERE SupplierID = @ID";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", txtSupName.Text.Trim());
                cmd.Parameters.AddWithValue("@Phone", txtPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim()); // هنا
                cmd.Parameters.AddWithValue("@LeadTime", (int)nudLeadTime.Value);
                cmd.Parameters.AddWithValue("@ID", selectedSupplierId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            LoadSuppliers();
            ClearFields();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedSupplierId == 0)
            {
                MessageBox.Show("Please select a supplier from the list first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this supplier?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string query = "DELETE FROM Suppliers WHERE SupplierID = @ID";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ID", selectedSupplierId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                LoadSuppliers();
                ClearFields();
            }
        }

        private void dgvSuppliers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvSuppliers.Rows[e.RowIndex];
                selectedSupplierId = Convert.ToInt32(row.Cells["SupplierID"].Value);
                txtSupName.Text = row.Cells["SupplierName"].Value.ToString();
                txtPhone.Text = row.Cells["Phone"].Value.ToString();

                // جلب الإيميل
                txtEmail.Text = row.Cells["Email"].Value != DBNull.Value ? row.Cells["Email"].Value.ToString() : "";

                nudLeadTime.Value = row.Cells["AgreedLeadTime"].Value != DBNull.Value ? Convert.ToDecimal(row.Cells["AgreedLeadTime"].Value) : 0;
            }
        }
        private void dgvSuppliers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void txtPhone_TextChanged(object sender, EventArgs e)
        {
        }

        private void txtSupName_TextChanged(object sender, EventArgs e)
        {
        }

        private void nudLeadTime_ValueChanged(object sender, EventArgs e)
        {
        }
        private void btnBack_Click(object sender, EventArgs e)
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
            hints.SetToolTip(btnAdd, "إضافة مورد جديد (Ctrl + S)");
            hints.SetToolTip(btnEdit, "تعديل بيانات المورد المحدد (Ctrl + E)");
            hints.SetToolTip(btnDelete, "حذف المورد المحدد نهائياً (Delete)");
            hints.SetToolTip(btnBack, "إغلاق الشاشة (Esc)");
        }

        // ====================================================
        // 🌟 محرك الاختصارات (Shortcuts) لشاشة الموردين
        // ====================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. إضافة مورد جديد (Ctrl + S) - استخدمنا S لتوحيد مفهوم "الحفظ"
            if (keyData == (Keys.Control | Keys.S))
            {
                btnAdd.PerformClick();
                return true;
            }

            // 2. حفظ التعديلات (Ctrl + E)
            else if (keyData == (Keys.Control | Keys.E))
            {
                btnEdit.PerformClick();
                return true;
            }

            // 3. حذف المورد (زر Delete في الكيبورد)
            else if (keyData == Keys.Delete)
            {
                // 🛡️ حماية: نتحقق أولاً أن المؤشر ليس داخل مربعات النص لتجنب حذف الحروف بالخطأ أثناء الكتابة
                if (!txtSupName.Focused && !txtPhone.Focused)
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

        private void frmSuppliers_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.Close();
        }
    }
}