using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class IS01Pkt : PacketUtil.Packet
	{
		public override void StringListToItems(string []lines)
		{
			foreach (string line in lines)
			{
				List<string> words = PacketUtil.Tokenize.GetWords(line);
				if (words[0]=="Length") Length.Value  = words[1];
				if (words[0]=="SeqNum") SeqNum.Value  = words[1];
				if (words[0]=="Cmd") Cmd.Value  = words[1];
				if (words[0]=="Status") Status.Value  = words[1];
				if (words[0]=="TargetSize") TargetSize.Value  = words[1];
				if (words[0]=="SlantRange") SlantRange.Value  = words[1];
				if (words[0]=="MslLat") MslLat.Value  = words[1];
				if (words[0]=="MslLon") MslLon.Value  = words[1];
				if (words[0]=="MslHeight") MslHeight.Value  = words[1];
				if (words[0]=="MslVelN") MslVelN.Value  = words[1];
				if (words[0]=="MslVelE") MslVelE.Value  = words[1];
				if (words[0]=="MslVelD") MslVelD.Value  = words[1];
				if (words[0]=="Roll") Roll.Value  = words[1];
				if (words[0]=="Pitch") Pitch.Value  = words[1];
				if (words[0]=="Yaw") Yaw.Value  = words[1];
				if (words[0]=="RollRate") RollRate.Value  = words[1];
				if (words[0]=="PitchRate") PitchRate.Value  = words[1];
				if (words[0]=="YawRate") YawRate.Value  = words[1];
				if (words[0]=="Reserved1_0") Reserved1_0.Value  = words[1];
				if (words[0]=="Reserved1_1") Reserved1_1.Value  = words[1];
				if (words[0]=="Reserved1_2") Reserved1_2.Value  = words[1];
				if (words[0]=="Reserved1_3") Reserved1_3.Value  = words[1];
				if (words[0]=="Reserved1_4") Reserved1_4.Value  = words[1];
				if (words[0]=="Reserved1_5") Reserved1_5.Value  = words[1];
				if (words[0]=="Reserved1_6") Reserved1_6.Value  = words[1];
				if (words[0]=="Reserved1_7") Reserved1_7.Value  = words[1];
				if (words[0]=="Reserved1_8") Reserved1_8.Value  = words[1];
				if (words[0]=="Reserved1_9") Reserved1_9.Value  = words[1];
				if (words[0]=="Reserved1_10") Reserved1_10.Value  = words[1];
				if (words[0]=="Reserved2_0") Reserved2_0.Value  = words[1];
				if (words[0]=="Reserved2_1") Reserved2_1.Value  = words[1];
				if (words[0]=="Reserved2_2") Reserved2_2.Value  = words[1];
				if (words[0]=="Reserved2_3") Reserved2_3.Value  = words[1];
				if (words[0]=="Reserved2_4") Reserved2_4.Value  = words[1];
				if (words[0]=="Reserved2_5") Reserved2_5.Value  = words[1];
				if (words[0]=="Reserved2_6") Reserved2_6.Value  = words[1];
				if (words[0]=="Reserved2_7") Reserved2_7.Value  = words[1];
				if (words[0]=="Reserved2_8") Reserved2_8.Value  = words[1];
				if (words[0]=="Reserved2_9") Reserved2_9.Value  = words[1];
				if (words[0]=="Reserved2_10") Reserved2_10.Value  = words[1];
				if (words[0]=="Reserved2_11") Reserved2_11.Value  = words[1];
				if (words[0]=="Reserved2_12") Reserved2_12.Value  = words[1];
				if (words[0]=="Reserved2_13") Reserved2_13.Value  = words[1];
				if (words[0]=="Reserved2_14") Reserved2_14.Value  = words[1];
				if (words[0]=="Reserved2_15") Reserved2_15.Value  = words[1];
				if (words[0]=="Reserved2_16") Reserved2_16.Value  = words[1];
				if (words[0]=="Reserved2_17") Reserved2_17.Value  = words[1];
				if (words[0]=="Reserved2_18") Reserved2_18.Value  = words[1];
				if (words[0]=="Reserved2_19") Reserved2_19.Value  = words[1];
				if (words[0]=="Reserved2_20") Reserved2_20.Value  = words[1];
				if (words[0]=="Reserved2_21") Reserved2_21.Value  = words[1];
				if (words[0]=="Reserved2_22") Reserved2_22.Value  = words[1];
				if (words[0]=="Reserved2_23") Reserved2_23.Value  = words[1];
				if (words[0]=="Reserved2_24") Reserved2_24.Value  = words[1];
				if (words[0]=="Reserved2_25") Reserved2_25.Value  = words[1];
				if (words[0]=="Reserved2_26") Reserved2_26.Value  = words[1];
				if (words[0]=="Reserved2_27") Reserved2_27.Value  = words[1];
				if (words[0]=="Reserved2_28") Reserved2_28.Value  = words[1];
				if (words[0]=="Reserved2_29") Reserved2_29.Value  = words[1];
				if (words[0]=="Reserved2_30") Reserved2_30.Value  = words[1];
				if (words[0]=="Reserved2_31") Reserved2_31.Value  = words[1];
			}
		}
	}
}
