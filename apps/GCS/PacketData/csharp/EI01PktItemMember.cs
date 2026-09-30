using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class EI01Pkt : PacketUtil.Packet
	{
		[ObservableProperty]
		private PacketUtil.PacketItem _reserved_0=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_0",
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
		private PacketUtil.PacketItem _reserved_1=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_1",
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
		private PacketUtil.PacketItem _reserved_2=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_2",
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
		private PacketUtil.PacketItem _reserved_3=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_3",
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
		private PacketUtil.PacketItem _reserved_4=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_4",
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
		private PacketUtil.PacketItem _reserved_5=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_5",
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
		private PacketUtil.PacketItem _reserved_6=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_6",
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
		private PacketUtil.PacketItem _reserved_7=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_7",
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
		private PacketUtil.PacketItem _reserved_8=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_8",
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
		private PacketUtil.PacketItem _reserved_9=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_9",
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
		private PacketUtil.PacketItem _reserved_10=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_10",
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
		private PacketUtil.PacketItem _reserved_11=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_11",
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
		private PacketUtil.PacketItem _reserved_12=new PacketUtil.PacketItemUint
		(
			"",
			"Reserved_12",
			"",
			"uint16_t",
			"D",
			1,
			24,
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
			26,
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
			28,
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
			30,
			2,
			"0",
			""
		);

	}
}
