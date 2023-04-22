using System.Diagnostics;
using System.Text;

namespace ClipDataEx
{
    public class ClipboardProtocolMessage
    {
        public const int MaxDataSize = 0xFFFFFF;
        public MessageType MessageType { get; set; }
        public uint SenderId { get; set; }
        public byte[]? Data { get; set; }

        public ulong FileSize { get; set; }
        public string? FileName { get; set; }
        public uint SequenceNumber { get; set; }
        public Guid FileId { get; set; }

        public ClipboardProtocolMessage()
        {
        }

        public ClipboardProtocolMessage(byte[] data)
        {
            using var MS = new MemoryStream(data, false);
            using var BR = new BinaryReader(MS);
            MessageType = (MessageType)BR.ReadByte();
            var headerType = (HeaderField)BR.ReadByte();
            while (headerType != HeaderField.None)
            {
                var length = BR.ReadUInt16();
                switch (headerType)
                {
                    case HeaderField.FileName:
                        FileName = Encoding.UTF8.GetString(BR.ReadBytes(length));
                        break;
                    case HeaderField.FileId:
                        if (length != 16)
                        {
                            throw new Exception($"Header {headerType} should be 16 bytes long but is {length}");
                        }
                        FileId = new Guid(BR.ReadBytes(16));
                        break;
                    case HeaderField.FileSize:
                        if (length != sizeof(ulong))
                        {
                            throw new Exception($"Header {headerType} should be {sizeof(ulong)} bytes long but is {length}");
                        }
                        FileSize = BR.ReadUInt64();
                        break;
                    case HeaderField.SequenceNumber:
                        if (length != sizeof(uint))
                        {
                            throw new Exception($"Header {headerType} should be {sizeof(uint)} bytes long but is {length}");
                        }
                        SequenceNumber = BR.ReadUInt32();
                        break;
                    case HeaderField.SenderId:
                        if (length != sizeof(uint))
                        {
                            throw new Exception($"Header {headerType} should be {sizeof(uint)} bytes long but is {length}");
                        }
                        SenderId = BR.ReadUInt32();
                        break;
                    default:
                        Debug.Print("Unknown header type: {0}; Content is {1} bytes", headerType, length);
                        //Discard data
                        BR.ReadBytes(length);
                        break;
                }
                headerType = (HeaderField)BR.ReadByte();
            }
            //Data length is a 24 bit integer
            int datalen = 0;
            for (var i = 0; i < 3; i++)
            {
                datalen <<= 8;
                datalen |= BR.ReadByte();
            }
            if (datalen > 0)
            {
                Data = BR.ReadBytes(datalen);
            }
        }

        public byte[] Serialize()
        {
            if (Data != null && Data.Length > MaxDataSize)
            {
                throw new InvalidOperationException("Data too long for serialization");
            }

            using var MS = new MemoryStream();
            using var BW = new BinaryWriter(MS);
            BW.Write((byte)MessageType);

            //Write headers
            if (SenderId != 0)
            {
                BW.Write((byte)HeaderField.SenderId);
                BW.Write((ushort)sizeof(uint));
                BW.Write(SenderId);
            }
            if (SequenceNumber != 0)
            {
                BW.Write((byte)HeaderField.SequenceNumber);
                BW.Write((ushort)sizeof(uint));
                BW.Write(SequenceNumber);
            }
            if (FileId != Guid.Empty)
            {
                BW.Write((byte)HeaderField.FileId);
                BW.Write((ushort)16);
                BW.Write(FileId.ToByteArray());
            }
            if (FileSize != 0)
            {
                BW.Write((byte)HeaderField.FileSize);
                BW.Write((ushort)sizeof(ulong));
                BW.Write(FileSize);
            }
            if (!string.IsNullOrEmpty(FileName))
            {
                var nameBytes = Encoding.UTF8.GetBytes(FileName);
                BW.Write((byte)HeaderField.FileName);
                BW.Write((ushort)nameBytes.Length);
                BW.Write(nameBytes);
            }
            BW.Write((byte)HeaderField.None);
            if (Data != null && Data.Length > 0)
            {
                BW.Write(new byte[] {
                    (byte)((Data.Length>>16) & 0xFF),
                    (byte)((Data.Length>> 8) & 0xFF),
                    (byte)((Data.Length>> 0) & 0xFF)
                });
                BW.Write(Data);
            }
            else
            {
                BW.Write(new byte[] { 0, 0, 0 });
            }
            BW.Flush();
            return MS.ToArray();
        }
    }

    public enum HeaderField : byte
    {
        /// <summary>
        /// Indicates end of the headers.
        /// This header has no length and no data
        /// </summary>
        None = 0,
        /// <summary>
        /// Id that is unique per file but identical across file segments.
        /// Format: <see cref="Guid" />
        /// </summary>
        FileId = None + 1,
        /// <summary>
        /// Name of the file.
        /// Required when sequence number is zero.
        /// Format: <see cref="string" />
        /// </summary>
        FileName = FileId + 1,
        /// <summary>
        /// Total size of the file.
        /// Required when sequence number is zero.
        /// Format: <see cref="ulong" />
        /// </summary>
        FileSize = FileName + 1,
        /// <summary>
        /// File sequence number.
        /// Starts at zero for the first segment and counts upwards for each file segment
        /// Format: <see cref="int" />
        /// </summary>
        SequenceNumber = FileSize + 1,
        /// <summary>
        /// Id of the sender.
        /// Format: <see cref="uint" />
        /// </summary>
        SenderId = SequenceNumber + 1
    }

    public enum MessageType : byte
    {
        /// <summary>
        /// Clipboard data contains a file or file part
        /// </summary>
        FileData = 0,
        /// <summary>
        /// Clipboard data acknowledges receiving a part
        /// </summary>
        Ack = 1,
        /// <summary>
        /// Clipboard data requests response
        /// </summary>
        Ping = 2,
        /// <summary>
        /// Response to <see cref="Ping"/>
        /// </summary>
        Pong = 3,
        /// <summary>
        /// Message type is not known
        /// </summary>
        Unknown = byte.MaxValue
    }
}
