using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ClipDataEx
{
    public static class ImageEncoder
    {
        public static int ByteCount(Size resolution)
        {
            return Math.Max(0, (resolution.Width * resolution.Height * 3) - sizeof(int));
        }

        public static Size ImageSize(int byteCount)
        {
            var pixelCount = (int)Math.Ceiling((byteCount + sizeof(int)) / 3.0);
            var size = (int)Math.Ceiling(Math.Sqrt(pixelCount));
            //Image width must be a multiple of 4 because of how DIB format works
            if (size % 4 != 0)
            {
                size += 4 - (size % 4);
            }
            return new Size(size, size);
        }

        public static Image Encode(byte[] rawData, int offset, int count, Size resolution, out int encoded)
        {
            if (rawData is null)
            {
                throw new ArgumentNullException(nameof(rawData));
            }
            if (rawData.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rawData));
            }
            if (offset < 0 || offset >= rawData.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count + offset > rawData.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (resolution.IsEmpty || resolution.Width == 0 || resolution.Height == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution));
            }

            var realCount = Math.Min(count, ByteCount(resolution));
            var ret = new Bitmap(resolution.Width, resolution.Height);
            var region = ret.LockBits(new Rectangle(0, 0, resolution.Width, resolution.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            if (region.Stride != resolution.Width * 3)
            {
                throw new Exception($"Stride length is {region.Stride} but should be {resolution.Width * 3}");
            }

            Marshal.Copy(Tools.GetBytes(realCount), 0, region.Scan0, sizeof(int));
            Marshal.Copy(rawData, offset, region.Scan0 + sizeof(int), realCount);

            ret.UnlockBits(region);
            encoded = realCount;
            return ret;
        }

        public static Image Encode(byte[] rawData, int offset, int count, out int encoded)
        {
            return Encode(rawData, offset, rawData.Length, ImageSize(count), out encoded);
        }

        public static Image Encode(byte[] rawData, out int encoded)
        {
            return Encode(rawData, 0, rawData.Length, out encoded);
        }

        public static byte[] Decode(Image data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Width % 4 != 0)
            {
                throw new FormatException("Image width not a multiple of 4");
            }

            Bitmap b = (Bitmap)data;
            var raw = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] count = new byte[sizeof(int)];
            Marshal.Copy(raw.Scan0, count, 0, sizeof(int));
            var decodedCount = Tools.ToInt32(count);
            if (decodedCount > (b.Width * b.Height * 3) - sizeof(int))
            {
                throw new FormatException($"{decodedCount} is larger than maximum possible count for this image");
            }
            byte[] ret = new byte[decodedCount];
            Marshal.Copy(raw.Scan0 + sizeof(int), ret, 0, ret.Length);
            b.UnlockBits(raw);
            return ret;
        }
    }
}
