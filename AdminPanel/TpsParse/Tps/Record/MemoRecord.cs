using TpsParse.Util;

namespace TpsParse.Tps.Record
{
    public class MemoRecord : TpsRecord
    {
        public MemoRecord(byte[] data, byte[] header)
            : base(data, header, true)
        {
            OwningRecord = BitUtil.ToInt32(header, 5, false);
            MemoIndex = header[9];
            SequenceNumber = BitUtil.ToInt16(header, 10, false);
        }

        public int OwningRecord { get; }
        public byte MemoIndex { get; }
        public short SequenceNumber { get; }

        public override T Accept<T>(ITpsFileVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}