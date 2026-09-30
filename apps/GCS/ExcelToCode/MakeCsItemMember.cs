using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ExcelToCode
{
    public partial class MakeCsCode
    {
 

        public static void ItemMember(string namespaceName, ExcelSheetParser packet)
        {
            List<ExcelRow> items = packet.GetItems();
            using (StreamWriter csFile = new StreamWriter($"./csharp/{packet.Name}PktItemMember.cs"))
            {
                OpenHeader(csFile, namespaceName, packet.Name);

                foreach (var item in items)
                {
                     
                    string lowerName = "_" + char.ToLower(item.Name[0]) + item.Name.Substring(1);
                    csFile.WriteLine($"\t\t[ObservableProperty]");
                    if (item.Type == "string")
                        csFile.WriteLine($"\t\tprivate PacketUtil.PacketItem {lowerName}=new PacketUtil.PacketItemString");
                    else if (item.Type.Contains("uint"))
                        csFile.WriteLine($"\t\tprivate PacketUtil.PacketItem {lowerName}=new PacketUtil.PacketItemUint");
                    else if (item.Type.Contains("int"))
                        csFile.WriteLine($"\t\tprivate PacketUtil.PacketItem {lowerName}=new PacketUtil.PacketItemInt");
                    else if (item.Type.Contains("float"))
                        csFile.WriteLine($"\t\tprivate PacketUtil.PacketItem {lowerName}=new PacketUtil.PacketItemFloat");
                    else
                        throw new NotImplementedException();
                    csFile.WriteLine($"\t\t(");
                    csFile.WriteLine($"\t\t\t\"\",");  // id
                    csFile.WriteLine($"\t\t\t\"{item.Name}\",");
                    csFile.WriteLine($"\t\t\t\"{item.Unit}\",");
                    csFile.WriteLine($"\t\t\t\"{item.Type}\",");
                    csFile.WriteLine($"\t\t\t\"{item.Format}\",");
                    csFile.WriteLine($"\t\t\t{item.Lsb},");
                    csFile.WriteLine($"\t\t\t{item.ByteStart},");
                    csFile.WriteLine($"\t\t\t{item.Size},");
                    csFile.WriteLine($"\t\t\t\"{item.DefaultValue}\",");
                    csFile.WriteLine($"\t\t\t\"{item.Comment}\"");
                    csFile.WriteLine($"\t\t);\n");
 
                }
                CloseHeader(csFile);
            }
        }

    }

}