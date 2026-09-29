using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class GndTruthPkt : PacketUtil.Packet
	{
		[ObservableProperty]
		private PacketUtil.PacketItem _valid=new PacketUtil.PacketItemUint
		(
			"",
			"Valid",
			"",
			"uint16_t",
			"D",
			1,
			0,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _frameID=new PacketUtil.PacketItemUint
		(
			"",
			"FrameID",
			"",
			"uint16_t",
			"D",
			1,
			2,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _trackErrorX=new PacketUtil.PacketItemInt
		(
			"",
			"TrackErrorX",
			"",
			"int16_t",
			"F6",
			0.005,
			4,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _trackErrorY=new PacketUtil.PacketItemInt
		(
			"",
			"TrackErrorY",
			"",
			"int16_t",
			"F6",
			0.005,
			6,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetPosX=new PacketUtil.PacketItemUint
		(
			"",
			"TargetPosX",
			"",
			"uint16_t",
			"D",
			1,
			8,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetPosY=new PacketUtil.PacketItemUint
		(
			"",
			"TargetPosY",
			"",
			"uint16_t",
			"D",
			1,
			10,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetSX=new PacketUtil.PacketItemUint
		(
			"",
			"TargetSX",
			"",
			"uint16_t",
			"D",
			1,
			12,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetSY=new PacketUtil.PacketItemUint
		(
			"",
			"TargetSY",
			"",
			"uint16_t",
			"D",
			1,
			14,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetEX=new PacketUtil.PacketItemUint
		(
			"",
			"TargetEX",
			"",
			"uint16_t",
			"D",
			1,
			16,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetEY=new PacketUtil.PacketItemUint
		(
			"",
			"TargetEY",
			"",
			"uint16_t",
			"D",
			1,
			18,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _slantRange=new PacketUtil.PacketItemUint
		(
			"",
			"SlantRange",
			"",
			"uint16_t",
			"F0",
			10,
			20,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _timeStamp=new PacketUtil.PacketItemInt
		(
			"",
			"TimeStamp",
			"",
			"int32_t",
			"D",
			1,
			22,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _lat=new PacketUtil.PacketItemFloat
		(
			"",
			"Lat",
			"",
			"float",
			"F8",
			1,
			26,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _lon=new PacketUtil.PacketItemFloat
		(
			"",
			"Lon",
			"",
			"float",
			"F8",
			1,
			30,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _alt=new PacketUtil.PacketItemFloat
		(
			"",
			"Alt",
			"",
			"float",
			"F8",
			1,
			34,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_0=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_0",
			"",
			"uint16_t",
			"D",
			1,
			38,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_1=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_1",
			"",
			"uint16_t",
			"D",
			1,
			40,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_2=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_2",
			"",
			"uint16_t",
			"D",
			1,
			42,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_3=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_3",
			"",
			"uint16_t",
			"D",
			1,
			44,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_4=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_4",
			"",
			"uint16_t",
			"D",
			1,
			46,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_5=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_5",
			"",
			"uint16_t",
			"D",
			1,
			48,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_6=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_6",
			"",
			"uint16_t",
			"D",
			1,
			50,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_7=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_7",
			"",
			"uint16_t",
			"D",
			1,
			52,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_8=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_8",
			"",
			"uint16_t",
			"D",
			1,
			54,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_9=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_9",
			"",
			"uint16_t",
			"D",
			1,
			56,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_10=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_10",
			"",
			"uint16_t",
			"D",
			1,
			58,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_11=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_11",
			"",
			"uint16_t",
			"D",
			1,
			60,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_12=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_12",
			"",
			"uint16_t",
			"D",
			1,
			62,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_13=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_13",
			"",
			"uint16_t",
			"D",
			1,
			64,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_14=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_14",
			"",
			"uint16_t",
			"D",
			1,
			66,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_15=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_15",
			"",
			"uint16_t",
			"D",
			1,
			68,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_16=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_16",
			"",
			"uint16_t",
			"D",
			1,
			70,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_17=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_17",
			"",
			"uint16_t",
			"D",
			1,
			72,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_18=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_18",
			"",
			"uint16_t",
			"D",
			1,
			74,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_19=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_19",
			"",
			"uint16_t",
			"D",
			1,
			76,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_20=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_20",
			"",
			"uint16_t",
			"D",
			1,
			78,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_21=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_21",
			"",
			"uint16_t",
			"D",
			1,
			80,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_22=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_22",
			"",
			"uint16_t",
			"D",
			1,
			82,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_23=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_23",
			"",
			"uint16_t",
			"D",
			1,
			84,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_24=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_24",
			"",
			"uint16_t",
			"D",
			1,
			86,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_25=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_25",
			"",
			"uint16_t",
			"D",
			1,
			88,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_26=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_26",
			"",
			"uint16_t",
			"D",
			1,
			90,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_27=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_27",
			"",
			"uint16_t",
			"D",
			1,
			92,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_28=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_28",
			"",
			"uint16_t",
			"D",
			1,
			94,
			2,
			"0",
			""
		);

	}
}
