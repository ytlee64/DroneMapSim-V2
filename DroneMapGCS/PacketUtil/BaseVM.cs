using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketUtil
{
    public class BaseVM : ObservableObject
    {
        public void OnRowSelect(object selectedItem)
        {
            PacketItem item = (PacketItem)selectedItem;

            DlgWord dlg = new(item,false);
            Console.WriteLine($"Column clicked: {item.Name} {item.Value} {item.BitList.Length}");
            if (item.BitList.Length>0)
            {
                dlg.Owner = System.Windows.Application.Current.MainWindow;
                dlg.ShowDialog();
            }
        }
    }
}
