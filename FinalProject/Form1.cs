using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;


namespace FinalProject
{
    public partial class Form1 : MaterialForm
    {
        public Form1()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void cbShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (cbShowPassword.Checked)
            {
                txtPassword.PasswordChar = '\0';

            }
            else
            {
                txtPassword.PasswordChar = '*';
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم المستخدم وكلمة المرور", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
            string query = "SELECT Role FROM Employees WHERE FullName = @username AND PasswordHash = @password";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", txtUserName.Text);
                    cmd.Parameters.AddWithValue("@password", txtPassword.Text);

                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            string userRole = result.ToString();

                            SessionManager.EmployeeName = txtUserName.Text;
                            SessionManager.Role = userRole;

                            this.Hide();

                            if (userRole == "Warehouse")
                            {
                                frmInventory inventoryForm = new frmInventory();
                                inventoryForm.Show();
                            }
                            else if (userRole == "Admin")
                            {
                                frmAdmin adminForm = new frmAdmin();
                                adminForm.Show();
                            }
                            else if (userRole == "Cashier")
                            {
                                frmCashier cashierForm = new frmCashier();
                                cashierForm.Show();
                            }
                            else
                            {
                                MessageBox.Show("صلاحية المستخدم غير معروفة في النظام.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Show();
                            }
                        }
                        else
                        {
                            MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("حدث خطأ في الاتصال بقاعدة البيانات: " + ex.Message, "خطأ تقني", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم المستخدم وكلمة المرور", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
            string query = "SELECT Role FROM Employees WHERE FullName = @username AND PasswordHash = @password";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", txtUserName.Text);
                    cmd.Parameters.AddWithValue("@password", txtPassword.Text);

                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            string userRole = result.ToString();

                            SessionManager.EmployeeName = txtUserName.Text;
                            SessionManager.Role = userRole;

                            this.Hide();

                            if (userRole == "Warehouse")
                            {
                                frmInventory inventoryForm = new frmInventory();
                                inventoryForm.Show();
                                SystemLogger.LogAction("تسجيل دخول", "تسجيل دخول بنجاح");
                            }
                            else if (userRole == "Admin")
                            {
                                frmAdmin adminForm = new frmAdmin();
                                adminForm.Show();
                                SystemLogger.LogAction("تسجيل دخول", "تسجيل دخول بنجاح");

                            }
                            else if (userRole == "Cashier")
                            {
                                frmCashier cashierForm = new frmCashier();
                                cashierForm.Show();
                                SystemLogger.LogAction("تسجيل دخول", "تسجيل دخول بنجاح");

                            }
                            else
                            {
                                MessageBox.Show("صلاحية المستخدم غير معروفة في النظام.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Show();
                                SystemLogger.LogAction("تسجيل دخول", "محاولة خاطئة لتسجيل الدخول");

                            }
                        }
                        else
                        {
                            MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            SystemLogger.LogAction("تسجيل دخول", "محاولة خاطئة لتسجيل الدخول");

                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("حدث خطأ في الاتصال بقاعدة البيانات: " + ex.Message, "خطأ تقني", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }
            }
        }
    }

}
