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
    // كلاس الألوان — فاتح وداكن
    // ====================================================
    public class ThemeColors
    {
        public Color Background { get; set; } // خلفية الفورم الرئيسية
        public Color CardSurface { get; set; } // خلفية البطاقات والـ Panels
        public Color CardSurfaceAlt { get; set; } // خلفية بديلة للـ GroupBox
        public Color InputBg { get; set; } // خلفية حقول الإدخال
        public Color TableRowAlt { get; set; } // لون الصفوف المتبادلة في الجدول
        public Color TableSelection { get; set; } // لون التحديد في الجدول
        public Color BorderColor { get; set; } // لون الحدود والفواصل
        public Color GridLine { get; set; } // خطوط الشبكة في المخططات
        public Color PurpleMain { get; set; } // اللون البنفسجي الأساسي
        public Color PurpleDark { get; set; } // البنفسجي الداكن (hover)
        public Color CyanAccent { get; set; } // لون التمييز
        public Color TextDark { get; set; } // النص الرئيسي
        public Color TextMuted { get; set; } // النص الثانوي الخافت
        public Color TextOnPurple { get; set; } // النص فوق الخلفية البنفسجية

        // ── الثيم الفاتح ──────────────────────────────────
        public static ThemeColors Light => new ThemeColors
        {
            Background = Color.FromArgb(244, 245, 247),
            CardSurface = Color.White,
            CardSurfaceAlt = Color.FromArgb(250, 250, 252),
            InputBg = Color.FromArgb(248, 249, 250),
            TableRowAlt = Color.FromArgb(248, 246, 255),
            TableSelection = Color.FromArgb(230, 218, 255),
            BorderColor = Color.FromArgb(220, 220, 230),
            GridLine = Color.FromArgb(235, 235, 235),
            PurpleMain = Color.FromArgb(160, 82, 229),
            PurpleDark = Color.FromArgb(128, 43, 226),
            CyanAccent = Color.FromArgb(0, 210, 211),
            TextDark = Color.FromArgb(50, 50, 65),
            TextMuted = Color.FromArgb(140, 140, 155),
            TextOnPurple = Color.White,
        };

        // ── الثيم الداكن ──────────────────────────────────
        public static ThemeColors Dark => new ThemeColors
        {
            Background = Color.FromArgb(22, 22, 35),
            CardSurface = Color.FromArgb(35, 35, 52),
            CardSurfaceAlt = Color.FromArgb(42, 42, 62),
            InputBg = Color.FromArgb(48, 48, 70),
            TableRowAlt = Color.FromArgb(40, 38, 60),
            TableSelection = Color.FromArgb(90, 55, 160),
            BorderColor = Color.FromArgb(65, 65, 90),
            GridLine = Color.FromArgb(55, 55, 75),
            PurpleMain = Color.FromArgb(155, 89, 232),
            PurpleDark = Color.FromArgb(120, 55, 210),
            CyanAccent = Color.FromArgb(0, 210, 220),
            TextDark = Color.FromArgb(210, 210, 228),
            TextMuted = Color.FromArgb(130, 130, 155),
            TextOnPurple = Color.White,
        };
    }

    // ====================================================
    // ThemeManager الرئيسي
    // ====================================================
    public static class ThemeManager
    {
        private static ThemeColors _current = ThemeColors.Light;
        private static bool _isDark = false;

        private static readonly HashSet<MaterialForm> _managedForms = new HashSet<MaterialForm>();
        private static readonly HashSet<Button> _hookedButtons = new HashSet<Button>();

        // أسماء أزرار الأيقونات في frmAdmin
        private static readonly HashSet<string> _iconButtonNames = new HashSet<string>
        {
            "button1", "btnEmployees", "btnSuppliers",
            "btnShipment", "btnCashier", "btnReports"
        };

        public static bool IsDarkMode => _isDark;

        // ── تبديل الثيم ──────────────────────────────────
        public static void ToggleDarkMode(Form form)
        {
            _isDark = !_isDark;
            _current = _isDark ? ThemeColors.Dark : ThemeColors.Light;

            ApplyGlobalTheme(form);
            ApplyThemeToAllPanelChildren(form);

            if (form is MaterialForm)
                UpdateMaterialTheme();
        }

        // ── الدالة الرئيسية ───────────────────────────────
        public static void ApplyGlobalTheme(Form form)
        {
            form.BackColor = _current.Background;

            if (form is MaterialForm mf)
                ApplyMaterialTheme(mf);

            EnhanceControls(form);
        }

        // ── للشاشات المحقونة داخل pnlContainer ───────────
        // استدعِها في frmAdmin بعد السطر: pnlContainer.Controls.Add(activeForm)
        public static void ApplyThemeToChildForm(Form childForm)
        {
            childForm.BackColor = _current.Background;
            EnhanceControls(childForm);
        }

        // ── بحث عن شاشات محقونة داخل Panels وتطبيق الثيم ─
        private static void ApplyThemeToAllPanelChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Form child)
                    ApplyThemeToChildForm(child);

                if (c.HasChildren)
                    ApplyThemeToAllPanelChildren(c);
            }
        }

        // ── MaterialSkin ──────────────────────────────────
        private static void ApplyMaterialTheme(MaterialForm form)
        {
            var mgr = MaterialSkinManager.Instance;
            if (!_managedForms.Contains(form))
            {
                mgr.AddFormToManage(form);
                _managedForms.Add(form);
            }
            UpdateMaterialTheme();
        }

        private static void UpdateMaterialTheme()
        {
            var mgr = MaterialSkinManager.Instance;
            mgr.Theme = _isDark
                ? MaterialSkinManager.Themes.DARK
                : MaterialSkinManager.Themes.LIGHT;

            mgr.ColorScheme = new ColorScheme(
                Primary.DeepPurple500, Primary.DeepPurple700,
                Primary.DeepPurple300, Accent.Cyan400,
                TextShade.WHITE);
        }

        // ── معالج العناصر (عميق + يطبّق لون الحاوي أولاً) ─
        private static void EnhanceControls(Control parent)
        {
            // تطبيق لون الخلفية على الحاوي نفسه إذا كان Panel
            if (parent is Panel || parent is FlowLayoutPanel || parent is TableLayoutPanel)
                parent.BackColor = _current.CardSurface;

            foreach (Control c in parent.Controls)
            {
                if (c is DataGridView dgv) ModernizeDataGridView(dgv);
                else if (c is Chart chart) ModernizeChart(chart);
                else if (c is Button btn) ModernizeButton(btn);
                else if (c is RichTextBox rtb) ModernizeRichTextBox(rtb);
                else if (c is TextBox txt) ModernizeTextBox(txt);
                else if (c is ComboBox cmb) ModernizeComboBox(cmb);
                else if (c is NumericUpDown nud) ModernizeNumericUpDown(nud);
                else if (c is DateTimePicker dtp) ModernizeDateTimePicker(dtp);
                else if (c is GroupBox gb) ModernizeGroupBox(gb);
                else if (c is Panel
                      || c is FlowLayoutPanel
                      || c is TableLayoutPanel) c.BackColor = _current.CardSurface;
                else if (c is Label lbl) ModernizeLabel(lbl);
                else if (c is CheckBox chk) ModernizeCheckable(chk);
                else if (c is RadioButton rb) ModernizeCheckable(rb);
                // MaterialSkin controls تُدار من MaterialSkinManager مباشرة

                if (c.HasChildren) EnhanceControls(c);
            }
        }

        // ── أزرار ─────────────────────────────────────────
        private static void ModernizeButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;

            bool isIconBtn = btn.Image != null
                          || btn.BackgroundImage != null
                          || _iconButtonNames.Contains(btn.Name);

            if (isIconBtn)
            {
                Color bg = btn.Parent?.BackColor ?? _current.Background;
                btn.BackColor = bg;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseDownBackColor = bg;
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, 0.05f);

                if (btn.BackgroundImage != null)
                    btn.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                btn.BackColor = _current.PurpleMain;
                btn.ForeColor = _current.TextOnPurple;

                if (!_hookedButtons.Contains(btn))
                {
                    btn.MouseEnter += OnBtnEnter;
                    btn.MouseLeave += OnBtnLeave;
                    _hookedButtons.Add(btn);
                }
                else
                {
                    // تحديث اللون فقط دون إعادة ربط الأحداث
                    btn.BackColor = _current.PurpleMain;
                }
            }
        }

        private static void OnBtnEnter(object s, EventArgs e)
        { if (s is Button b) b.BackColor = _current.PurpleDark; }

        private static void OnBtnLeave(object s, EventArgs e)
        { if (s is Button b) b.BackColor = _current.PurpleMain; }

        // ── TextBox ───────────────────────────────────────
        private static void ModernizeTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = _current.InputBg;
            txt.ForeColor = _current.TextDark;
            // RTL يُترك من الـ Designer لكل حقل
        }

        private static void ModernizeRichTextBox(RichTextBox rtb)
        {
            rtb.BorderStyle = BorderStyle.None;
            rtb.BackColor = _current.InputBg;
            rtb.ForeColor = _current.TextDark;
        }

        // ── ComboBox ──────────────────────────────────────
        private static void ModernizeComboBox(ComboBox cmb)
        {
            cmb.BackColor = _current.InputBg;
            cmb.ForeColor = _current.TextDark;
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.Cursor = Cursors.Hand;
        }

        // ── NumericUpDown ─────────────────────────────────
        private static void ModernizeNumericUpDown(NumericUpDown nud)
        {
            nud.BackColor = _current.InputBg;
            nud.ForeColor = _current.TextDark;
            nud.BorderStyle = BorderStyle.FixedSingle;
        }

        // ── DateTimePicker ────────────────────────────────
        private static void ModernizeDateTimePicker(DateTimePicker dtp)
        {
            dtp.CalendarMonthBackground = _current.CardSurface;
            dtp.CalendarForeColor = _current.TextDark;
            dtp.CalendarTitleBackColor = _current.PurpleMain;
            dtp.CalendarTitleForeColor = _current.TextOnPurple;
            dtp.ForeColor = _current.TextDark;
        }

        // ── GroupBox ──────────────────────────────────────
        private static void ModernizeGroupBox(GroupBox gb)
        {
            gb.BackColor = _current.CardSurfaceAlt;
            gb.ForeColor = _current.PurpleMain;
        }

        // ── Label ─────────────────────────────────────────
        private static void ModernizeLabel(Label lbl)
        {
            // نرث لون الأب الفعلي لتجنب المربع الأبيض عند الحقن
            lbl.BackColor = lbl.Parent?.BackColor ?? _current.Background;
            lbl.ForeColor = _current.TextDark;
        }

        // ── CheckBox / RadioButton ────────────────────────
        private static void ModernizeCheckable(Control c)
        {
            c.BackColor = c.Parent?.BackColor ?? _current.Background;
            c.ForeColor = _current.TextDark;
            c.Cursor = Cursors.Hand;
        }

        // ── DataGridView ──────────────────────────────────
        private static void ModernizeDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = _current.CardSurface;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = _current.BorderColor;

            // خلايا عادية
            dgv.DefaultCellStyle.BackColor = _current.CardSurface;
            dgv.DefaultCellStyle.ForeColor = _current.TextDark;
            dgv.DefaultCellStyle.SelectionBackColor = _current.TableSelection;
            dgv.DefaultCellStyle.SelectionForeColor = _current.TextOnPurple;

            // صفوف متبادلة اللون (Zebra Striping)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = _current.TableRowAlt;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = _current.TextDark;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = _current.TableSelection;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = _current.TextOnPurple;

            // رأس الأعمدة
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = _current.PurpleMain;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = _current.TextOnPurple;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = _current.PurpleMain;
            dgv.ColumnHeadersHeight = 40;

            dgv.RowHeadersVisible = false;
            dgv.RowTemplate.Height = 35;
            dgv.AllowUserToResizeRows = false;
        }

        // ── Chart ─────────────────────────────────────────
        private static void ModernizeChart(Chart chart)
        {
            chart.BackColor = _current.CardSurface;
            chart.BorderlineColor = _current.BorderColor;

            foreach (var area in chart.ChartAreas)
            {
                area.BackColor = _current.CardSurface;
                area.BackSecondaryColor = _current.CardSurface;
                area.BorderColor = _current.BorderColor;

                area.AxisX.MajorGrid.LineColor = _current.GridLine;
                area.AxisY.MajorGrid.LineColor = _current.GridLine;
                area.AxisX.LabelStyle.ForeColor = _current.TextMuted;
                area.AxisY.LabelStyle.ForeColor = _current.TextMuted;
                area.AxisX.TitleForeColor = _current.TextDark;
                area.AxisY.TitleForeColor = _current.TextDark;
                area.AxisX.LineColor = _current.BorderColor;
                area.AxisY.LineColor = _current.BorderColor;
                area.AxisX.MajorTickMark.LineColor = _current.BorderColor;
                area.AxisY.MajorTickMark.LineColor = _current.BorderColor;
            }

            chart.Palette = ChartColorPalette.None;
            chart.PaletteCustomColors = new[]
            {
                _current.PurpleMain,
                _current.CyanAccent,
                Color.FromArgb(255, 118, 142),
                Color.FromArgb(255, 214,   0),
                Color.FromArgb(100, 200, 120),
            };

            foreach (var s in chart.Series)
            {
                s.BorderWidth = 2;
                s.LabelForeColor = _current.TextDark;
            }

            foreach (var legend in chart.Legends)
            {
                legend.BackColor = _current.CardSurface;
                legend.ForeColor = _current.TextDark;
            }
        }
    }
}