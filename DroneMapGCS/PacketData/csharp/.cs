using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;

namespace DroneMapGCS
{
	partial class  : ObservableObject
	{
		[ObservableProperty]
		public IS01Pkt _iS01 = new();

		[ObservableProperty]
		public SI01Pkt _sI01 = new();

		[ObservableProperty]
		public IE01Pkt _iE01 = new();

		[ObservableProperty]
		public EI01Pkt _eI01 = new();

		[ObservableProperty]
		public GndTruthPkt _gndTruth = new();

		[ObservableProperty]
		public ImgHdrPkt _imgHdr = new();

		private ObservableCollection<PacketUtil.Packet>? packets;

		public void Init()
		{
			packets = new ObservableCollection<PacketUtil.Packet>
			{
				 IS01,
				 SI01,
				 IE01,
				 EI01,
				 GndTruth,
				 ImgHdr,
			};
		}
	}
}
