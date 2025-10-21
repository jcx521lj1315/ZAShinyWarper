using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLAWarper
{
    public partial class LabelForm : Form
    {
        public string ShownText => lblInfoText.Text;

        public void SetText(string text)
        {
            lblInfoText.Text = text;
        }

        public LabelForm()
        {
            InitializeComponent();
        }
    }
}
