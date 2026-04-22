namespace FinalProject
{
    partial class frmAdmin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAdmin));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnCashier = new System.Windows.Forms.Button();
            this.btnShipment = new System.Windows.Forms.Button();
            this.btnReports = new System.Windows.Forms.Button();
            this.btnEmployees = new System.Windows.Forms.Button();
            this.btnSuppliers = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlContainer = new System.Windows.Forms.Panel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlAICommand = new System.Windows.Forms.Panel();
            this.lblRecommendedQty = new System.Windows.Forms.Label();
            this.lblAIAlert = new System.Windows.Forms.RichTextBox();
            this.btnAutoRestock = new System.Windows.Forms.Button();
            this.lblAITitle = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.pnlContainer.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.pnlAICommand.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 6;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.875F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13.75F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.875F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.btnCashier, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnShipment, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnReports, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnEmployees, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnSuppliers, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.button1, 2, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 64);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(844, 100);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // btnCashier
            // 
            this.btnCashier.BackColor = System.Drawing.Color.Transparent;
            this.btnCashier.BackgroundImage = global::FinalProject.Properties.Resources.images;
            this.btnCashier.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnCashier.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCashier.FlatAppearance.BorderSize = 0;
            this.btnCashier.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCashier.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCashier.Location = new System.Drawing.Point(3, 3);
            this.btnCashier.Name = "btnCashier";
            this.btnCashier.Size = new System.Drawing.Size(119, 94);
            this.btnCashier.TabIndex = 0;
            this.btnCashier.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnCashier.UseVisualStyleBackColor = false;
            this.btnCashier.Click += new System.EventHandler(this.btnCashier_Click);
            // 
            // btnShipment
            // 
            this.btnShipment.BackColor = System.Drawing.Color.Transparent;
            this.btnShipment.BackgroundImage = global::FinalProject.Properties.Resources.download__4_1;
            this.btnShipment.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnShipment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnShipment.FlatAppearance.BorderSize = 0;
            this.btnShipment.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnShipment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnShipment.Location = new System.Drawing.Point(676, 3);
            this.btnShipment.Name = "btnShipment";
            this.btnShipment.Size = new System.Drawing.Size(165, 94);
            this.btnShipment.TabIndex = 5;
            this.btnShipment.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnShipment.UseVisualStyleBackColor = false;
            this.btnShipment.Click += new System.EventHandler(this.btnShipment_Click);
            // 
            // btnReports
            // 
            this.btnReports.BackColor = System.Drawing.Color.Transparent;
            this.btnReports.BackgroundImage = global::FinalProject.Properties.Resources.download__2_1;
            this.btnReports.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnReports.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReports.FlatAppearance.BorderSize = 0;
            this.btnReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReports.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReports.Location = new System.Drawing.Point(389, 3);
            this.btnReports.Name = "btnReports";
            this.btnReports.Size = new System.Drawing.Size(131, 94);
            this.btnReports.TabIndex = 3;
            this.btnReports.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnReports.UseVisualStyleBackColor = false;
            this.btnReports.Click += new System.EventHandler(this.btnReports_Click);
            // 
            // btnEmployees
            // 
            this.btnEmployees.BackColor = System.Drawing.Color.Transparent;
            this.btnEmployees.BackgroundImage = global::FinalProject.Properties.Resources.download__3_1;
            this.btnEmployees.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnEmployees.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEmployees.FlatAppearance.BorderSize = 0;
            this.btnEmployees.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEmployees.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEmployees.Location = new System.Drawing.Point(526, 3);
            this.btnEmployees.Name = "btnEmployees";
            this.btnEmployees.Size = new System.Drawing.Size(144, 94);
            this.btnEmployees.TabIndex = 4;
            this.btnEmployees.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnEmployees.UseVisualStyleBackColor = false;
            this.btnEmployees.Click += new System.EventHandler(this.btnEmployees_Click);
            // 
            // btnSuppliers
            // 
            this.btnSuppliers.BackColor = System.Drawing.Color.Transparent;
            this.btnSuppliers.BackgroundImage = global::FinalProject.Properties.Resources.download;
            this.btnSuppliers.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnSuppliers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSuppliers.FlatAppearance.BorderSize = 0;
            this.btnSuppliers.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSuppliers.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSuppliers.Location = new System.Drawing.Point(128, 3);
            this.btnSuppliers.Name = "btnSuppliers";
            this.btnSuppliers.Size = new System.Drawing.Size(139, 94);
            this.btnSuppliers.TabIndex = 1;
            this.btnSuppliers.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnSuppliers.UseVisualStyleBackColor = false;
            this.btnSuppliers.Click += new System.EventHandler(this.btnSuppliers_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.Transparent;
            this.button1.BackgroundImage = global::FinalProject.Properties.Resources.download__1_1;
            this.button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(273, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(110, 94);
            this.button1.TabIndex = 2;
            this.button1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.pnlContainer, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 164);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(844, 430);
            this.tableLayoutPanel2.TabIndex = 8;
            // 
            // pnlContainer
            // 
            this.pnlContainer.Controls.Add(this.tableLayoutPanel3);
            this.pnlContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContainer.Location = new System.Drawing.Point(3, 3);
            this.pnlContainer.Name = "pnlContainer";
            this.pnlContainer.Size = new System.Drawing.Size(838, 424);
            this.pnlContainer.TabIndex = 9;
            this.pnlContainer.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.pnlAICommand, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Left;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(295, 424);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // pnlAICommand
            // 
            this.pnlAICommand.BackColor = System.Drawing.Color.Transparent;
            this.pnlAICommand.Controls.Add(this.lblRecommendedQty);
            this.pnlAICommand.Controls.Add(this.lblAIAlert);
            this.pnlAICommand.Controls.Add(this.btnAutoRestock);
            this.pnlAICommand.Controls.Add(this.lblAITitle);
            this.pnlAICommand.Location = new System.Drawing.Point(3, 3);
            this.pnlAICommand.Name = "pnlAICommand";
            this.pnlAICommand.Size = new System.Drawing.Size(289, 418);
            this.pnlAICommand.TabIndex = 0;
            // 
            // lblRecommendedQty
            // 
            this.lblRecommendedQty.AutoSize = true;
            this.lblRecommendedQty.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRecommendedQty.Location = new System.Drawing.Point(39, 242);
            this.lblRecommendedQty.Name = "lblRecommendedQty";
            this.lblRecommendedQty.Size = new System.Drawing.Size(26, 13);
            this.lblRecommendedQty.TabIndex = 4;
            this.lblRecommendedQty.Text = "Qty";
            // 
            // lblAIAlert
            // 
            this.lblAIAlert.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.lblAIAlert.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAIAlert.ForeColor = System.Drawing.Color.Red;
            this.lblAIAlert.Location = new System.Drawing.Point(0, 104);
            this.lblAIAlert.Name = "lblAIAlert";
            this.lblAIAlert.ReadOnly = true;
            this.lblAIAlert.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.lblAIAlert.Size = new System.Drawing.Size(286, 135);
            this.lblAIAlert.TabIndex = 3;
            this.lblAIAlert.Text = "";
            // 
            // btnAutoRestock
            // 
            this.btnAutoRestock.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnAutoRestock.Location = new System.Drawing.Point(190, 395);
            this.btnAutoRestock.Name = "btnAutoRestock";
            this.btnAutoRestock.Size = new System.Drawing.Size(75, 23);
            this.btnAutoRestock.TabIndex = 2;
            this.btnAutoRestock.Text = "Re Stock";
            this.btnAutoRestock.UseVisualStyleBackColor = true;
            this.btnAutoRestock.Click += new System.EventHandler(this.btnAutoRestock_Click);
            // 
            // lblAITitle
            // 
            this.lblAITitle.AutoSize = true;
            this.lblAITitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAITitle.Location = new System.Drawing.Point(15, 9);
            this.lblAITitle.Name = "lblAITitle";
            this.lblAITitle.Size = new System.Drawing.Size(96, 13);
            this.lblAITitle.TabIndex = 0;
            this.lblAITitle.Text = "Smart Inventory";
            // 
            // frmAdmin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(850, 597);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmAdmin";
            this.Text = "Admin";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmAdmin_FormClosed);
            this.Load += new System.EventHandler(this.frmAdmin_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.pnlContainer.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.pnlAICommand.ResumeLayout(false);
            this.pnlAICommand.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnSuppliers;
        private System.Windows.Forms.Button btnShipment;
        private System.Windows.Forms.Button btnCashier;
        private System.Windows.Forms.Button btnReports;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnEmployees;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel pnlAICommand;
        private System.Windows.Forms.Label lblAITitle;
        private System.Windows.Forms.Button btnAutoRestock;
        private System.Windows.Forms.RichTextBox lblAIAlert;
        private System.Windows.Forms.Panel pnlContainer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label lblRecommendedQty;
    }
}