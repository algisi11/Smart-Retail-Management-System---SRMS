using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class frmBackup : MaterialForm
    {
        string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";
        public frmBackup()
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);
        }

        private void frmBackup_Load(object sender, EventArgs e)
        {

        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // فتح نافذة لاختيار المجلد الذي سيحفظ فيه النسخة
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "اختر المجلد لحفظ النسخة الاحتياطية";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBackup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPath.Text))
            {
                MessageBox.Show("الرجاء اختيار مسار الحفظ أولاً!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // إنشاء اسم ملف فريد يحتوي على تاريخ وساعة النسخ
            string fileName = "SmartInventoryDB_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".bak";
            string fullPath = Path.Combine(txtPath.Text, fileName);

            // استعلام الـ SQL للنسخ الاحتياطي
            string backupQuery = $"BACKUP DATABASE [SmartInventoryDB] TO DISK = '{fullPath}' WITH FORMAT, MEDIANAME = 'Z_SQLServerBackups', NAME = 'Full Backup of SmartInventoryDB'";

            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnBackup.Enabled = false;
                btnBackup.Text = "⏳ جاري النسخ...";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(backupQuery, conn))
                    {
                        conn.Open();
                        // نستخدم CommandTimeout طويل لأن النسخ قد يأخذ وقتاً إذا كانت القاعدة كبيرة
                        cmd.CommandTimeout = 300;
                        cmd.ExecuteNonQuery();
                    }
                }

                SystemLogger.LogAction("نسخ احتياطي", $"تم أخذ نسخة احتياطية للقاعدة بنجاح في المسار: {fullPath}");
                MessageBox.Show($"تم أخذ النسخة الاحتياطية بنجاح!\nتجدها في:\n{fullPath}", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء النسخ الاحتياطي (تأكد من صلاحيات المجلد المختار):\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnBackup.Enabled = true;
                btnBackup.Text = "بدء النسخ الاحتياطي";
            }
        }
    }
    }

