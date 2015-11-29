using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Byepass
{
    public partial class PinPrompt : Form
    {
        private String pin = "";

        public PinPrompt()
        {
            InitializeComponent();
        }

        private void frmPinPrompt_Load(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            pin = txtPin.Text;
            this.Close();
        }

        public String getPin()
        {
            return pin;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
