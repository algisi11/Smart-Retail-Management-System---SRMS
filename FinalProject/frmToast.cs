using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class frmToast : Form
    {
        public frmToast(string message)
        {
            InitializeComponent();
            ThemeManager.ApplyGlobalTheme(this);

            lblMessage.Text = message;
        }

        private void frmToast_Load(object sender, EventArgs e)
        {
            int x = Screen.PrimaryScreen.WorkingArea.Width - this.Width - 20;
            int y = Screen.PrimaryScreen.WorkingArea.Height - this.Height - 20;
            this.Location = new Point(x, y);

            // تشغيل العداد ليبدأ في حساب الـ 3 ثوانٍ
            toastTimer.Start();
        }

        private void toastTimer_Tick(object sender, EventArgs e)
        {
            toastTimer.Stop(); // إيقاف العداد

            // إضافة حركة تلاشي (Fade Out) أنيقة
            Timer fadeOutTimer = new Timer();
            fadeOutTimer.Interval = 50;
            fadeOutTimer.Tick += (s, args) =>
            {
                if (this.Opacity > 0)
                {
                    this.Opacity -= 0.1; // تقليل الشفافية تدريجياً
                }
                else
                {
                    fadeOutTimer.Stop();
                    this.Close(); // إغلاق الشاشة تماماً
                }
            };
            fadeOutTimer.Start();
        }
    }
    
}
