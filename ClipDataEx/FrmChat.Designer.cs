namespace ClipDataEx
{
    partial class FrmChat
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
            BtnSend = new Button();
            TbSend = new TextBox();
            TbReceive = new TextBox();
            SuspendLayout();
            // 
            // BtnSend
            // 
            BtnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            BtnSend.Location = new Point(514, 285);
            BtnSend.Name = "BtnSend";
            BtnSend.Size = new Size(75, 23);
            BtnSend.TabIndex = 0;
            BtnSend.Text = "&Send";
            BtnSend.UseVisualStyleBackColor = true;
            BtnSend.Click += BtnSend_Click;
            // 
            // TbSend
            // 
            TbSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            TbSend.Location = new Point(12, 287);
            TbSend.Name = "TbSend";
            TbSend.Size = new Size(496, 20);
            TbSend.TabIndex = 1;
            TbSend.KeyDown += TbSend_KeyDown;
            // 
            // TbReceive
            // 
            TbReceive.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            TbReceive.Location = new Point(12, 12);
            TbReceive.Multiline = true;
            TbReceive.Name = "TbReceive";
            TbReceive.ReadOnly = true;
            TbReceive.ScrollBars = ScrollBars.Vertical;
            TbReceive.Size = new Size(577, 267);
            TbReceive.TabIndex = 2;
            // 
            // FrmChat
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(601, 320);
            Controls.Add(TbReceive);
            Controls.Add(TbSend);
            Controls.Add(BtnSend);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmChat";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Chat";
            FormClosing += FrmChat_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button BtnSend;
        private TextBox TbSend;
        private TextBox TbReceive;
    }
}