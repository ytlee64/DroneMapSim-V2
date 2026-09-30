using System;
using System.Text;

namespace PacketUtil
{
    public partial class PacketItemString : PacketItem
    {
        public PacketItemString(string id, string name, string unit, string type, string format, double lsb, int bytestart, int bytesize, string defvalue, string comment)
            : base(id, name, unit, type, format, lsb, bytestart, bytesize, defvalue, comment)
        {
            
        }

        public override void SetByteData(IPacketConvert pConvertor, byte[] ByteData, string svalue)
        {

            byte[] stringBytes = Encoding.UTF8.GetBytes(svalue);

            // 특정 인덱스(예: 5)에서부터 문자열의 바이트를 복사
            Array.Clear(ByteData, ByteStart, ByteSize);
            Array.Copy(stringBytes, 0, ByteData, ByteStart, stringBytes.Length);
             
        }


        public override string GetValue(IPacketConvert pConvertor, byte[] ByteData)
        {
            string svalue= Encoding.UTF8.GetString(ByteData, ByteStart, ByteSize);
            return svalue;
        }

    }
 
}
