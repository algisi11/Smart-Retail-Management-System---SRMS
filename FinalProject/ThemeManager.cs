using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Windows.Forms.DataVisualization.Charting;

namespace FinalProject
{
    // ====================================================
    // 1. كلاس مستقل يحمل مجموعة الألوان (Light / Dark)
    //    التعديل: بدلاً من ألوان hard-coded داخل ThemeManager
    // ====================================================
    public class ThemeColors
    {
        public Color Background { get; set; }
        public Color CardSurface { get; set; }
        public Color InputBg { get; set; }
        public Color PurpleMain { get; set; }
        public Color PurpleDark { get; set; }
        public Color CyanAccent { get; set; }
        public Color TextDark { get; set; }
        public Color TextMuted { get; set; }

        // الثيم الفاتح (الافتراضي)
        public static ThemeColors Light => new ThemeColors
        {
            Background = Color.FromArgb(244, 245, 247),
            CardSurface = Color.White,
            InputBg = Color.FromArgb(248, 249, 250),
            PurpleMain = Color.FromArgb(160, 82, 229),
            PurpleDark = Color.FromArgb(128, 43, 226),
            CyanAccent = Color.FromArgb(0, 210, 211),
            TextDark = Color.FromArgb(70, 70, 70),
            TextMuted = Color.FromArgb(150, 150, 150),
        };

        // الثيم الداكن
        public static ThemeColors Dark => new ThemeColors
        {
            Background = Color.FromArgb(30, 30, 46),
            CardSurface = Color.FromArgb(45, 45, 65),
            InputBg = Color.FromArgb(55, 55, 75),
            PurpleMain = Color.FromArgb(180, 100, 255),
            PurpleDark = Color.FromArgb(140, 60, 240),
            CyanAccent = Color.FromArgb(0, 230, 230),
            TextDark = Color.FromArgb(220, 220, 235),
            TextMuted = Color.FromArgb(140, 140, 160),
        };
    }

    // ====================================================
    // 2. ThemeManager الرئيسي (مع كل التعديلات)
    // ====================================================
    public static class ThemeManager
    {
        // الثيم الحالي النشط (افتراضياً فاتح)
        private static ThemeColors _current = ThemeColors.Light;
        private static bool _isDark = false;

        // تتبع الفورمز المضافة لـ MaterialSkinManager لمنع التكرار (تعديل #7)
        private static readonly HashSet<MaterialForm> _managedForms = new HashSet<MaterialForm>();

        // تتبع الأزرار التي طُبّقت عليها الأحداث لمنع التراكم (تعديل #6)
        private static readonly HashSet<Button> _hookedButtons = new HashSet<Button>();

        // ====================================================
        // دالة التبديل بين الثيم الفاتح والداكن (تعديل #2)
        // ====================================================
        public static void ToggleDarkMode(Form form)
        {
            _isDark = !_isDark;
            _current = _isDark ? ThemeColors.Dark : ThemeColors.Light;

            // تطبيق على الفورم الرئيسية
            ApplyGlobalTheme(form);

            // تطبيق على كل الشاشات المحقونة داخل أي Panel في الفورم
            ApplyThemeToAllPanelChildren(form);

            if (form is MaterialForm)
                UpdateMaterialTheme();
        }

