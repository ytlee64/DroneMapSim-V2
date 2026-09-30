using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToCode
{
    public partial class MakeCsCode
    {
        public static void Constructor(string namespaceName, ExcelSheetParser packet) 
        {
            string dirPath = "./csharp";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            List<ExcelRow> items = packet.GetItems();
            using (StreamWriter csFile = new StreamWriter($"./csharp/{packet.Name}PktConstructor.cs"))
            {
                OpenHeader(csFile, namespaceName, packet.Name);
                
                csFile.WriteLine($"\t\tpublic {packet.Name}Pkt()");
                csFile.WriteLine("\t\t{");
                csFile.WriteLine($"\t\t\tpConvertor = new PacketUtil.PacketConvert{packet.Endian}();");
                csFile.WriteLine($"\t\t\tPacketLength = {packet.TotalLength};");
                csFile.WriteLine($"\t\t\tByteData = new byte[PacketLength];");
                csFile.WriteLine($"\t\t\tName = \"{packet.Name}\";");
                csFile.WriteLine($"\t\t\tItems = new ObservableCollection<PacketUtil.PacketItem>");
                csFile.WriteLine($"\t\t\t{{");
                for (int i = 0; i < items.Count; i++)
                {
                     if(items[i].Name.ToLower().Contains("reserved"))
                        continue;
                    csFile.WriteLine($"\t\t\t\t{items[i].Name},");
                }
                csFile.WriteLine($"\t\t\t}};");

                csFile.WriteLine($"\t\t\tforeach (PacketUtil.PacketItem item in Items)");
                csFile.WriteLine($"\t\t\t\titem.Initialize(this);");
                csFile.WriteLine($"\t\t\tByteDataToItems();");
                
                csFile.WriteLine("\t\t}");
                CloseHeader(csFile);
            }
        }
    }
}
