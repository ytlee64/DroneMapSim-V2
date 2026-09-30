using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToCode
{
    public partial class MakeCsCode
    {
        public static void StringListToItems(string namespaceName, ExcelSheetParser packet)
        {
            List<ExcelRow> items = packet.GetItems();
            using (StreamWriter csFile = new StreamWriter($"./csharp/{packet.Name}PktStringListToItems.cs"))
            {
                OpenHeader(csFile, namespaceName, packet.Name);
                csFile.WriteLine("\t\tpublic override void StringListToItems(string []lines)");
                csFile.WriteLine("\t\t{");
                csFile.WriteLine("\t\t\tforeach (string line in lines)");
                csFile.WriteLine("\t\t\t{");
                csFile.WriteLine("\t\t\t\tList<string> words = PacketUtil.Tokenize.GetWords(line);");
                foreach (var item in items)
                {
                    csFile.WriteLine($"\t\t\t\tif (words[0]==\"{item.Name}\") {item.Name}.Value  = words[1];");
                }
                csFile.WriteLine("\t\t\t}");
                csFile.WriteLine("\t\t}");
                CloseHeader(csFile);
            }
        }
    }

}