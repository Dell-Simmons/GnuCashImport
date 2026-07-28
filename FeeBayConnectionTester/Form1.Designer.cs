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
            monthsBackPicker = new NumericUpDown();
            label1 = new Label();
            panel1 = new Panel();
            ((System.ComponentModel.ISupportInitialize)monthsBackPicker).BeginInit();
            panel1.SuspendLayout();
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
            btnSparkCC.Location = new Point(472, 144);
            btnSparkCC.Name = "btnSparkCC";
            btnSparkCC.Size = new Size(169, 94);
            btnSparkCC.TabIndex = 1;
            btnSparkCC.Text = "Spark CC\r\n";
            btnSparkCC.UseVisualStyleBackColor = true;
            btnSparkCC.Click += btnSimpleFin_Click;
            // 
            // btnStripe
            // 
            btnStripe.Location = new Point(472, 376);
            btnStripe.Name = "btnStripe";
            btnStripe.Size = new Size(169, 94);
            btnStripe.TabIndex = 2;
            btnStripe.Text = "Stripe (DSD)";
            btnStripe.UseVisualStyleBackColor = true;
            btnStripe.Click += btnStripe_Click;
            // 
            // btnPeakCU
            // 
            btnPeakCU.Location = new Point(472, 260);
            btnPeakCU.Name = "btnPeakCU";
            btnPeakCU.Size = new Size(169, 94);
            btnPeakCU.TabIndex = 5;
            btnPeakCU.Text = "Peak CU";
            btnPeakCU.UseVisualStyleBackColor = true;
            btnPeakCU.Click += btnPeakCU_Click;
            // 
            // monthsBackPicker
            // 
            monthsBackPicker.Location = new Point(161, 15);
            monthsBackPicker.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            monthsBackPicker.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            monthsBackPicker.Name = "monthsBackPicker";
            monthsBackPicker.Size = new Size(55, 39);
            monthsBackPicker.TabIndex = 6;
            monthsBackPicker.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 17);
            label1.Name = "label1";
            label1.Size = new Size(152, 32);
            label1.TabIndex = 7;
            label1.Text = "Months Back";
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(monthsBackPicker);
            panel1.Location = new Point(448, 476);
            panel1.Name = "panel1";
            panel1.Size = new Size(229, 69);
            panel1.TabIndex = 8;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 573);
            Controls.Add(panel1);
            Controls.Add(btnPeakCU);
            Controls.Add(btnStripe);
            Controls.Add(btnSparkCC);
            Controls.Add(btnFeeBay);
            Font = new Font("Segoe UI", 12F);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)monthsBackPicker).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnFeeBay;
        private Button btnSparkCC;
        private Button btnStripe;
        private Button btnPeakCU;
        private NumericUpDown monthsBackPicker;
        private Label label1;
        private Panel panel1;
    }
}
