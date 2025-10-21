namespace PLAWarper
{
    partial class LabelForm
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
            lblInfoText = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // lblInfoText
            // 
            lblInfoText.Location = new System.Drawing.Point(12, 9);
            lblInfoText.Name = "lblInfoText";
            lblInfoText.Size = new System.Drawing.Size(308, 89);
            lblInfoText.TabIndex = 0;
            lblInfoText.Text = "Text goes here";
            lblInfoText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // LabelForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(332, 107);
            ControlBox = false;
            Controls.Add(lblInfoText);
            Name = "LabelForm";
            Text = "LabelForm";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblInfoText;
    }
}