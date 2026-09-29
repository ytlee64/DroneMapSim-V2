using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class IS01Pkt : PacketUtil.Packet
	{
		[ObservableProperty]
		private PacketUtil.PacketItem _length=new PacketUtil.PacketItemUint
		(
			"",
			"Length",
			"",
			"uint16_t",
			"D",
			1,
			0,
			2,
			"0x23",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _seqNum=new PacketUtil.PacketItemUint
		(
			"",
			"SeqNum",
			"",
			"uint16_t",
			"D",
			1,
			2,
			2,
			"0x1",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _cmd=new PacketUtil.PacketItemUint
		(
			"",
			"Cmd",
			"",
			"uint16_t",
			"D",
			1,
			4,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _status=new PacketUtil.PacketItemUint
		(
			"",
			"Status",
			"",
			"uint16_t",
			"D",
			1,
			6,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetSize=new PacketUtil.PacketItemUint
		(
			"",
			"TargetSize",
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
		private PacketUtil.PacketItem _slantRange=new PacketUtil.PacketItemUint
		(
			"",
			"SlantRange",
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
		private PacketUtil.PacketItem _mslLat=new PacketUtil.PacketItemInt
		(
			"",
			"MslLat",
			"deg",
			"int32_t",
			"F10",
			1E-05,
			12,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _mslLon=new PacketUtil.PacketItemInt
		(
			"",
			"MslLon",
			"deg",
			"int32_t",
			"F10",
			1E-05,
			16,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _mslHeight=new PacketUtil.PacketItemInt
		(
			"",
			"MslHeight",
			"m",
			"int32_t",
			"F0",
			10,
			20,
			4,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _mslVelN=new PacketUtil.PacketItemInt
		(
			"",
			"MslVelN",
			"m/s",
			"int16_t",
			"D",
			1,
			24,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _mslVelE=new PacketUtil.PacketItemInt
		(
			"",
			"MslVelE",
			"m/s",
			"int16_t",
			"D",
			1,
			26,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _mslVelD=new PacketUtil.PacketItemInt
		(
			"",
			"MslVelD",
			"m/s",
			"int16_t",
			"D",
			1,
			28,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _roll=new PacketUtil.PacketItemInt
		(
			"",
			"Roll",
			"deg",
			"int16_t",
			"D",
			1,
			30,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _pitch=new PacketUtil.PacketItemInt
		(
			"",
			"Pitch",
			"deg",
			"int16_t",
			"D",
			1,
			32,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _yaw=new PacketUtil.PacketItemInt
		(
			"",
			"Yaw",
			"deg",
			"int16_t",
			"D",
			1,
			34,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _rollRate=new PacketUtil.PacketItemInt
		(
			"",
			"RollRate",
			"",
			"int16_t",
			"F2",
			0.1,
			36,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _pitchRate=new PacketUtil.PacketItemInt
		(
			"",
			"PitchRate",
			"",
			"int16_t",
			"F2",
			0.1,
			38,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _yawRate=new PacketUtil.PacketItemInt
		(
			"",
			"YawRate",
			"",
			"int16_t",
			"F2",
			0.1,
			40,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_0=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_0",
			"",
			"int16_t",
			"D",
			1,
			42,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_1=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_1",
			"",
			"int16_t",
			"D",
			1,
			44,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_2=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_2",
			"",
			"int16_t",
			"D",
			1,
			46,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_3=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_3",
			"",
			"int16_t",
			"D",
			1,
			48,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_4=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_4",
			"",
			"int16_t",
			"D",
			1,
			50,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_5=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_5",
			"",
			"int16_t",
			"D",
			1,
			52,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_6=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_6",
			"",
			"int16_t",
			"D",
			1,
			54,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_7=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_7",
			"",
			"int16_t",
			"D",
			1,
			56,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_8=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_8",
			"",
			"int16_t",
			"D",
			1,
			58,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_9=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_9",
			"",
			"int16_t",
			"D",
			1,
			60,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved1_10=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_10",
			"",
			"int16_t",
			"D",
			1,
			62,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_0=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_0",
			"",
			"int16_t",
			"D",
			1,
			64,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_1=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_1",
			"",
			"int16_t",
			"D",
			1,
			66,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_2=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_2",
			"",
			"int16_t",
			"D",
			1,
			68,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_3=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_3",
			"",
			"int16_t",
			"D",
			1,
			70,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_4=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_4",
			"",
			"int16_t",
			"D",
			1,
			72,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_5=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_5",
			"",
			"int16_t",
			"D",
			1,
			74,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_6=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_6",
			"",
			"int16_t",
			"D",
			1,
			76,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_7=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_7",
			"",
			"int16_t",
			"D",
			1,
			78,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_8=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_8",
			"",
			"int16_t",
			"D",
			1,
			80,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_9=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_9",
			"",
			"int16_t",
			"D",
			1,
			82,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_10=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_10",
			"",
			"int16_t",
			"D",
			1,
			84,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_11=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_11",
			"",
			"int16_t",
			"D",
			1,
			86,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_12=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_12",
			"",
			"int16_t",
			"D",
			1,
			88,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_13=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_13",
			"",
			"int16_t",
			"D",
			1,
			90,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_14=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_14",
			"",
			"int16_t",
			"D",
			1,
			92,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_15=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_15",
			"",
			"int16_t",
			"D",
			1,
			94,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_16=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_16",
			"",
			"int16_t",
			"D",
			1,
			96,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_17=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_17",
			"",
			"int16_t",
			"D",
			1,
			98,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_18=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_18",
			"",
			"int16_t",
			"D",
			1,
			100,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_19=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_19",
			"",
			"int16_t",
			"D",
			1,
			102,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_20=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_20",
			"",
			"int16_t",
			"D",
			1,
			104,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_21=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_21",
			"",
			"int16_t",
			"D",
			1,
			106,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_22=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_22",
			"",
			"int16_t",
			"D",
			1,
			108,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_23=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_23",
			"",
			"int16_t",
			"D",
			1,
			110,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_24=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_24",
			"",
			"int16_t",
			"D",
			1,
			112,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_25=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_25",
			"",
			"int16_t",
			"D",
			1,
			114,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_26=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_26",
			"",
			"int16_t",
			"D",
			1,
			116,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_27=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_27",
			"",
			"int16_t",
			"D",
			1,
			118,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_28=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_28",
			"",
			"int16_t",
			"D",
			1,
			120,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_29=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_29",
			"",
			"int16_t",
			"D",
			1,
			122,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_30=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_30",
			"",
			"int16_t",
			"D",
			1,
			124,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _reserved2_31=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_31",
			"",
			"int16_t",
			"D",
			1,
			126,
			2,
			"0",
			""
		);

	}
}
