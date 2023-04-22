using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ClipDataEx
{
    public class ClipboardDataHandler : IDisposable, IMessageFilter
    {
        public delegate void ClipboardDataEventHandler(object sender, ClipboardProtocolMessage data);
        public delegate void SendFileCompleteHandler(object sender, ClipboardProtocolMessage lastAck);

        public event ClipboardDataEventHandler ClipboardData = delegate { };
        public event SendFileCompleteHandler SendFileComplete = delegate { };

        [DllImport("User32.dll")]
        private static extern bool AddClipboardFormatListener(IntPtr hWnd);
        [DllImport("User32.dll")]
        private static extern bool RemoveClipboardFormatListener(IntPtr hWnd);

        public const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int KeyLength = 32;
        private const int ChunkSize = 0xFFFFF;

        private byte[]? key;
        private readonly IntPtr hWnd;
        private readonly List<Guid> processedItems = new();
        private ClipboardProtocolMessage? lastData;
        private byte[]? pendingData;

        public bool Ready => key != null && key.Length == 32;
        public bool Busy => lastData != null || pendingData != null;
        public uint Id { get; private set; }

        public ClipboardDataHandler(IntPtr hWnd)
        {
            NewId();
            Application.AddMessageFilter(this);
            AddClipboardFormatListener(hWnd);
            this.hWnd = hWnd;
        }

        public void NewId()
        {
            Id = (uint)(Random.Shared.NextInt64() & 0xFFFFFFFF);
        }

        public void SetKey(byte[] key)
        {
            if (key is null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.Length != KeyLength)
            {
                throw new ArgumentException($"Key must be {KeyLength} bytes", nameof(key));
            }
            this.key = (byte[])key.Clone();
        }

        public void SetKey(string Password)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                throw new ArgumentException($"'{nameof(Password)}' cannot be null or whitespace.", nameof(Password));
            }

            key = SHA256.HashData(Encoding.UTF8.GetBytes(Password));
        }

        public void CancelFile()
        {
            lastData = null;
            pendingData = null;
        }

        public void SendFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"'{nameof(fileName)}' cannot be null or whitespace.", nameof(fileName));
            }
            SendFile(Path.GetFileName(fileName), File.ReadAllBytes(fileName));
        }

        public void SendFile(string fileName, byte[] data)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException($"'{nameof(fileName)}' cannot be null or empty.", nameof(fileName));
            }
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length == 0)
            {
                throw new ArgumentException("File is empty", nameof(data));
            }
            if (pendingData != null || lastData != null)
            {
                throw new InvalidOperationException("A file send is already in progress. Cancel it before sending a new file");
            }
            lastData = new ClipboardProtocolMessage()
            {
                Data = data.Take(ChunkSize).ToArray(),
                SenderId = Id,
                FileId = Guid.NewGuid(),
                FileName = Path.GetFileName(fileName),
                FileSize = (ulong)data.Length,
                MessageType = MessageType.FileData,
                SequenceNumber = 0
            };
            pendingData = data.Skip(ChunkSize).ToArray();
            SendData(lastData.Serialize());
        }

        private ClipboardProtocolMessage? CheckClipboard()
        {
            if (!Ready)
            {
                throw new InvalidOperationException("Key has not been set yet");
            }
            if (Clipboard.ContainsImage())
            {
                Debug.Print("Clipboard contains image data. Trying to decode it");
                var data = ReceiveData();
                if (data == null)
                {
                    Debug.Print("Decoding image data failed. Not a ClipDataEx formatted image, wrong decryption key, or duplicate message.");
                    return null;
                }
                Debug.Print("Decoded {0} bytes", data.Length);
                return new ClipboardProtocolMessage(data);
            }
            return null;
        }

        private void AckTransfer(ClipboardProtocolMessage result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            Debug.Print("Acknowledging file part from {0:X8}", result.SenderId);
            var message = new ClipboardProtocolMessage
            {
                MessageType = MessageType.Ack,
                SenderId = Id,
                FileId = result.FileId,
                SequenceNumber = result.SequenceNumber
            };
            SendData(message.Serialize());
        }

        private void SendNextPart(ClipboardProtocolMessage result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            //This was not an Ack for us because we're not sending
            if (lastData == null)
            {
                return;
            }
            //Not for us. Valid file but not the one we're trying to send.
            //We simply resend our part
            if (result.FileId != lastData.FileId)
            {
                SendData(lastData.Serialize());
                return;
            }
            //This Ack is not for the last sequence number. We resend the packet
            if (result.SequenceNumber != lastData.SequenceNumber)
            {
                SendData(lastData.Serialize());
                return;
            }
            //At this point the Ack is correct. We send the next part if any
            if (pendingData != null && pendingData.Length > 0)
            {
                Debug.Print("Sending next file part to {0:X8}", result.SenderId);
                ++lastData.SequenceNumber;
                lastData.Data = pendingData.Take(ChunkSize).ToArray();
                pendingData = pendingData.Skip(ChunkSize).ToArray();
                SendData(lastData.Serialize());
                return;
            }
            Debug.Print("Sending file to {0:X8} complete", result.SenderId);

            //No more data to send. Transfer is complete
            lastData = null;
            pendingData = null;
            SendFileComplete(this, result);
        }

        private void Pong(ClipboardProtocolMessage result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            Debug.Print("Responding to ping packet from {0:X8}", result.SenderId);
            var message = new ClipboardProtocolMessage
            {
                MessageType = MessageType.Pong,
                SenderId = Id
            };
            SendData(message.Serialize());
        }

        private int SendData(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            //Just using "new Guid()" is not recommended because it created v4 Guids,
            //which have some bits hardcoded.
            var random = new Guid(RandomNumberGenerator.GetBytes(16));
            processedItems.Add(random);
            Debug.Print("Encrypt {0} bytes before storing in clipboard", data.Length);
            var encrypted = random.ToByteArray().Concat(Encrypt(data)).ToArray();
            Debug.Print("Encoding data into image");
            using var i = ImageEncoder.Encode(encrypted, out int sent);
            Debug.Print("Image of resolution {0} created", i.Size);
            Clipboard.Clear();
            Clipboard.SetImage(i);
            Debug.Print("Clipboard image set. {0}/{1} bytes of data processed", sent, encrypted.Length);
            return sent;
        }

        private byte[]? ReceiveData()
        {
            if (Clipboard.ContainsImage())
            {
                try
                {
                    using var b = (Bitmap)Clipboard.GetImage();
                    Debug.Print("Got image from clipboard with resolution {0}", b.Size);
                    var bytes = ImageEncoder.Decode(b);
                    //Cannot be our image if it's too small
                    if (bytes.Length < 16)
                    {
                        Debug.Print("Ignoring image; too small");
                        return null;
                    }
                    var id = new Guid(bytes.Take(16).ToArray());
                    //Check for duplicate image
                    if (processedItems.Contains(id))
                    {
                        Debug.Print("Ignoring image; duplicate");
                        return null;
                    }
                    processedItems.Add(id);
                    return Decrypt(bytes.Skip(16).ToArray());
                }
                catch (Exception ex)
                {
                    Debug.Print("Failed to decode clipboard image data. [{1}]: {0}", ex.Message, ex.GetType().Name);
                    return null;
                }
            }
            return null;
        }

        private byte[]? Decrypt(byte[] data)
        {
            if (!Ready)
            {
                throw new InvalidOperationException("Key has not been set yet");
            }
            using var enc = new AesGcm(key!);
            var nonce = data
                .Take(AesGcm.NonceByteSizes.MaxSize)
                .ToArray();
            var tag = data
                .Skip(nonce.Length)
                .Take(AesGcm.TagByteSizes.MaxSize)
                .ToArray();
            var ciphertext = data
                .Skip(nonce.Length + tag.Length)
                .ToArray();
            var plaintext = new byte[ciphertext.Length];
            Debug.Print("Decrypting {0} bytes", plaintext.Length);
            Tools.DebugDumpBytes(nonce);
            Tools.DebugDumpBytes(tag);
            Tools.DebugDumpBytes(ciphertext);
            try
            {
                enc.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            catch (Exception ex)
            {
                Debug.Print("Decryption failed. [{1}]: {0}", ex.Message, ex.GetType().Name);
                return null;
            }
            Debug.Print("Decryption success. Got {0} bytes", plaintext.Length);
            Tools.DebugDumpBytes(plaintext);
            return plaintext;
        }

        private byte[] Encrypt(byte[] plaintext)
        {
            if (!Ready)
            {
                throw new InvalidOperationException("Key has not been set yet");
            }
            using var enc = new AesGcm(key!);
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];
            var ciphertext = new byte[plaintext.Length];
            RandomNumberGenerator.Fill(nonce);
            try
            {
                enc.Encrypt(nonce, plaintext, ciphertext, tag);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to encrypt {plaintext.Length} bytes of data. See inner exception for details", ex);
            }
            Debug.Print("Encrypted {0} bytes", plaintext.Length);
            Tools.DebugDumpBytes(plaintext);
            Tools.DebugDumpBytes(nonce);
            Tools.DebugDumpBytes(tag);
            Tools.DebugDumpBytes(ciphertext);
            return nonce
                .Concat(tag)
                .Concat(ciphertext)
                .ToArray();
        }

        public void Dispose()
        {
            key = null;
            RemoveClipboardFormatListener(hWnd);
            Application.RemoveMessageFilter(this);
            GC.SuppressFinalize(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {

                Debug.Print("Got WM_CLIPBOARDUPDATE");
                if (Ready)
                {
                    var result = CheckClipboard();
                    if (result != null)
                    {
                        //Do not process our own data
                        if (result.SenderId != Id)
                        {
                            Debug.Print("Processing message of type {0} from {1:X8}",
                                result.MessageType, result.SenderId);
                            switch (result.MessageType)
                            {
                                case MessageType.FileData:
                                    AckTransfer(result);
                                    ClipboardData(this, result);
                                    break;
                                case MessageType.Ack:
                                    SendNextPart(result);
                                    break;
                                case MessageType.Ping:
                                    Pong(result);
                                    break;
                                case MessageType.Pong:
                                    break;
                                default:
                                    throw new NotImplementedException($"Unknown type: {result.MessageType}");
                            }
                        }
                        else
                        {
                            Debug.Print("Ignoring loopback message of type {0}", result.MessageType);
                        }
                    }
                }
            }
            return m.Msg == WM_CLIPBOARDUPDATE;
        }
    }
}
