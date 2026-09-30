using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PacketUtil
{
	partial class BinToCsvVM : ObservableObject
	{

        [ObservableProperty]
        public  ObservableCollection<PacketUtil.Packet> ?_packets;

		public void Init(ObservableCollection<PacketUtil.Packet> ?packets)
		{
			Packets = packets;
		}
		 
	}
}
