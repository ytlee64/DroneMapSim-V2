using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
namespace DroneMapGCS
{
	public partial class SI01Pkt : PacketUtil.Packet
	{
		public override void StringListToItems(string []lines)
		{
			foreach (string line in lines)
			{
				List<string> words = PacketUtil.Tokenize.GetWords(line);
				if (words[0]=="Length") Length.Value  = words[1];
				if (words[0]=="SeqNumRtn") SeqNumRtn.Value  = words[1];
				if (words[0]=="CmdRtn") CmdRtn.Value  = words[1];
				if (words[0]=="StatusRtn") StatusRtn.Value  = words[1];
				if (words[0]=="TargetSizeRtn") TargetSizeRtn.Value  = words[1];
				if (words[0]=="SlantRangeRtn") SlantRangeRtn.Value  = words[1];
				if (words[0]=="Valid") Valid.Value  = words[1];
				if (words[0]=="OperTime") OperTime.Value  = words[1];
				if (words[0]=="Bit") Bit.Value  = words[1];
				if (words[0]=="PBit") PBit.Value  = words[1];
				if (words[0]=="IBit") IBit.Value  = words[1];
				if (words[0]=="CBit") CBit.Value  = words[1];
				if (words[0]=="EomFpaTemp") EomFpaTemp.Value  = words[1];
				if (words[0]=="EomTemp") EomTemp.Value  = words[1];
				if (words[0]=="IpmCpuTemp") IpmCpuTemp.Value  = words[1];
				if (words[0]=="TrackStatus") TrackStatus.Value  = words[1];
				if (words[0]=="TrackErrorX") TrackErrorX.Value  = words[1];
				if (words[0]=="TrackErrorY") TrackErrorY.Value  = words[1];
				if (words[0]=="TargetPosX") TargetPosX.Value  = words[1];
				if (words[0]=="TargetPosY") TargetPosY.Value  = words[1];
				if (words[0]=="TrackBIT") TrackBIT.Value  = words[1];
				if (words[0]=="TrackConfidence") TrackConfidence.Value  = words[1];
				if (words[0]=="TargetSX") TargetSX.Value  = words[1];
				if (words[0]=="TargetSY") TargetSY.Value  = words[1];
				if (words[0]=="TargetEX") TargetEX.Value  = words[1];
				if (words[0]=="TargetEY") TargetEY.Value  = words[1];
				if (words[0]=="Fid") Fid.Value  = words[1];
				if (words[0]=="Pid") Pid.Value  = words[1];
				if (words[0]=="Latency") Latency.Value  = words[1];
				if (words[0]=="SerialNumber") SerialNumber.Value  = words[1];
				if (words[0]=="IpmAlgVer") IpmAlgVer.Value  = words[1];
				if (words[0]=="IpmOpsVer") IpmOpsVer.Value  = words[1];
				if (words[0]=="IpmFpgaVer") IpmFpgaVer.Value  = words[1];
				if (words[0]=="EomOpsVer") EomOpsVer.Value  = words[1];
				if (words[0]=="EomFpgaVer") EomFpgaVer.Value  = words[1];
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
				if (words[0]=="Reserved1_11") Reserved1_11.Value  = words[1];
				if (words[0]=="Reserved1_12") Reserved1_12.Value  = words[1];
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
			}
		}
	}
}
