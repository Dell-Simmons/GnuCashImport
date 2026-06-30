namespace FeeBayConnectionTester
{
    partial class AskForSimpleFinSetupToken
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
            tbxSetupToken = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // tbxSetupToken
            // 
            tbxSetupToken.Location = new Point(44, 185);
            tbxSetupToken.Multiline = true;
            tbxSetupToken.Name = "tbxSetupToken";
            tbxSetupToken.Size = new Size(633, 46);
            tbxSetupToken.TabIndex = 0;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(157, 307);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(126, 57);
            btnOk.TabIndex = 1;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click_1;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(425, 307);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(126, 57);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click_1;
            // 
            // AskForSimpleFinSetupToken
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(tbxSetupToken);
            Name = "AskForSimpleFinSetupToken";
            Text = "AskForSimpleFinSetupToken";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox tbxSetupToken;
        private Button btnOk;
        private Button btnCancel;
    }
}