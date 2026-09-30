using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class IE01Pkt : PacketUtil.Packet
	{
		public override void StringListToItems(string []lines)
		{
			foreach (string line in lines)
			{
				List<string> words = PacketUtil.Tokenize.GetWords(line);
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
			}
		}
	}
}
