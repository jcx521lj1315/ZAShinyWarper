namespace ZAShinyWarper
{
    partial class WarpProgressForm
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
            lblWarping = new Label();
            btn_Cancel = new Button();
            SuspendLayout();
            // 
            // lblWarping
            // 
            lblWarping.Location = new Point(12, 9);
            lblWarping.Name = "lblWarping";
            lblWarping.Size = new Size(308, 89);
            lblWarping.TabIndex = 0;
            lblWarping.Text = "Warping...";
            lblWarping.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btn_Cancel
            // 
            btn_Cancel.Dock = DockStyle.Bottom;
            btn_Cancel.FlatStyle = FlatStyle.Flat;
            btn_Cancel.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btn_Cancel.Location = new Point(0, 86);
            btn_Cancel.Margin = new Padding(0);
            btn_Cancel.Name = "btn_Cancel";
            btn_Cancel.Size = new Size(332, 21);
            btn_Cancel.TabIndex = 1;
            btn_Cancel.Text = "Cancel";
            btn_Cancel.UseVisualStyleBackColor = true;
            btn_Cancel.Click += OnClickCancelButton;
            // 
            // WarpProgressForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(332, 107);
            ControlBox = false;
            Controls.Add(btn_Cancel);
            Controls.Add(lblWarping);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "WarpProgressForm";
            StartPosition = FormStartPosition.CenterParent;
            TopMost = true;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblWarping;
        private Button btn_Cancel;
    }
}