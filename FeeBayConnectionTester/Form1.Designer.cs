namespace FeeBayConnectionTester
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnFeeBay = new Button();
            btnPeakCu = new Button();
            SuspendLayout();
            // 
            // btnFeeBay
            // 
            btnFeeBay.Location = new Point(472, 28);
            btnFeeBay.Name = "btnFeeBay";
            btnFeeBay.Size = new Size(169, 94);
            btnFeeBay.TabIndex = 0;
            btnFeeBay.Text = "feeBay";
            btnFeeBay.UseVisualStyleBackColor = true;
            btnFeeBay.Click += btnFeeBay_Click;
            // 
            // btnPeakCu
            // 
            btnPeakCu.Location = new Point(472, 149);
            btnPeakCu.Name = "btnPeakCu";
            btnPeakCu.Size = new Size(169, 94);
            btnPeakCu.TabIndex = 1;
            btnPeakCu.Text = "Peak CU";
            btnPeakCu.UseVisualStyleBackColor = true;
            btnPeakCu.Click += btnSimpleFin_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnPeakCu);
            Controls.Add(btnFeeBay);
            Font = new Font("Segoe UI", 12F);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button btnFeeBay;
        private Button btnPeakCu;
    }
}
