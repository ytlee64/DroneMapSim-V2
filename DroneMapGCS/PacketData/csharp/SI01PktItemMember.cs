using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class SI01Pkt : PacketUtil.Packet
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
			"0x32",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _seqNumRtn=new PacketUtil.PacketItemUint
		(
			"",
			"SeqNumRtn",
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
		private PacketUtil.PacketItem _cmdRtn=new PacketUtil.PacketItemUint
		(
			"",
			"CmdRtn",
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
		private PacketUtil.PacketItem _statusRtn=new PacketUtil.PacketItemUint
		(
			"",
			"StatusRtn",
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
		private PacketUtil.PacketItem _targetSizeRtn=new PacketUtil.PacketItemUint
		(
			"",
			"TargetSizeRtn",
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
		private PacketUtil.PacketItem _slantRangeRtn=new PacketUtil.PacketItemUint
		(
			"",
			"SlantRangeRtn",
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
		private PacketUtil.PacketItem _valid=new PacketUtil.PacketItemUint
		(
			"",
			"Valid",
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
		private PacketUtil.PacketItem _operTime=new PacketUtil.PacketItemUint
		(
			"",
			"OperTime",
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
		private PacketUtil.PacketItem _bit=new PacketUtil.PacketItemUint
		(
			"",
			"Bit",
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
		private PacketUtil.PacketItem _pBit=new PacketUtil.PacketItemUint
		(
			"",
			"PBit",
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
		private PacketUtil.PacketItem _iBit=new PacketUtil.PacketItemUint
		(
			"",
			"IBit",
			"",
			"uint16_t",
			"D",
			1,
			20,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _cBit=new PacketUtil.PacketItemUint
		(
			"",
			"CBit",
			"",
			"uint16_t",
			"D",
			1,
			22,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _eomFpaTemp=new PacketUtil.PacketItemInt
		(
			"",
			"EomFpaTemp",
			"",
			"int16_t",
			"D",
			1,
			24,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _eomTemp=new PacketUtil.PacketItemInt
		(
			"",
			"EomTemp",
			"",
			"int16_t",
			"D",
			1,
			26,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _ipmCpuTemp=new PacketUtil.PacketItemInt
		(
			"",
			"IpmCpuTemp",
			"",
			"int16_t",
			"D",
			1,
			28,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _trackStatus=new PacketUtil.PacketItemInt
		(
			"",
			"TrackStatus",
			"",
			"int16_t",
			"D",
			1,
			30,
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
			"D",
			1,
			32,
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
			"D",
			1,
			34,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetPosX=new PacketUtil.PacketItemInt
		(
			"",
			"TargetPosX",
			"",
			"int16_t",
			"D",
			1,
			36,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _targetPosY=new PacketUtil.PacketItemInt
		(
			"",
			"TargetPosY",
			"",
			"int16_t",
			"D",
			1,
			38,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _trackBIT=new PacketUtil.PacketItemInt
		(
			"",
			"TrackBIT",
			"",
			"int16_t",
			"D",
			1,
			40,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _trackConfidence=new PacketUtil.PacketItemInt
		(
			"",
			"TrackConfidence",
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
		private PacketUtil.PacketItem _targetSX=new PacketUtil.PacketItemInt
		(
			"",
			"TargetSX",
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
		private PacketUtil.PacketItem _targetSY=new PacketUtil.PacketItemInt
		(
			"",
			"TargetSY",
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
		private PacketUtil.PacketItem _targetEX=new PacketUtil.PacketItemInt
		(
			"",
			"TargetEX",
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
		private PacketUtil.PacketItem _targetEY=new PacketUtil.PacketItemInt
		(
			"",
			"TargetEY",
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
		private PacketUtil.PacketItem _fid=new PacketUtil.PacketItemInt
		(
			"",
			"Fid",
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
		private PacketUtil.PacketItem _pid=new PacketUtil.PacketItemUint
		(
			"",
			"Pid",
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
		private PacketUtil.PacketItem _latency=new PacketUtil.PacketItemUint
		(
			"",
			"Latency",
			"",
			"uint16_t",
			"F4",
			0.01,
			56,
			2,
			"0",
			""
		);

		[ObservableProperty]
		private PacketUtil.PacketItem _serialNumber=new PacketUtil.PacketItemUint
		(
			"",
			"SerialNumber",
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
		private PacketUtil.PacketItem _ipmAlgVer=new PacketUtil.PacketItemUint
		(
			"",
			"IpmAlgVer",
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
		private PacketUtil.PacketItem _ipmOpsVer=new PacketUtil.PacketItemInt
		(
			"",
			"IpmOpsVer",
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
		private PacketUtil.PacketItem _ipmFpgaVer=new PacketUtil.PacketItemInt
		(
			"",
			"IpmFpgaVer",
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
		private PacketUtil.PacketItem _eomOpsVer=new PacketUtil.PacketItemInt
		(
			"",
			"EomOpsVer",
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
		private PacketUtil.PacketItem _eomFpgaVer=new PacketUtil.PacketItemInt
		(
			"",
			"EomFpgaVer",
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
		private PacketUtil.PacketItem _reserved1_0=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_0",
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
		private PacketUtil.PacketItem _reserved1_1=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_1",
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
		private PacketUtil.PacketItem _reserved1_2=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_2",
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
		private PacketUtil.PacketItem _reserved1_3=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_3",
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
		private PacketUtil.PacketItem _reserved1_4=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_4",
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
		private PacketUtil.PacketItem _reserved1_5=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_5",
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
		private PacketUtil.PacketItem _reserved1_6=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_6",
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
		private PacketUtil.PacketItem _reserved1_7=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_7",
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
		private PacketUtil.PacketItem _reserved1_8=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_8",
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
		private PacketUtil.PacketItem _reserved1_9=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_9",
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
		private PacketUtil.PacketItem _reserved1_10=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_10",
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
		private PacketUtil.PacketItem _reserved1_11=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_11",
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
		private PacketUtil.PacketItem _reserved1_12=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved1_12",
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
		private PacketUtil.PacketItem _reserved2_0=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_0",
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
		private PacketUtil.PacketItem _reserved2_1=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_1",
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
		private PacketUtil.PacketItem _reserved2_2=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_2",
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
		private PacketUtil.PacketItem _reserved2_3=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_3",
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
		private PacketUtil.PacketItem _reserved2_4=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_4",
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
		private PacketUtil.PacketItem _reserved2_5=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_5",
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
		private PacketUtil.PacketItem _reserved2_6=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_6",
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
		private PacketUtil.PacketItem _reserved2_7=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_7",
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
		private PacketUtil.PacketItem _reserved2_8=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_8",
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
		private PacketUtil.PacketItem _reserved2_9=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_9",
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
		private PacketUtil.PacketItem _reserved2_10=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_10",
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
		private PacketUtil.PacketItem _reserved2_11=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_11",
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
		private PacketUtil.PacketItem _reserved2_12=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_12",
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
		private PacketUtil.PacketItem _reserved2_13=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_13",
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
		private PacketUtil.PacketItem _reserved2_14=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_14",
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
		private PacketUtil.PacketItem _reserved2_15=new PacketUtil.PacketItemInt
		(
			"",
			"Reserved2_15",
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
