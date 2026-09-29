using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelToCode
{
    static class MakeVm
    {

        public static void MakeCs(string namespaceName, string vmname, List<string> packets)
        {

            using (StreamWriter f = new StreamWriter($"./csharp/{vmname}.cs"))
            {

                f.WriteLine("using CommunityToolkit.Mvvm.ComponentModel;");
                f.WriteLine("using CommunityToolkit.Mvvm.Messaging;");
                f.WriteLine("using CommunityToolkit.Mvvm.Messaging.Messages;");
                f.WriteLine("using System.Collections.Generic;");

                f.WriteLine("");
                f.WriteLine($"namespace {namespaceName}");
                f.WriteLine("{");
                f.WriteLine($"\tpartial class {vmname} : ObservableObject");
                f.WriteLine("\t{");
                foreach (string packetname in packets)
                {
                    f.WriteLine($"\t\t[ObservableProperty]");
                    f.WriteLine($"\t\tpublic {packetname}Pkt _{char.ToLower(packetname[0]) + packetname.Substring(1)} = new();");
                    f.WriteLine();
                }

                f.WriteLine("\t\tprivate ObservableCollection<PacketUtil.Packet>? packets;");
                f.WriteLine();

                f.WriteLine("\t\tpublic void Init()");
                f.WriteLine("\t\t{");
                f.WriteLine("\t\t\tpackets = new ObservableCollection<PacketUtil.Packet>");
                f.WriteLine("\t\t\t{");

                foreach (string packetname in packets)
                {
                    f.WriteLine($"\t\t\t\t {packetname},");
                }

                f.WriteLine("\t\t\t};");
                f.WriteLine("\t\t}");

                f.WriteLine("\t}");
                f.WriteLine("}");

            }
        }
    }
}