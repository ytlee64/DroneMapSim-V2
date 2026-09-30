using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PacketUtil
{
    public partial class PacketItemFloat: PacketItem
    {
        public PacketItemFloat(string id, string name, string unit, string type, string format, double lsb, int bytestart, int bytesize, string defvalue, string comment)
            : base(id, name, unit, type, format, lsb, bytestart, bytesize, defvalue, comment)
        {
            
        }

        public override void SetByteData(IPacketConvert pConvertor, byte[] ByteData, string svalue)
        {
            if (ByteSize == 4)
                pConvertor.SetFloat(ByteData, ByteStart, svalue);
            else if (ByteSize == 8)
                pConvertor.SetDouble(ByteData, ByteStart, svalue);
        }

        public override string GetValue(IPacketConvert pConvertor, byte[] ByteData)
        {
            if (ByteSize == 4)
                dvalue = pConvertor.GetFloat(ByteData, ByteStart);
            else if (ByteSize == 8)
                dvalue = pConvertor.GetDouble(ByteData, ByteStart);
            return dvalue.ToString(Format);
        }

    }
 
}
