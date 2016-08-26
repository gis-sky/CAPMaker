using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CAPMaker
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();

            init();
        }

        void init()
        {
            richTextBox1.Text = Properties.Resources.Helpdoc;
        }
        
    }
}
