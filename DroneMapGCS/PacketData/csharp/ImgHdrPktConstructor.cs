using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class ImgHdrPkt : PacketUtil.Packet
	{
		public ImgHdrPkt()
		{
			pConvertor = new PacketUtil.PacketConvertLittleEndian();
			PacketLength = 32;
			ByteData = new byte[PacketLength];
			Name = "ImgHdr";
			Items = new ObservableCollection<PacketUtil.PacketItem>
			{
				ImageType,
				FrameId,
				Chksum,
			};
			foreach (PacketUtil.PacketItem item in Items)
				item.Initialize(this);
			ByteDataToItems();
		}
	}
}
