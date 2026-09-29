using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class IS01Pkt : PacketUtil.Packet
	{
		public IS01Pkt()
		{
			pConvertor = new PacketUtil.PacketConvertLittleEndian();
			PacketLength = 128;
			ByteData = new byte[PacketLength];
			Name = "IS01";
			Items = new ObservableCollection<PacketUtil.PacketItem>
			{
				Length,
				SeqNum,
				Cmd,
				Status,
				TargetSize,
				SlantRange,
				MslLat,
				MslLon,
				MslHeight,
				MslVelN,
				MslVelE,
				MslVelD,
				Roll,
				Pitch,
				Yaw,
				RollRate,
				PitchRate,
				YawRate,
			};
			foreach (PacketUtil.PacketItem item in Items)
				item.Initialize(this);
			ByteDataToItems();
		}
	}
}
