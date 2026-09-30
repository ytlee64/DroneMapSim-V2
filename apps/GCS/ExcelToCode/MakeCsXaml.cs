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

        public static void MakeXaml(string namespaceName, ExcelSheetParser packet)
        {
            List<ExcelRow> items = packet.GetItems();
            using (StreamWriter xaml = new StreamWriter($"./csharp/{packet.Name}Packet.xaml", false, Encoding.UTF8))
            {
                foreach (var item in items)
                {
                    xaml.Write($"<uc:ItemCtrl Grid.Row=\"0\" ItemName=\"{item.KorName}\" ");
                    xaml.WriteLine($"ItemValue=\"{{Binding {packet.Name}.{item.Name}}}\" Height=\"Auto\"/>");
                    xaml.Write($"<uc:BitCtrl Grid.Column=\"0\" ItemName=\"{item.KorName}\" ");
                    xaml.WriteLine($"{{Binding {packet.Name}.{item.Name}}}\" Height=\"Auto\"/>");
                    xaml.Write($"<TextBox Grid.Row=\"0\" Grid.Column=\"0\" Text=\"{{Binding ");
                    xaml.WriteLine($"{packet.Name}.{item.Name}}}\" Width=\"Auto\"/>");
                }
            }
        }
    }

}