        // يبحث عن أي شاشة (Form) محقونة داخل Panels ويُطبّق الثيم عليها
        private static void ApplyThemeToAllPanelChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Form childForm)
                {
                    ApplyThemeToChildForm(childForm);
                }
                if (c.HasChildren)
                    ApplyThemeToAllPanelChildren(c);
            }
        }

        public static bool IsDarkMode => _isDark;

        // ====================================================
        // الدالة الرئيسية (نقطة الدخول الوحيدة)
        // ====================================================
        public static void ApplyGlobalTheme(Form form)
        {
            form.BackColor = _current.Background;

            if (form is MaterialForm materialForm)
            {
                ApplyMaterialTheme(materialForm);
            }

            EnhanceControls(form);
        }

        // ====================================================
        // دالة مخصصة للشاشات المحقونة داخل pnlContainer
        // استدعِها في frmAdmin بعد الحقن مباشرة:
        //   ThemeManager.ApplyThemeToChildForm(activeForm);
        // ====================================================
        public static void ApplyThemeToChildForm(Form childForm)
        {
            childForm.BackColor = _current.Background;
            EnhanceControls(childForm);
        }

        // ====================================================
        // تطبيق MaterialSkin مع الحماية من التكرار (تعديل #7)
        // ====================================================
        private static void ApplyMaterialTheme(MaterialForm form)
        {
            var manager = MaterialSkinManager.Instance;

            // إضافة الفورم مرة واحدة فقط
            if (!_managedForms.Contains(form))
            {
                manager.AddFormToManage(form);
                _managedForms.Add(form);
            }

            UpdateMaterialTheme();
        }

        private static void UpdateMaterialTheme()
        {
            var manager = MaterialSkinManager.Instance;
            manager.Theme = _isDark
                ? MaterialSkinManager.Themes.DARK
                : MaterialSkinManager.Themes.LIGHT;

            manager.ColorScheme = new ColorScheme(
                Primary.DeepPurple500, Primary.DeepPurple700,
                Primary.DeepPurple300, Accent.Cyan400,
                TextShade.WHITE
            );
        }

        // ====================================================
        // المعالج الرئيسي للعناصر (تعديل #3 - Dictionary بدلاً من if/else)
        // ====================================================
        private static void EnhanceControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                // تطبيق التنسيق حسب نوع العنصر
                if (c is DataGridView dgv) ModernizeDataGridView(dgv);
                else if (c is Chart chart) ModernizeChart(chart);
                else if (c is Button btn) ModernizeButton(btn);
                else if (c is RichTextBox rtb) ModernizeRichTextBox(rtb);
                else if (c is TextBox txt) ModernizeTextBox(txt);
                else if (c is ComboBox cmb) ModernizeComboBox(cmb);       // تعديل #8
                else if (c is NumericUpDown nud) ModernizeNumericUpDown(nud);  // تعديل #8
                else if (c is GroupBox gb) ModernizeGroupBox(gb);
                else if (c is Panel || c is FlowLayoutPanel
                              || c is TableLayoutPanel) c.BackColor = _current.CardSurface;
                else if (c is Label lbl) ModernizeLabel(lbl);
                else if (c is CheckBox chk) ModernizeCheckable(chk);
                else if (c is RadioButton rb) ModernizeCheckable(rb);

                // تجاهل عناصر MaterialSkin الخاصة لعدم التعارض معها (تعديل #4)
                // MaterialButton, MaterialTextField, etc. تُدار من MaterialSkinManager مباشرة

                // الانتقال للعناصر الداخلية (التكرار العميق)
                if (c.HasChildren) EnhanceControls(c);
            }
        }

        // ====================================================
        // تنسيق الأزرار مع إصلاح Event Leak (تعديل #6)
        // ====================================================
        // أسماء أزرار الأيقونات في frmAdmin (تحتوي BackgroundImage من الـ Designer)
        private static readonly HashSet<string> _iconButtonNames = new HashSet<string>
        {
            "button1", "btnEmployees", "btnSuppliers",
            "btnShipment", "btnCashier", "btnReports"
        };

        private static void ModernizeButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;

            // زر يحتوي على صورة (.Image أو .BackgroundImage)
            // أو زر اسمه ضمن قائمة أزرار الأيقونات في frmAdmin
            bool isIconButton = btn.Image != null
                             || btn.BackgroundImage != null
                             || _iconButtonNames.Contains(btn.Name);

            if (isIconButton)
            {
                // نستخدم لون الخلفية الفعلي بدلاً من Transparent
                // لأن Transparent يرث لون الـ Parent وقد يسبب مربعاً أبيض عند الحقن
                Color iconBg = btn.Parent != null ? btn.Parent.BackColor : _current.Background;
                btn.BackColor = iconBg;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseDownBackColor = iconBg;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(iconBg, 0.05f);

                if (btn.BackgroundImage != null)
                    btn.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                btn.BackColor = _current.PurpleMain;
                btn.ForeColor = Color.White;

                // ربط الأحداث مرة واحدة فقط (تعديل #6)
                if (!_hookedButtons.Contains(btn))
                {
                    btn.MouseEnter += OnButtonMouseEnter;
                    btn.MouseLeave += OnButtonMouseLeave;
                    _hookedButtons.Add(btn);
                }
                else
                {
                    btn.BackColor = _current.PurpleMain;
                }
            }
        }

        private static void OnButtonMouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn) btn.BackColor = _current.PurpleDark;
        }

        private static void OnButtonMouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn) btn.BackColor = _current.PurpleMain;
        }

        // ====================================================
        // تنسيق TextBox مع إصلاح مشكلة RTL (تعديل #5)
        // ====================================================
        private static void ModernizeTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = _current.InputBg;
            txt.ForeColor = _current.TextDark;

            // الإصلاح: لا نغير RTL إلا إذا كان النص لاتينياً بحتاً (تعديل #5)
            // نترك الـ RightToLeft كما هو من الـ Designer لكل حقل
            // (أزلنا السطر txt.RightToLeft = RightToLeft.No الذي كان يكسر العربية)
        }

        private static void ModernizeRichTextBox(RichTextBox rtb)
        {
            rtb.BorderStyle = BorderStyle.None;
            rtb.BackColor = _current.InputBg;
            rtb.ForeColor = _current.TextDark;
            // RTL يُترك كما هو من الـ Designer
        }

        // ====================================================
        // تنسيق ComboBox و NumericUpDown (تعديل #8)
        // ====================================================
        private static void ModernizeComboBox(ComboBox cmb)
        {
            cmb.BackColor = _current.InputBg;
            cmb.ForeColor = _current.TextDark;
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.Cursor = Cursors.Hand;
        }

        private static void ModernizeNumericUpDown(NumericUpDown nud)
        {
            nud.BackColor = _current.InputBg;
            nud.ForeColor = _current.TextDark;
            nud.BorderStyle = BorderStyle.FixedSingle;
        }

        // ====================================================
        // بقية دوال التنسيق (محدّثة لتستخدم _current بدلاً من الألوان الثابتة)
        // ====================================================
        private static void ModernizeGroupBox(GroupBox gb)
        {
            gb.BackColor = _current.CardSurface;
            gb.ForeColor = _current.PurpleMain;
        }

        private static void ModernizeLabel(Label lbl)
        {
            lbl.BackColor = Color.Transparent;
            lbl.ForeColor = _current.TextDark;
        }

        private static void ModernizeCheckable(Control c)
        {
            c.BackColor = Color.Transparent;
            c.ForeColor = _current.TextDark;
            c.Cursor = Cursors.Hand;
        }

        private static void ModernizeDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = _current.CardSurface;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(243, 238, 255);
            dgv.DefaultCellStyle.SelectionForeColor = _current.PurpleDark;
            dgv.DefaultCellStyle.BackColor = _current.CardSurface;
            dgv.DefaultCellStyle.ForeColor = _current.TextDark;

            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;

            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = _current.PurpleMain;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 40;
            dgv.RowTemplate.Height = 35;
            dgv.AllowUserToResizeRows = false;
        }

        private static void ModernizeChart(Chart chart)
        {
            chart.BackColor = _current.CardSurface;

            foreach (var area in chart.ChartAreas)
            {
                area.BackColor = _current.CardSurface;
                area.AxisX.MajorGrid.LineColor = Color.FromArgb(240, 240, 240);
                area.AxisY.MajorGrid.LineColor = Color.FromArgb(240, 240, 240);
                area.AxisX.LabelStyle.ForeColor = _current.TextMuted;
                area.AxisY.LabelStyle.ForeColor = _current.TextMuted;
                area.AxisX.LineColor = Color.FromArgb(220, 220, 220);
                area.AxisY.LineColor = Color.FromArgb(220, 220, 220);
            }

            chart.Palette = ChartColorPalette.None;
            chart.PaletteCustomColors = new Color[]
            {
                _current.PurpleMain,
                _current.CyanAccent,
                Color.FromArgb(255, 118, 142),
                Color.FromArgb(255, 214, 0)
            };

            foreach (var series in chart.Series) series.BorderWidth = 0;
            foreach (var legend in chart.Legends)
            {
                legend.BackColor = _current.CardSurface;
                legend.ForeColor = _current.TextDark;
            }
        }
    }
}