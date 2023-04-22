using System.Diagnostics;
using System.Text;

namespace ClipDataEx
{
    public static class Tools
    {
        public static TextWriter DebugWriter { get; set; } = TextWriter.Null;
        public static byte[] GetBytes(ushort x)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.GetBytes(x).Reverse().ToArray();
            }
            return BitConverter.GetBytes(x);
        }

        public static byte[] GetBytes(int x)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.GetBytes(x).Reverse().ToArray();
            }
            return BitConverter.GetBytes(x);
        }

        public static byte[] GetBytes(uint x)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.GetBytes(x).Reverse().ToArray();
            }
            return BitConverter.GetBytes(x);
        }

        public static byte[] GetBytes(ulong x)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.GetBytes(x).Reverse().ToArray();
            }
            return BitConverter.GetBytes(x);
        }

        public static int ToInt32(byte[] x, int offset = 0)
        {
            var chunk = x.Skip(offset).Take(sizeof(int));
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt32(chunk.Reverse().ToArray(), 0);
            }
            return BitConverter.ToInt32(chunk.ToArray(), 0);
        }

        public static ulong ToUInt64(byte[] x, int offset = 0)
        {
            var chunk = x.Skip(offset).Take(sizeof(ulong));
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt64(chunk.Reverse().ToArray(), 0);
            }
            return BitConverter.ToUInt64(chunk.ToArray(), 0);
        }

        public static string FormatSize(double size)
        {
            string[] sizes = "Bytes,KB,MB,GB,TB".Split(',');
            int index = 0;
            while (size >= 1024 && index + 1 < sizes.Length)
            {
                ++index;
                size /= 1024.0;
            }
            return $"{size:#.#} {sizes[index]}";
        }

        [Conditional("DEBUG")]
        public static void DebugDumpBytes(byte[] data)
        {
            DebugWriter.WriteLine("Dumping {0} bytes", data?.Length ?? 0);
            if (data == null)
            {
                return;
            }
            const int width = 16;
            for (var i = 0; i < data.Length; i += width)
            {
                var str = "";
                var hex = string.Join(" ", data.Skip(i).Take(width).Select(m => m.ToString("X2")));
                foreach (var b in data.Skip(i).Take(width))
                {
                    //Latin1 has two unprintable ranges
                    if (b < 0x20 || (b > 0x7F && b < 0xA0))
                    {
                        str += '.';
                    }
                    else
                    {
                        str += Encoding.Latin1.GetString(new byte[] { b });
                    }
                }
                DebugWriter.WriteLine("{0:X8}: {1,-48} {2}", i, hex, str);
            }
        }
    }
}
