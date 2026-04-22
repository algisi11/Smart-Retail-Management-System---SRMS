using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;




namespace FinalProject
{
    public partial class AddEmp : MaterialForm
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        int selectedEmployeeId = 0;

        public AddEmp()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
            ToolTip btnToolTip = new ToolTip();

            // 2. إعدادات الوقت (أهم جزء لتطبيق فكرة "الوقوف المطول")
            btnToolTip.InitialDelay = 800;  // الانتظار 800 جزء من الثانية (أقل من ثانية بقليل) قبل ظهور التلميح
            btnToolTip.AutoPopDelay = 5000; // مدة بقاء التلميح ظاهراً (5 ثوانٍ)
            btnToolTip.ReshowDelay = 200;   // سرعة ظهوره عند الانتقال لزر آخر مباشرة
            btnToolTip.ShowAlways = true;   // إظهاره حتى لو لم تكن الشاشة هي النشطة تماماً

            // 3. ربط التلميحات بالأزرار
            // (قم بتغيير أسماء الأزرار لأسماء أزرارك الحقيقية)
            btnToolTip.SetToolTip(btnSave, "حفظ  (Ctrl + S)");
            btnToolTip.SetToolTip(btnDelete, " حذف (Ctrl + D)");
            btnToolTip.SetToolTip(btnEdit, "تعديل (Ctrl + E)");
            btnToolTip.SetToolTip(btnBack, "خروج (ESC)");


        }

        private void AddEmp_Load(object sender, EventArgs e)
        {
            LoadEmployees();
            txtEmpPassword.PasswordChar = '*';
        }

        private void LoadEmployees()
        {
            string query = "SELECT EmployeeID, FullName, Role, PasswordHash FROM Employees";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvEmployees.DataSource = dt;

                if (dgvEmployees.Columns.Contains("EmployeeID"))
                    dgvEmployees.Columns["EmployeeID"].Visible = false;

                dgvEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                dgvEmployees.ReadOnly = true;
                dgvEmployees.AllowUserToAddRows = false;
                dgvEmployees.AllowUserToDeleteRows = false;
            }
        }

        private void ClearFields()
        {
            txtEmpName.Clear();
            txtEmpPassword.Clear();
            cmbRole.SelectedIndex = -1;
            selectedEmployeeId = 0;
            txtEmpName.Focus();
        }







        private void cbShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (cbShowPassword.Checked)
            {
                txtEmpPassword.PasswordChar = '\0';
            }
            else
            {
                txtEmpPassword.PasswordChar = '*';
            }
        }

        

        private void dgvEmployees_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvEmployees.Rows[e.RowIndex];
                selectedEmployeeId = Convert.ToInt32(row.Cells["EmployeeID"].Value);
                txtEmpName.Text = row.Cells["FullName"].Value.ToString();
                cmbRole.Text = row.Cells["Role"].Value.ToString();
                txtEmpPassword.Text = row.Cells["PasswordHash"].Value.ToString();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmpName.Text) || string.IsNullOrWhiteSpace(txtEmpPassword.Text) || cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Please fill all fields!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string checkQuery = "SELECT COUNT(*) FROM Employees WHERE FullName = @Name";
                SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@Name", txtEmpName.Text.Trim());

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    MessageBox.Show("This employee name already exists in the system!", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string query = "INSERT INTO Employees (FullName, Role, PasswordHash) VALUES (@Name, @Role, @Password)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", txtEmpName.Text.Trim());
                cmd.Parameters.AddWithValue("@Role", cmbRole.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Password", txtEmpPassword.Text);

                cmd.ExecuteNonQuery();
                SystemLogger.LogAction("إضافة موظف جديد", "تمت إضافة موظف جديد من قبل الأدمن");

            }

            LoadEmployees();
            ClearFields();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (selectedEmployeeId == 0)
            {
                MessageBox.Show("Please select an employee from the list first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE Employees SET FullName = @Name, Role = @Role, PasswordHash = @Password WHERE EmployeeID = @ID";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", txtEmpName.Text);
                cmd.Parameters.AddWithValue("@Role", cmbRole.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Password", txtEmpPassword.Text);
                cmd.Parameters.AddWithValue("@ID", selectedEmployeeId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            SystemLogger.LogAction("تعديل بيانات موظف موجود", "تعديل بيانات موظف موجود من قبل الأدمن");

            LoadEmployees();
            ClearFields();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedEmployeeId == 0)
            {
                MessageBox.Show("Please select an employee from the list first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this employee?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string query = "DELETE FROM Employees WHERE EmployeeID = @ID";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ID", selectedEmployeeId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SystemLogger.LogAction("حذف موظف موجود", "تم حذف موظف موجود من قبل الأدمن");

                LoadEmployees();
                ClearFields();
            }
        }

        private void cbShowPassword_CheckedChanged_1(object sender, EventArgs e)
        {
            if (cbShowPassword.Checked)
            {
                txtEmpPassword.PasswordChar = '\0';
            }
            else
            {
                txtEmpPassword.PasswordChar = '*';
            }
        }

        private void btnBack_Click_1(object sender, EventArgs e)
        {
            this.Close();

        }

        private void AddEmp_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.Close();

        }

        // 🌟 المحرك الصامت لالتقاط اختصارات لوحة المفاتيح
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. اختصار الحفظ (Ctrl + S)
            if (keyData == (Keys.Control | Keys.S))
            {
                // استدعاء الضغطة برمجياً كأن المستخدم ضغط بالماوس
                // قم بتغيير اسم الزر ليطابق الزر الموجود لديك
                btnSave.PerformClick();

                return true; // إخبار الويندوز أننا تعاملنا مع الضغطة، لا تفعل شيئاً آخر
            }
           
            else if (keyData == (Keys.Control | Keys.D))
            {
                btnDelete.PerformClick();
                return true;
            }

            else if (keyData == (Keys.Control | Keys.E))
            {
                btnEdit.PerformClick();
                return true;
            }
            // 5. زر الهروب (ESC) لإغلاق الشاشة
            else if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }

            // إذا كانت الضغطة ليست من اختصاراتنا، اترك الويندوز يتعامل معها بشكل طبيعي
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}