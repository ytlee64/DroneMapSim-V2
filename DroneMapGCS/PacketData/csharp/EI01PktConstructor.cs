using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class EI01Pkt : PacketUtil.Packet
	{
		public EI01Pkt()
		{
			pConvertor = new PacketUtil.PacketConvertLittleEndian();
			PacketLength = 32;
			ByteData = new byte[PacketLength];
			Name = "EI01";
			Items = new ObservableCollection<PacketUtil.PacketItem>
			{
			};
			foreach (PacketUtil.PacketItem item in Items)
				item.Initialize(this);
			ByteDataToItems();
		}
	}
}
