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
            btnSparkCC = new Button();
            btnStripe = new Button();
            btnPeakCU = new Button();
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
            // btnSparkCC
            // 
            btnSparkCC.Location = new Point(472, 299);
            btnSparkCC.Name = "btnSparkCC";
            btnSparkCC.Size = new Size(169, 94);
            btnSparkCC.TabIndex = 1;
            btnSparkCC.Text = "Spark CC\r\n";
            btnSparkCC.UseVisualStyleBackColor = true;
            btnSparkCC.Click += btnSimpleFin_Click;
            // 
            // btnStripe
            // 
            btnStripe.Location = new Point(472, 137);
            btnStripe.Name = "btnStripe";
            btnStripe.Size = new Size(169, 94);
            btnStripe.TabIndex = 2;
            btnStripe.Text = "Stripe (DSD)";
            btnStripe.UseVisualStyleBackColor = true;
            btnStripe.Click += btnStripe_Click;
            // 
            // btnPeakCU
            // 
            btnPeakCU.Location = new Point(472, 413);
            btnPeakCU.Name = "btnPeakCU";
            btnPeakCU.Size = new Size(169, 94);
            btnPeakCU.TabIndex = 5;
            btnPeakCU.Text = "Peak CU";
            btnPeakCU.UseVisualStyleBackColor = true;
            btnPeakCU.Click += btnPeakCU_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 573);
            Controls.Add(btnPeakCU);
            Controls.Add(btnStripe);
            Controls.Add(btnSparkCC);
            Controls.Add(btnFeeBay);
            Font = new Font("Segoe UI", 12F);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button btnFeeBay;
        private Button btnSparkCC;
        private Button btnStripe;
        private Button btnPeakCU;
    }
}
