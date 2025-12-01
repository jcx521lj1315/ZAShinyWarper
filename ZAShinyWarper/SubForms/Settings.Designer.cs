using System.Windows.Forms;
using ZAShinyWarper;

namespace ZAShinyWarper
{
    partial class SettingsForm
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
            components = new System.ComponentModel.Container();
            programConfigBindingSource = new BindingSource(components);
            dGWebhookSettings = new DataGridView();
            webhookColumn = new DataGridViewTextBoxColumn();
            messageColumn = new DataGridViewTextBoxColumn();
            checkColumn = new DataGridViewCheckBoxColumn();
            btnSave = new Button();
            btnCancel = new Button();
            btnRemoveRow = new Button();
            btnAddRow = new Button();
            btnTest = new Button();
            tCSettings = new TabControl();
            tPSettings = new TabPage();
            gBConnection = new GroupBox();
            cBProtocol = new ComboBox();
            tB_Port = new TextBox();
            lblProtocol = new Label();
            lblPort = new Label();
            tB_IP = new TextBox();
            lbl_IP = new Label();
            gBPopUps = new GroupBox();
            cBMatchNotify = new CheckBox();
            cBFullCache = new CheckBox();
            cBFilterNonMatch = new CheckBox();
            tPWebhook = new TabPage();
            ((System.ComponentModel.ISupportInitialize)programConfigBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dGWebhookSettings).BeginInit();
            tCSettings.SuspendLayout();
            tPSettings.SuspendLayout();
            gBConnection.SuspendLayout();
            gBPopUps.SuspendLayout();
            tPWebhook.SuspendLayout();
            SuspendLayout();
            // 
            // programConfigBindingSource
            // 
            programConfigBindingSource.DataSource = typeof(ProgramConfig);
            // 
            // dGWebhookSettings
            // 
            dGWebhookSettings.AllowUserToAddRows = false;
            dGWebhookSettings.AllowUserToResizeColumns = false;
            dGWebhookSettings.AllowUserToResizeRows = false;
            dGWebhookSettings.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dGWebhookSettings.Columns.AddRange(new DataGridViewColumn[] { webhookColumn, messageColumn, checkColumn });
            dGWebhookSettings.Dock = DockStyle.Top;
            dGWebhookSettings.Location = new Point(3, 3);
            dGWebhookSettings.MultiSelect = false;
            dGWebhookSettings.Name = "dGWebhookSettings";
            dGWebhookSettings.RowHeadersVisible = false;
            dGWebhookSettings.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dGWebhookSettings.RowTemplate.Resizable = DataGridViewTriState.False;
            dGWebhookSettings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dGWebhookSettings.Size = new Size(523, 187);
            dGWebhookSettings.TabIndex = 0;
            // 
            // webhookColumn
            // 
            webhookColumn.HeaderText = "Webhook Address";
            webhookColumn.Name = "webhookColumn";
            webhookColumn.Width = 260;
            // 
            // messageColumn
            // 
            messageColumn.HeaderText = "Message Content";
            messageColumn.Name = "messageColumn";
            messageColumn.Width = 200;
            // 
            // checkColumn
            // 
            checkColumn.HeaderText = "Enabled";
            checkColumn.Name = "checkColumn";
            checkColumn.Width = 60;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(366, 262);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += OnClickSave;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(447, 262);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += OnClickCancel;
            // 
            // btnRemoveRow
            // 
            btnRemoveRow.Location = new Point(87, 196);
            btnRemoveRow.Name = "btnRemoveRow";
            btnRemoveRow.Size = new Size(75, 23);
            btnRemoveRow.TabIndex = 4;
            btnRemoveRow.Text = "Remove";
            btnRemoveRow.UseVisualStyleBackColor = true;
            btnRemoveRow.Click += OnClickRemove;
            // 
            // btnAddRow
            // 
            btnAddRow.Location = new Point(6, 196);
            btnAddRow.Name = "btnAddRow";
            btnAddRow.Size = new Size(75, 23);
            btnAddRow.TabIndex = 3;
            btnAddRow.Text = "Add";
            btnAddRow.UseVisualStyleBackColor = true;
            btnAddRow.Click += OnClickAdd;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(168, 196);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(75, 23);
            btnTest.TabIndex = 5;
            btnTest.Text = "Test";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Click += OnClickTest;
            // 
            // tCSettings
            // 
            tCSettings.Controls.Add(tPSettings);
            tCSettings.Controls.Add(tPWebhook);
            tCSettings.Location = new Point(2, 4);
            tCSettings.Name = "tCSettings";
            tCSettings.SelectedIndex = 0;
            tCSettings.Size = new Size(537, 252);
            tCSettings.TabIndex = 6;
            // 
            // tPSettings
            // 
            tPSettings.Controls.Add(gBConnection);
            tPSettings.Controls.Add(gBPopUps);
            tPSettings.Location = new Point(4, 24);
            tPSettings.Name = "tPSettings";
            tPSettings.Padding = new Padding(3);
            tPSettings.Size = new Size(529, 224);
            tPSettings.TabIndex = 0;
            tPSettings.Text = "Main";
            tPSettings.UseVisualStyleBackColor = true;
            // 
            // gBConnection
            // 
            gBConnection.Controls.Add(cBProtocol);
            gBConnection.Controls.Add(tB_Port);
            gBConnection.Controls.Add(lblProtocol);
            gBConnection.Controls.Add(lblPort);
            gBConnection.Controls.Add(tB_IP);
            gBConnection.Controls.Add(lbl_IP);
            gBConnection.Location = new Point(6, 6);
            gBConnection.Name = "gBConnection";
            gBConnection.Size = new Size(200, 107);
            gBConnection.TabIndex = 4;
            gBConnection.TabStop = false;
            gBConnection.Text = "Connection";
            // 
            // cBProtocol
            // 
            cBProtocol.FormattingEnabled = true;
            cBProtocol.Location = new Point(61, 74);
            cBProtocol.Name = "cBProtocol";
            cBProtocol.Size = new Size(61, 23);
            cBProtocol.TabIndex = 21;
            // 
            // tB_Port
            // 
            tB_Port.Location = new Point(61, 45);
            tB_Port.Margin = new Padding(4, 3, 4, 3);
            tB_Port.MaxLength = 15;
            tB_Port.Name = "tB_Port";
            tB_Port.RightToLeft = RightToLeft.Yes;
            tB_Port.Size = new Size(61, 23);
            tB_Port.TabIndex = 20;
            tB_Port.Text = "1";
            // 
            // lblProtocol
            // 
            lblProtocol.AutoSize = true;
            lblProtocol.Location = new Point(7, 77);
            lblProtocol.Margin = new Padding(4, 0, 4, 0);
            lblProtocol.Name = "lblProtocol";
            lblProtocol.Size = new Size(55, 15);
            lblProtocol.TabIndex = 22;
            lblProtocol.Text = "Protocol:";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(7, 48);
            lblPort.Margin = new Padding(4, 0, 4, 0);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(56, 15);
            lblPort.TabIndex = 19;
            lblPort.Text = "USB Port:";
            // 
            // tB_IP
            // 
            tB_IP.Location = new Point(27, 16);
            tB_IP.Margin = new Padding(4, 3, 4, 3);
            tB_IP.MaxLength = 15;
            tB_IP.Name = "tB_IP";
            tB_IP.RightToLeft = RightToLeft.Yes;
            tB_IP.Size = new Size(95, 23);
            tB_IP.TabIndex = 3;
            tB_IP.Text = "192.168.0.1";
            // 
            // lbl_IP
            // 
            lbl_IP.AutoSize = true;
            lbl_IP.Location = new Point(7, 19);
            lbl_IP.Margin = new Padding(4, 0, 4, 0);
            lbl_IP.Name = "lbl_IP";
            lbl_IP.Size = new Size(20, 15);
            lbl_IP.TabIndex = 2;
            lbl_IP.Text = "IP:";
            // 
            // gBPopUps
            // 
            gBPopUps.Controls.Add(cBMatchNotify);
            gBPopUps.Controls.Add(cBFullCache);
            gBPopUps.Controls.Add(cBFilterNonMatch);
            gBPopUps.Location = new Point(6, 119);
            gBPopUps.Name = "gBPopUps";
            gBPopUps.Size = new Size(138, 97);
            gBPopUps.TabIndex = 3;
            gBPopUps.TabStop = false;
            gBPopUps.Text = "Pop-Up Notifications";
            // 
            // cBMatchNotify
            // 
            cBMatchNotify.AutoSize = true;
            cBMatchNotify.Location = new Point(6, 22);
            cBMatchNotify.Name = "cBMatchNotify";
            cBMatchNotify.Size = new Size(100, 19);
            cBMatchNotify.TabIndex = 0;
            cBMatchNotify.Text = "Filter Matches";
            cBMatchNotify.UseVisualStyleBackColor = true;
            // 
            // cBFullCache
            // 
            cBFullCache.AutoSize = true;
            cBFullCache.Location = new Point(6, 72);
            cBFullCache.Name = "cBFullCache";
            cBFullCache.Size = new Size(81, 19);
            cBFullCache.TabIndex = 2;
            cBFullCache.Text = "Full Cache";
            cBFullCache.UseVisualStyleBackColor = true;
            // 
            // cBFilterNonMatch
            // 
            cBFilterNonMatch.AutoSize = true;
            cBFilterNonMatch.Location = new Point(6, 47);
            cBFilterNonMatch.Name = "cBFilterNonMatch";
            cBFilterNonMatch.Size = new Size(128, 19);
            cBFilterNonMatch.TabIndex = 1;
            cBFilterNonMatch.Text = "Filter Non-Matches";
            cBFilterNonMatch.UseVisualStyleBackColor = true;
            // 
            // tPWebhook
            // 
            tPWebhook.Controls.Add(dGWebhookSettings);
            tPWebhook.Controls.Add(btnTest);
            tPWebhook.Controls.Add(btnRemoveRow);
            tPWebhook.Controls.Add(btnAddRow);
            tPWebhook.Location = new Point(4, 24);
            tPWebhook.Name = "tPWebhook";
            tPWebhook.Padding = new Padding(3);
            tPWebhook.Size = new Size(529, 224);
            tPWebhook.TabIndex = 1;
            tPWebhook.Text = "Webhook";
            tPWebhook.UseVisualStyleBackColor = true;
            // 
            // WebhookForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(534, 294);
            Controls.Add(tCSettings);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "WebhookForm";
            Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)programConfigBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)dGWebhookSettings).EndInit();
            tCSettings.ResumeLayout(false);
            tPSettings.ResumeLayout(false);
            gBConnection.ResumeLayout(false);
            gBConnection.PerformLayout();
            gBPopUps.ResumeLayout(false);
            gBPopUps.PerformLayout();
            tPWebhook.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private BindingSource programConfigBindingSource;
        private DataGridView dGWebhookSettings;
        private DataGridViewTextBoxColumn webhookColumn;
        private DataGridViewTextBoxColumn messageColumn;
        private DataGridViewCheckBoxColumn checkColumn;
        private Button btnSave;
        private Button btnCancel;
        private Button btnRemoveRow;
        private Button btnAddRow;
        private Button btnTest;
        private TabControl tCSettings;
        private TabPage tPWebhook;
        private TabPage tPSettings;
        private CheckBox cBFullCache;
        private CheckBox cBFilterNonMatch;
        private CheckBox cBMatchNotify;
        private GroupBox gBPopUps;
        private GroupBox gBConnection;
        private TextBox tB_IP;
        private Label lbl_IP;
        private TextBox tB_Port;
        private Label lblPort;
        private ComboBox cBProtocol;
        private Label lblProtocol;
    }
}