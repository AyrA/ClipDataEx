namespace ClipDataEx
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            label1 = new Label();
            label2 = new Label();
            TbId = new TextBox();
            TbPassword = new TextBox();
            BtnNewId = new Button();
            BtnSetPassword = new Button();
            BtnSendFile = new Button();
            OFD = new OpenFileDialog();
            LvFiles = new ListView();
            chFilename = new ColumnHeader();
            chSource = new ColumnHeader();
            chProgress = new ColumnHeader();
            chState = new ColumnHeader();
            CMS = new ContextMenuStrip(components);
            SaveAsToolStripMenuItem = new ToolStripMenuItem();
            DeleteToolStripMenuItem = new ToolStripMenuItem();
            BtnAbort = new Button();
            SFD = new SaveFileDialog();
            CMS.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(49, 17);
            label1.Name = "label1";
            label1.Size = new Size(16, 13);
            label1.TabIndex = 0;
            label1.Text = "Id";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 46);
            label2.Name = "label2";
            label2.Size = new Size(53, 13);
            label2.TabIndex = 3;
            label2.Text = "Password";
            // 
            // TbId
            // 
            TbId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            TbId.Location = new Point(71, 14);
            TbId.Name = "TbId";
            TbId.ReadOnly = true;
            TbId.Size = new Size(460, 20);
            TbId.TabIndex = 1;
            // 
            // TbPassword
            // 
            TbPassword.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            TbPassword.Location = new Point(71, 43);
            TbPassword.Name = "TbPassword";
            TbPassword.Size = new Size(460, 20);
            TbPassword.TabIndex = 4;
            TbPassword.UseSystemPasswordChar = true;
            // 
            // BtnNewId
            // 
            BtnNewId.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnNewId.Location = new Point(537, 12);
            BtnNewId.Name = "BtnNewId";
            BtnNewId.Size = new Size(75, 23);
            BtnNewId.TabIndex = 2;
            BtnNewId.Text = "New";
            BtnNewId.UseVisualStyleBackColor = true;
            BtnNewId.Click += BtnNewId_Click;
            // 
            // BtnSetPassword
            // 
            BtnSetPassword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnSetPassword.Location = new Point(537, 41);
            BtnSetPassword.Name = "BtnSetPassword";
            BtnSetPassword.Size = new Size(75, 23);
            BtnSetPassword.TabIndex = 5;
            BtnSetPassword.Text = "Set";
            BtnSetPassword.UseVisualStyleBackColor = true;
            BtnSetPassword.Click += BtnSetPassword_Click;
            // 
            // BtnSendFile
            // 
            BtnSendFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnSendFile.Location = new Point(456, 70);
            BtnSendFile.Name = "BtnSendFile";
            BtnSendFile.Size = new Size(75, 23);
            BtnSendFile.TabIndex = 6;
            BtnSendFile.Text = "Send File";
            BtnSendFile.UseVisualStyleBackColor = true;
            BtnSendFile.Click += BtnSendFile_Click;
            // 
            // OFD
            // 
            OFD.Filter = "All files|*.*";
            OFD.Title = "Select file to send";
            // 
            // LvFiles
            // 
            LvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LvFiles.Columns.AddRange(new ColumnHeader[] { chFilename, chSource, chProgress, chState });
            LvFiles.ContextMenuStrip = CMS;
            LvFiles.FullRowSelect = true;
            LvFiles.Location = new Point(12, 99);
            LvFiles.Name = "LvFiles";
            LvFiles.Size = new Size(599, 330);
            LvFiles.TabIndex = 8;
            LvFiles.UseCompatibleStateImageBehavior = false;
            LvFiles.View = View.Details;
            LvFiles.DoubleClick += LvFiles_DoubleClick;
            LvFiles.KeyDown += LvFiles_KeyDown;
            // 
            // chFilename
            // 
            chFilename.Text = "File name";
            chFilename.Width = 200;
            // 
            // chSource
            // 
            chSource.Text = "Sender";
            chSource.Width = 80;
            // 
            // chProgress
            // 
            chProgress.Text = "Progress";
            chProgress.Width = 150;
            // 
            // chState
            // 
            chState.Text = "State";
            chState.Width = 80;
            // 
            // CMS
            // 
            CMS.Items.AddRange(new ToolStripItem[] { SaveAsToolStripMenuItem, DeleteToolStripMenuItem });
            CMS.Name = "CMS";
            CMS.Size = new Size(124, 48);
            // 
            // SaveAsToolStripMenuItem
            // 
            SaveAsToolStripMenuItem.Name = "SaveAsToolStripMenuItem";
            SaveAsToolStripMenuItem.Size = new Size(123, 22);
            SaveAsToolStripMenuItem.Text = "&Save As...";
            SaveAsToolStripMenuItem.Click += SaveAsToolStripMenuItem_Click;
            // 
            // DeleteToolStripMenuItem
            // 
            DeleteToolStripMenuItem.Name = "DeleteToolStripMenuItem";
            DeleteToolStripMenuItem.Size = new Size(123, 22);
            DeleteToolStripMenuItem.Text = "&Delete";
            DeleteToolStripMenuItem.Click += DeleteToolStripMenuItem_Click;
            // 
            // BtnAbort
            // 
            BtnAbort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnAbort.Location = new Point(537, 70);
            BtnAbort.Name = "BtnAbort";
            BtnAbort.Size = new Size(75, 23);
            BtnAbort.TabIndex = 7;
            BtnAbort.Text = "Abort";
            BtnAbort.UseVisualStyleBackColor = true;
            BtnAbort.Click += BtnAbort_Click;
            // 
            // SFD
            // 
            SFD.Title = "Save File";
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(624, 441);
            Controls.Add(BtnAbort);
            Controls.Add(LvFiles);
            Controls.Add(BtnSendFile);
            Controls.Add(BtnSetPassword);
            Controls.Add(BtnNewId);
            Controls.Add(TbPassword);
            Controls.Add(TbId);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "FrmMain";
            Text = "Clip Data Extract";
            FormClosing += FrmMain_FormClosing;
            CMS.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox TbId;
        private TextBox TbPassword;
        private Button BtnNewId;
        private Button BtnSetPassword;
        private Button BtnSendFile;
        private OpenFileDialog OFD;
        private ListView LvFiles;
        private ColumnHeader chFilename;
        private ColumnHeader chSource;
        private ColumnHeader chProgress;
        private ColumnHeader chState;
        private ContextMenuStrip CMS;
        private ToolStripMenuItem SaveAsToolStripMenuItem;
        private ToolStripMenuItem DeleteToolStripMenuItem;
        private Button BtnAbort;
        private SaveFileDialog SFD;
    }
}