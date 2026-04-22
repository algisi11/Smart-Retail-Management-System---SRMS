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
    public partial class frmAuditLog : Form
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        public frmAuditLog()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
        }

        private void frmAuditLog_Load(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // جلب آخر 500 حركة أمنية مرتبة من الأحدث للأقدم
                string query = @"
                    SELECT TOP 500 
                        LogDate AS 'وقت وتاريخ الحركة', 
                        Username AS 'المستخدم', 
                        ActionType AS 'نوع الحركة', 
                        Details AS 'التفاصيل'
                    FROM SystemLogs 
                    ORDER BY LogDate DESC";

                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgvLogs.DataSource = dt;

                    // تنسيق الأعمدة لتبدو مرتبة
                    dgvLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    if (dgvLogs.Columns.Count > 0)
                    {
                        dgvLogs.Columns["وقت وتاريخ الحركة"].Width = 150;
                        dgvLogs.Columns["المستخدم"].Width = 100;
                        dgvLogs.Columns["نوع الحركة"].Width = 150;
                        // التفاصيل تأخذ المساحة المتبقية تلقائياً
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في تحميل سجل المراقبة: " + ex.Message);
                }
            }
        }
    }
}
