namespace FinalProject
{
    partial class frmReturns
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.txtInvoiceSearch = new System.Windows.Forms.TextBox();
            this.btnReturnItem = new MaterialSkin.Controls.MaterialButton();
            this.dgvInvoiceDetails = new System.Windows.Forms.DataGridView();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInvoiceDetails)).BeginInit();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.txtInvoiceSearch);
            this.flowLayoutPanel1.Controls.Add(this.btnReturnItem);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 64);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(794, 43);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // txtInvoiceSearch
            // 
            this.txtInvoiceSearch.Location = new System.Drawing.Point(3, 3);
            this.txtInvoiceSearch.Multiline = true;
            this.txtInvoiceSearch.Name = "txtInvoiceSearch";
            this.txtInvoiceSearch.Size = new System.Drawing.Size(437, 38);
            this.txtInvoiceSearch.TabIndex = 0;
            this.txtInvoiceSearch.TextChanged += new System.EventHandler(this.txtInvoiceSearch_TextChanged);
            // 
            // btnReturnItem
            // 
            this.btnReturnItem.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.btnReturnItem.Depth = 0;
            this.btnReturnItem.HighEmphasis = true;
            this.btnReturnItem.Icon = null;
            this.btnReturnItem.Location = new System.Drawing.Point(447, 6);
            this.btnReturnItem.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnReturnItem.MouseState = MaterialSkin.MouseState.HOVER;
            this.btnReturnItem.Name = "btnReturnItem";
            this.btnReturnItem.NoAccentTextColor = System.Drawing.Color.Empty;
            this.btnReturnItem.Size = new System.Drawing.Size(184, 36);
            this.btnReturnItem.TabIndex = 1;
            this.btnReturnItem.Text = "Return to Inventory";
            this.btnReturnItem.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.btnReturnItem.UseAccentColor = false;
            this.btnReturnItem.UseVisualStyleBackColor = true;
            this.btnReturnItem.Click += new System.EventHandler(this.btnReturnItem_Click);
            // 
            // dgvInvoiceDetails
            // 
            this.dgvInvoiceDetails.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvInvoiceDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvInvoiceDetails.Location = new System.Drawing.Point(3, 107);
            this.dgvInvoiceDetails.Name = "dgvInvoiceDetails";
            this.dgvInvoiceDetails.Size = new System.Drawing.Size(794, 340);
            this.dgvInvoiceDetails.TabIndex = 1;
            this.dgvInvoiceDetails.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvInvoiceDetails_CellContentClick);
            // 
            // frmReturns
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dgvInvoiceDetails);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "frmReturns";
            this.Text = "Returns";
            this.Load += new System.EventHandler(this.frmReturns_Load);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInvoiceDetails)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TextBox txtInvoiceSearch;
        private System.Windows.Forms.DataGridView dgvInvoiceDetails;
        private MaterialSkin.Controls.MaterialButton btnReturnItem;
    }
}