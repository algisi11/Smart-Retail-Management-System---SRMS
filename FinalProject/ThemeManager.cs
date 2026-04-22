using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Windows.Forms.DataVisualization.Charting;

namespace FinalProject
{
    public static class ThemeManager
    {
        // الألوان المريحة للعين
        private static readonly Color BgLightGray = Color.FromArgb(244, 245, 247);
        private static readonly Color CardWhite = Color.White;
        private static readonly Color InputBackground = Color.FromArgb(248, 249, 250);
        private static readonly Color PurpleMain = Color.FromArgb(160, 82, 229);
        private static readonly Color PurpleDark = Color.FromArgb(128, 43, 226);
        private static readonly Color CyanAccent = Color.FromArgb(0, 210, 211);
        private static readonly Color TextDark = Color.FromArgb(70, 70, 70);
        private static readonly Color TextMuted = Color.FromArgb(150, 150, 150);

        public static void ApplyGlobalTheme(Form form)
        {
            form.BackColor = BgLightGray;

            if (form is MaterialForm materialForm)
            {
                ApplyMaterialTheme(materialForm);
            }

            EnhanceControls(form);
        }

        private static void ApplyMaterialTheme(MaterialForm form)
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(form);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.DeepPurple500, Primary.DeepPurple700,
                Primary.DeepPurple300, Accent.Cyan400,
                TextShade.WHITE
            );
        }

        private static void EnhanceControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel)
                {
                    c.BackColor = CardWhite;
                }
                else if (c is GroupBox gb)
                {
                    gb.BackColor = CardWhite;
                    gb.ForeColor = PurpleMain;
                    // تم إزالة تغيير الخط هنا للحفاظ على المقاس
                }
                else if (c is Button btn) ModernizeButton(btn);
                else if (c is DataGridView dgv) ModernizeDataGridView(dgv);
                else if (c is TextBox || c is RichTextBox) ModernizeTextBox(c);
                else if (c is Chart chart) ModernizeChart(chart);
                else if (c is Label || c is CheckBox || c is RadioButton) ModernizeTextElements(c);

                if (c.HasChildren) EnhanceControls(c);
            }
        }

        private static void ModernizeTextElements(Control c)
        {
            c.BackColor = Color.Transparent;
            c.ForeColor = TextDark;
            // ❌ تم إزالة تغيير الخط (Font) هنا لكي لا تتمدد العناصر وتتداخل

            if (c is CheckBox || c is RadioButton) c.Cursor = Cursors.Hand;
        }

        private static void ModernizeTextBox(Control txt)
        {
            if (txt is TextBox t) t.BorderStyle = BorderStyle.FixedSingle;
            if (txt is RichTextBox rt) rt.BorderStyle = BorderStyle.None;

            txt.BackColor = InputBackground;
            txt.ForeColor = TextDark;
            // ❌ تم إزالة تغيير الخط (Font) هنا للحفاظ على ارتفاع مربع النص

            txt.RightToLeft = RightToLeft.No;
        }

        private static void ModernizeButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;

            if (btn.Image != null || btn.BackgroundImage != null)
            {
                btn.BackColor = Color.Transparent;
                btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            }
            else
            {
                btn.BackColor = PurpleMain;
                btn.ForeColor = Color.White;
                // ❌ تم إزالة تغيير الخط (Font) من الزر لكي لا يكبر النص ويخرج عن حدوده

                btn.MouseEnter += (s, e) => btn.BackColor = PurpleDark;
                btn.MouseLeave += (s, e) => btn.BackColor = PurpleMain;
            }
        }

        private static void ModernizeDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = CardWhite;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(243, 238, 255);
            dgv.DefaultCellStyle.SelectionForeColor = PurpleDark;
            dgv.DefaultCellStyle.BackColor = CardWhite;
            dgv.DefaultCellStyle.ForeColor = TextDark;
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;

            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = PurpleMain;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            // أبقينا الخط هنا فقط لأن الجداول ترتب نفسها تلقائياً ولا تسبب تداخلاً
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 40;
            dgv.RowTemplate.Height = 35;
            dgv.AllowUserToResizeRows = false;
        }

        private static void ModernizeChart(Chart chart)
        {
            chart.BackColor = CardWhite;

            foreach (var chartArea in chart.ChartAreas)
            {
                chartArea.BackColor = CardWhite;
                chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(240, 240, 240);
                chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(240, 240, 240);
                chartArea.AxisX.LabelStyle.ForeColor = TextMuted;
                chartArea.AxisY.LabelStyle.ForeColor = TextMuted;
                chartArea.AxisX.LineColor = Color.FromArgb(220, 220, 220);
                chartArea.AxisY.LineColor = Color.FromArgb(220, 220, 220);
            }

            Color[] customPalette = new Color[]
            {
                PurpleMain, CyanAccent, Color.FromArgb(255, 118, 142), Color.FromArgb(255, 214, 0)
            };
            chart.Palette = ChartColorPalette.None;
            chart.PaletteCustomColors = customPalette;

            foreach (var series in chart.Series) series.BorderWidth = 0;
            foreach (var legend in chart.Legends)
            {
                legend.BackColor = CardWhite;
                legend.ForeColor = TextDark;
            }
        }
    }
}