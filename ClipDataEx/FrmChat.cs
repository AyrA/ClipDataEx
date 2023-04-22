using System.Text.RegularExpressions;

namespace ClipDataEx
{
    public partial class FrmChat : Form
    {
        public delegate void SendMessageHandler(object sender, string message);

        public event SendMessageHandler SendMessage = delegate { };

        public FrmChat()
        {
            InitializeComponent();
        }

        public void AddMessage(uint sender, string message)
        {
            message = FilterMessage(message);
            TbReceive.Text += $"<{sender:X8}> {message}\r\n\r\n";
        }

        public void SetEditMessage(string message)
        {
            TbSend.Text = message;
            TbSend.Focus();
            TbSend.Select(TbSend.Text.Length, 0);
        }

        private static string FilterMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }
            message = message.Trim();
            message = Regex.Replace(message, "\\s+", " ");
            return message;
        }

        private void Send()
        {
            var msg = TbSend.Text;
            TbSend.Focus();
            TbSend.Text = "";
            SendMessage(this, msg);
        }

        private void TbSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;
                Send();
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            Send();
        }

        private void FrmChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Do not actually close the form when it's done by the user
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
