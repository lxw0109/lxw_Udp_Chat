using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace lxw_Udp_Chat
{
    public partial class IfRecv : Form
    {
        //If you have clicked the button.
        public bool ifClick = false;
        //If you want to receive.
        public bool ifRecv = false;
        string info = "Do you want to receive the file: ";
      
        public IfRecv()
        {
            InitializeComponent();
            this.label1.Text = this.info;
        }
        public void setLabel(string filename)
        {
            this.label1.Text += filename + "?";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            try
            {
                this.ifClick = true;
                Button bt = sender as Button;
                switch (bt.Tag.ToString())
                {
                    case "Yes":
                        {
                            this.ifRecv = true;
                        }
                        break;
                    case "No":
                        {
                            this.ifRecv = false;
                        }
                        break;
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.ToString());
            }
        }
    }
}
