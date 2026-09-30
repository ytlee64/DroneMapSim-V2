using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class GndTruthPkt : PacketUtil.Packet
	{
		public override void StringListToItems(string []lines)
		{
			foreach (string line in lines)
			{
				List<string> words = PacketUtil.Tokenize.GetWords(line);
				if (words[0]=="Valid") Valid.Value  = words[1];
				if (words[0]=="FrameID") FrameID.Value  = words[1];
				if (words[0]=="TrackErrorX") TrackErrorX.Value  = words[1];
				if (words[0]=="TrackErrorY") TrackErrorY.Value  = words[1];
				if (words[0]=="TargetPosX") TargetPosX.Value  = words[1];
				if (words[0]=="TargetPosY") TargetPosY.Value  = words[1];
				if (words[0]=="TargetSX") TargetSX.Value  = words[1];
				if (words[0]=="TargetSY") TargetSY.Value  = words[1];
				if (words[0]=="TargetEX") TargetEX.Value  = words[1];
				if (words[0]=="TargetEY") TargetEY.Value  = words[1];
				if (words[0]=="SlantRange") SlantRange.Value  = words[1];
				if (words[0]=="TimeStamp") TimeStamp.Value  = words[1];
				if (words[0]=="Lat") Lat.Value  = words[1];
				if (words[0]=="Lon") Lon.Value  = words[1];
				if (words[0]=="Alt") Alt.Value  = words[1];
				if (words[0]=="Reserved_0") Reserved_0.Value  = words[1];
				if (words[0]=="Reserved_1") Reserved_1.Value  = words[1];
				if (words[0]=="Reserved_2") Reserved_2.Value  = words[1];
				if (words[0]=="Reserved_3") Reserved_3.Value  = words[1];
				if (words[0]=="Reserved_4") Reserved_4.Value  = words[1];
				if (words[0]=="Reserved_5") Reserved_5.Value  = words[1];
				if (words[0]=="Reserved_6") Reserved_6.Value  = words[1];
				if (words[0]=="Reserved_7") Reserved_7.Value  = words[1];
				if (words[0]=="Reserved_8") Reserved_8.Value  = words[1];
				if (words[0]=="Reserved_9") Reserved_9.Value  = words[1];
				if (words[0]=="Reserved_10") Reserved_10.Value  = words[1];
				if (words[0]=="Reserved_11") Reserved_11.Value  = words[1];
				if (words[0]=="Reserved_12") Reserved_12.Value  = words[1];
				if (words[0]=="Reserved_13") Reserved_13.Value  = words[1];
				if (words[0]=="Reserved_14") Reserved_14.Value  = words[1];
				if (words[0]=="Reserved_15") Reserved_15.Value  = words[1];
				if (words[0]=="Reserved_16") Reserved_16.Value  = words[1];
				if (words[0]=="Reserved_17") Reserved_17.Value  = words[1];
				if (words[0]=="Reserved_18") Reserved_18.Value  = words[1];
				if (words[0]=="Reserved_19") Reserved_19.Value  = words[1];
				if (words[0]=="Reserved_20") Reserved_20.Value  = words[1];
				if (words[0]=="Reserved_21") Reserved_21.Value  = words[1];
				if (words[0]=="Reserved_22") Reserved_22.Value  = words[1];
				if (words[0]=="Reserved_23") Reserved_23.Value  = words[1];
				if (words[0]=="Reserved_24") Reserved_24.Value  = words[1];
				if (words[0]=="Reserved_25") Reserved_25.Value  = words[1];
				if (words[0]=="Reserved_26") Reserved_26.Value  = words[1];
				if (words[0]=="Reserved_27") Reserved_27.Value  = words[1];
				if (words[0]=="Reserved_28") Reserved_28.Value  = words[1];
			}
		}
	}
}
