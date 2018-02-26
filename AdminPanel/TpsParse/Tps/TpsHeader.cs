using System.IO;
using System.Text;
using TpsParse.Util;

namespace TpsParse.Tps
{
    /// <summary>
    ///     TpsHeader represents the first 0x200 bytes of a TPS file.
    ///     Aside from the 'tOpS' identifier, it holds an array of page addresses and other meta information.
    /// </summary>
    public class TpsHeader
    {
        public TpsHeader(Stream inputStream)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[0x200];
            inputStream.Read(buffer, 0, 0x200);

            Addr = BitUtil.ToInt32(buffer, 0);
            HdrSize = BitUtil.ToInt16(buffer, 4);
            FileLength1 = BitUtil.ToInt32(buffer, 6);
            FileLength2 = BitUtil.ToInt32(buffer, 10);
            TopSpeed = Encoding.ASCII.GetString(buffer, 14, 4);
            Zeros = BitUtil.ToInt16(buffer, 18);
            LastIssuedRow = BitUtil.ToInt32(buffer, 20);
            Changes = BitUtil.ToInt32(buffer, 24);
            ManagementPageRef = BitUtil.ToInt32(buffer, 28);

            PageStart = GetInt32Array(buffer, 32, 60);
            PageEnd = GetInt32Array(buffer, 272, 60);
        }

        public int Addr { get; }
        public short HdrSize { get; }
        public int FileLength1 { get; }
        public int FileLength2 { get; }
        public string TopSpeed { get; }
        public short Zeros { get; }
        public int LastIssuedRow { get; }
        public int Changes { get; }
        public int ManagementPageRef { get; }
        public int[] PageStart { get; }
        public int[] PageEnd { get; }

        private static int[] GetInt32Array(byte[] buffer, int startIndex, int length)
        {
            var result = new int[length];
            for (var i = 0; i < length; i++)
                result[i] = (BitUtil.ToInt32(buffer, startIndex + i * 4) << 8) + 0x200;
            return result;
        }

        public bool IsTopSpeedFile()
        {
            return TopSpeed == "tOpS";
        }
    }
}