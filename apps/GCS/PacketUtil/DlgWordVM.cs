using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PacketUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

namespace PacketUtil
{
    partial class DlgWordVM : ObservableObject
    {

        [ObservableProperty]
        public PacketItem _wordItem;

        [ObservableProperty]
        public ObservableCollection<PacketItem> _bitItems;

        public DlgWordVM(PacketItem item)
        {
            WordItem = item;
            BitItems = new(); 
            UInt64 value = 0;
            try
            {
                value = UInt64.Parse(WordItem.Value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            }catch (FormatException)
            {
                value = 0;
            }
            
            for (int i = 0; i < item.BitList.Length; i++)
            {
                if (item.BitList[i] != "_")
                {
                    PacketItemUint bitItem = new PacketItemUint((i + 1).ToString(), item.BitList[i], "", "bit", "D", 1.0, 0, 0, "0", "");
                    bitItem.Value=(((UInt64)(1 <<i)&value)>>i).ToString();
                    BitItems.Add(bitItem);
                }
            }
             
        }

        public void Update()
        {
            UInt64 value = 0;
            for (int i = 0; i < BitItems.Count; i++)
            {
                int bits = int.Parse(BitItems[i].Id)-1;
                value |= UInt64.Parse(BitItems[i].Value)<< bits;
            }
            if(WordItem is not null)
                WordItem.Value = "0x" + value.ToString(WordItem.Format);            
        }
    }
}
   