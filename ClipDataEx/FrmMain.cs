using System.Diagnostics;
using System.Text;

namespace ClipDataEx
{
    public partial class FrmMain : Form
    {
        private readonly ClipboardDataHandler handler;

        private readonly Dictionary<Guid, List<ClipboardProtocolMessage>> fileParts = new();

        public FrmMain()
        {
            handler = new(Handle);
            InitializeComponent();
            NewId();
#if DEBUG
            handler.SetKey(TbPassword.Text = "Test1234");
#endif
            handler.ClipboardData += Handler_ClipboardData;
            handler.SendFileComplete += Handler_SendFileComplete;
            handler.ChatMessage += Handler_ChatMessage;
        }

        private void Handler_ChatMessage(object sender, ClipboardProtocolMessage data)
        {
            if (data?.Data == null)
            {
                Debug.Print("Got ChatMessage event but data is null");
                return;
            }
            if (data.Data.Length > 1000)
            {
                Debug.Print("Got ChatMessage event but data is invalid (too long, is {0})", data.Data.Length);
                return;
            }
            var message = Encoding.UTF8.GetString(data.Data);
            SetChatMessage(data.SenderId, message);
            //NOOP
        }

        private void Handler_SendFileComplete(object sender, ClipboardProtocolMessage lastAck)
        {
            MessageBox.Show("File send complete", "Send file", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Handler_ClipboardData(object sender, ClipboardProtocolMessage data)
        {
            Debug.Print("Got message type: {0}", data.MessageType);
            if (data.MessageType == MessageType.FileData)
            {
                if (!fileParts.TryGetValue(data.FileId, out var messages))
                {
                    messages = new List<ClipboardProtocolMessage>();
                    fileParts.Add(data.FileId, messages);
                }
                messages.Add(data);
                RenderList();
            }
            if (!string.IsNullOrEmpty(data.FileName))
            {
                Debug.Print("File name: {0}", data.FileName);
            }
        }

        private void SetChatMessage(uint senderId, string message)
        {
            var f = OpenChat();
            f.AddMessage(senderId, message);
        }

        private FrmChat OpenChat()
        {
            var f = Application.OpenForms.OfType<FrmChat>().FirstOrDefault();
            if (f == null)
            {
                f = new FrmChat();
                f.SendMessage += delegate (object sender, string message)
                {
                    try
                    {
                        handler.SendChatMessage(message);
                        //Add message to chat after it has been sent
                        SetChatMessage(handler.Id, message);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Unable to send your message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //Add the message back to the edit box on error
                        f.SetEditMessage(message);
                    }
                };
            }
            f.Show();
            return f;
        }

        private void RenderList()
        {
            LvFiles.SuspendLayout();
            LvFiles.Items.Clear();
            foreach (var file in fileParts.Values.Where(m => m.Count > 0))
            {
                var first = file.First(m => m.SequenceNumber == 0);
                var sum = file.Sum(m => m.Data?.Length ?? 0);
                var total = (int)first.FileSize;
                var perc = Math.Min(100, sum * 100 / total);
                //Field order: Name, Sender, Progress, State
                var item = LvFiles.Items.Add(first.FileName);
                item.Tag = first.FileId;
                item.SubItems.Add(first.SenderId.ToString("X8"));
                item.SubItems.Add($"{Tools.FormatSize(sum)}/{Tools.FormatSize(total)} ({perc}%)");
                item.SubItems.Add(sum == total ? "Complete" : "In progress");
            }
            LvFiles.ResumeLayout();
        }

        private void NewId()
        {
            if (handler.Busy)
            {
                MessageBox.Show("You cannot change the id when a file transfer is ongoing", "Ongoing transfer", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            handler.NewId();
            TbId.Text = handler.Id.ToString("X8");
        }

        private void SaveSelectedItems()
        {
            var incomplete = new List<string>();
            foreach (var item in LvFiles.SelectedItems.OfType<ListViewItem>())
            {
                var id = (Guid)item.Tag;
                var fileEntry = fileParts[id];
                var index = fileEntry.First(m => m.SequenceNumber == 0);
                if ((ulong)fileEntry.Sum(m => m.Data?.Length ?? 0) != index.FileSize)
                {
                    incomplete.Add(index.FileName!);
                    continue;
                }
                var ext = Path.GetExtension(index.FileName);
                //File without extension
                if (string.IsNullOrEmpty(ext))
                {
                    ext = index.FileName;
                }
                else
                {
                    //Add asterisk to create file name mask with extension
                    ext = $"*{ext}";
                }
                SFD.FileName = index.FileName;
                SFD.Filter = $"{index.FileName}|{ext}|All files|*.*";
                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    using var fs = SFD.OpenFile();
                    foreach (var part in fileEntry.OrderBy(m => m.SequenceNumber))
                    {
                        if (part.Data == null)
                        {
                            throw null!;
                        }
                        fs.Write(part.Data, 0, part.Data.Length);
                    }
                }
            }
            if (incomplete.Count > 0)
            {
                var msg = "The following files are incomplete and were skipped:\r\n- " + string.Join("\r\n- ", incomplete);
                MessageBox.Show(msg, "Incomplete files were skipped", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void DeleteSelectedItems()
        {
            var reload = false;
            foreach (var item in LvFiles.SelectedItems.OfType<ListViewItem>())
            {
                var id = (Guid)item.Tag;
                var fileEntry = fileParts[id];
                var index = fileEntry.First(m => m.SequenceNumber == 0);
                if (MessageBox.Show($"Remove {index.FileName} from the list?", "Delete file", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    fileParts.Remove(id);
                    reload = true;
                }
            }
            if (reload)
            {
                RenderList();
            }
        }

        #region Event Handler

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            handler.Dispose();
        }

        private void BtnNewId_Click(object sender, EventArgs e)
        {
            NewId();
        }

        private void BtnSetPassword_Click(object sender, EventArgs e)
        {
            if (handler.Busy)
            {
                MessageBox.Show("You cannot change the password when a file transfer is ongoing", "Ongoing transfer", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            try
            {
                handler.SetKey(TbPassword.Text);
                MessageBox.Show("Key has been set", "Key set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                TbPassword.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Key not set", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSendFile_Click(object sender, EventArgs e)
        {
            if (!handler.Ready)
            {
                MessageBox.Show("Clipboard handler is not ready. Ensure an id and password has been set",
                    "File select", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (handler.Busy)
            {
                MessageBox.Show("You cannot send a new file a file transfer is already ongoing", "Ongoing transfer", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                handler.SendFile(OFD.FileName);
            }
        }

        private void BtnAbort_Click(object sender, EventArgs e)
        {
            if (handler.Busy)
            {
                if (MessageBox.Show("Abort transfer?", "Cancel operation", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    handler.CancelFile();
                }
            }
            else
            {
                MessageBox.Show("There's no outbound transfer ongoing", "No transfer", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSelectedItems();
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedItems();
        }

        private void LvFiles_DoubleClick(object sender, EventArgs e)
        {
            SaveSelectedItems();
        }

        private void LvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = e.Handled = true;
                SaveSelectedItems();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = e.Handled = true;
                DeleteSelectedItems();
            }
        }

        private void BtnChat_Click(object sender, EventArgs e)
        {
            OpenChat();
        }

        #endregion
    }
}