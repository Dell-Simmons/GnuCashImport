using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FeeBayConnectionTester
{
    public partial class FormAskForSimpleFinSetupToken : Form
    {

        public string SetupToken
        {
            get => tbxSetupToken.Text;
            //set => tbxSetupToken.Text = value;
        }

        public FormAskForSimpleFinSetupToken()
        {
            InitializeComponent();
        }

      

        private void btnOk_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
