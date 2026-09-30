using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class SI01Pkt : PacketUtil.Packet
	{
		public SI01Pkt()
		{
			pConvertor = new PacketUtil.PacketConvertLittleEndian();
			PacketLength = 128;
			ByteData = new byte[PacketLength];
			Name = "SI01";
			Items = new ObservableCollection<PacketUtil.PacketItem>
			{
				Length,
				SeqNumRtn,
				CmdRtn,
				StatusRtn,
				TargetSizeRtn,
				SlantRangeRtn,
				Valid,
				OperTime,
				Bit,
				PBit,
				IBit,
				CBit,
				EomFpaTemp,
				EomTemp,
				IpmCpuTemp,
				TrackStatus,
				TrackErrorX,
				TrackErrorY,
				TargetPosX,
				TargetPosY,
				TrackBIT,
				TrackConfidence,
				TargetSX,
				TargetSY,
				TargetEX,
				TargetEY,
				Fid,
				Pid,
				Latency,
				SerialNumber,
				IpmAlgVer,
				IpmOpsVer,
				IpmFpgaVer,
				EomOpsVer,
				EomFpgaVer,
			};
			foreach (PacketUtil.PacketItem item in Items)
				item.Initialize(this);
			ByteDataToItems();
		}
	}
}
