using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class GndTruthPkt : PacketUtil.Packet
	{
		public GndTruthPkt()
		{
			pConvertor = new PacketUtil.PacketConvertLittleEndian();
			PacketLength = 96;
			ByteData = new byte[PacketLength];
			Name = "GndTruth";
			Items = new ObservableCollection<PacketUtil.PacketItem>
			{
				Valid,
				FrameID,
				TrackErrorX,
				TrackErrorY,
				TargetPosX,
				TargetPosY,
				TargetSX,
				TargetSY,
				TargetEX,
				TargetEY,
				SlantRange,
				TimeStamp,
				Lat,
				Lon,
				Alt,
			};
			foreach (PacketUtil.PacketItem item in Items)
				item.Initialize(this);
			ByteDataToItems();
		}
	}
}
