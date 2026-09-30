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
    public partial class PacketItemInt : PacketItem
    {
        public PacketItemInt(string id, string name, string unit, string type, string format, double lsb, int bytestart, int bytesize, string defvalue, string comment)
            : base(id, name, unit, type, format, lsb, bytestart, bytesize, defvalue, comment)
        {

        }

        public override void SetByteData(IPacketConvert pConvertor, byte[] ByteData,string svalue)
        {
            pConvertor.SetSigned(ByteData, ByteStart, ByteSize, Lsb, Format, svalue);
        }

        public override string GetValue(IPacketConvert pConvertor, byte[] ByteData)
        {
            string svalue;
            if (Lsb == 1)
            {
                long intvalue = pConvertor.GetSigned(ByteData, ByteStart, ByteSize);
                dvalue = intvalue;
                if (Format.Contains("X"))
                {
                    if (Parent is not null)
                        svalue = Parent.pConvertor.GetHexString(Parent.GetByteData(), ByteStart, ByteSize);
                    else
                        svalue = "0x";
                }
                else
                    svalue = intvalue.ToString(Format);

                if (_symboldict.ContainsKey((ulong)intvalue))
                    Symbol = $"{_symboldict[(ulong)intvalue]}({svalue})";
                else
                    Symbol = $"({svalue})";
            }
            else
            {
                dvalue = pConvertor.GetSigned(ByteData, ByteStart, ByteSize) * Lsb;
                svalue = dvalue.ToString(Format);
            }
            return svalue;
 
        }

    }

}
