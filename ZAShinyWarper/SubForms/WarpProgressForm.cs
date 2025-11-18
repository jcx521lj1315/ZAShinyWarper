namespace ZAShinyWarper
{
    public partial class WarpProgressForm : Form
    {

        public event EventHandler? CancelRequested;
        public string ShownText => lblWarping.Text;

        public void SetText(string text)
        {
            lblWarping.Text = text;
        }

        public WarpProgressForm()
        {
            InitializeComponent();
        }

        private void OnClickCancelButton(object sender, EventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
