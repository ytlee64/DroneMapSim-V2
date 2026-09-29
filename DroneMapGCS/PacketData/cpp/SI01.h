#pragma once
#include "type.h"

#pragma pack(1)


	struct SI01 // LittleEndian
	{
		uint16_t Length; // 1 #  # 
		uint16_t SeqNumRtn; // 1 #  # 
		uint16_t CmdRtn; // 1 #  # 
		uint16_t StatusRtn; // 1 #  # 
		uint16_t TargetSizeRtn; // 1 #  # 
		uint16_t SlantRangeRtn; // 1 #  # 
		uint16_t Valid; // 1 #  # 
		uint16_t OperTime; // 1 #  # 
		uint16_t Bit; // 1 #  # 
		uint16_t PBit; // 1 #  # 
		uint16_t IBit; // 1 #  # 
		uint16_t CBit; // 1 #  # 
		int16_t EomFpaTemp; // 1 #  # 
		int16_t EomTemp; // 1 #  # 
		int16_t IpmCpuTemp; // 1 #  # 
		int16_t TrackStatus; // 1 #  # 
		int16_t TrackErrorX; // 1 #  # 
		int16_t TrackErrorY; // 1 #  # 
		int16_t TargetPosX; // 1 #  # 
		int16_t TargetPosY; // 1 #  # 
		int16_t TrackBIT; // 1 #  # 
		int16_t TrackConfidence; // 1 #  # 
		int16_t TargetSX; // 1 #  # 
		int16_t TargetSY; // 1 #  # 
		int16_t TargetEX; // 1 #  # 
		int16_t TargetEY; // 1 #  # 
		int16_t Fid; // 1 #  # 
		uint16_t Pid; // 1 #  # 
		uint16_t Latency; // 0.01 #  # 
		uint16_t SerialNumber; // 1 #  # 
		uint16_t IpmAlgVer; // 1 #  # 
		int16_t IpmOpsVer; // 1 #  # 
		int16_t IpmFpgaVer; // 1 #  # 
		int16_t EomOpsVer; // 1 #  # 
		int16_t EomFpgaVer; // 1 #  # 
		int16_t Reserved1_0; // 1 #  # 
		int16_t Reserved1_1; // 1 #  # 
		int16_t Reserved1_2; // 1 #  # 
		int16_t Reserved1_3; // 1 #  # 
		int16_t Reserved1_4; // 1 #  # 
		int16_t Reserved1_5; // 1 #  # 
		int16_t Reserved1_6; // 1 #  # 
		int16_t Reserved1_7; // 1 #  # 
		int16_t Reserved1_8; // 1 #  # 
		int16_t Reserved1_9; // 1 #  # 
		int16_t Reserved1_10; // 1 #  # 
		int16_t Reserved1_11; // 1 #  # 
		int16_t Reserved1_12; // 1 #  # 
		int16_t Reserved2_0; // 1 #  # 
		int16_t Reserved2_1; // 1 #  # 
		int16_t Reserved2_2; // 1 #  # 
		int16_t Reserved2_3; // 1 #  # 
		int16_t Reserved2_4; // 1 #  # 
		int16_t Reserved2_5; // 1 #  # 
		int16_t Reserved2_6; // 1 #  # 
		int16_t Reserved2_7; // 1 #  # 
		int16_t Reserved2_8; // 1 #  # 
		int16_t Reserved2_9; // 1 #  # 
		int16_t Reserved2_10; // 1 #  # 
		int16_t Reserved2_11; // 1 #  # 
		int16_t Reserved2_12; // 1 #  # 
		int16_t Reserved2_13; // 1 #  # 
		int16_t Reserved2_14; // 1 #  # 
		int16_t Reserved2_15; // 1 #  # 
	};


#pragma pack()


