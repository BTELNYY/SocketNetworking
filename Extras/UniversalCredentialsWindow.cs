using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketNetworking.Extras
{
    public partial class UniversalCredentialsWindow : Form
    {
        /// <summary>
        /// If the form is cancelled or closed, this will have the arguments of "", "". Otherwise, the values of the <see cref="txtUsername"/> and <see cref="txtPassword"/> in that order.
        /// </summary>
        public event Action<string, string> AcceptedOrRejected;

        public UniversalCredentialsWindow()
        {
            InitializeComponent();
            FormClosing += (sender, args) =>
            {
                AcceptedOrRejected?.Invoke(txtUsername.Text, txtPassword.Text);
            };
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
