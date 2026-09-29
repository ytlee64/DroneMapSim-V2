using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToCode
{
    partial class MakeCsCode
    {
        public static void OpenHeader(StreamWriter f, string namespaceName, string packetName)
        {
            f.WriteLine("using System.Collections.ObjectModel;");
            f.WriteLine("using System.Collections.Generic;");
            f.WriteLine("using System;");
            f.WriteLine("using System.IO;");
            f.WriteLine("using CommunityToolkit.Mvvm.ComponentModel;");
            f.WriteLine($"namespace {namespaceName}");
            f.WriteLine("{");
            f.WriteLine($"\tpublic partial class {packetName}Pkt : PacketUtil.Packet");
            f.WriteLine("\t{");
        }

        public static void CloseHeader(StreamWriter f) 
        {
            f.WriteLine("\t}");
            f.WriteLine("}");
        }

        public static void MakeCsAll(string namespaceName, ExcelSheetParser packet)
        {
            Constructor(namespaceName, packet);
            StringListToItems(namespaceName, packet);
            ItemMember(namespaceName, packet);
            MakeXaml(namespaceName, packet);
        }


    }
}
