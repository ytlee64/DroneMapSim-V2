#pragma once
#include "type.h"

#pragma pack(1)


	struct GndTruth // LittleEndian
	{
		uint16_t Valid; // 1 #  # 
		uint16_t FrameID; // 1 #  # 
		int16_t TrackErrorX; // 0.005 #  # 
		int16_t TrackErrorY; // 0.005 #  # 
		uint16_t TargetPosX; // 1 #  # 
		uint16_t TargetPosY; // 1 #  # 
		uint16_t TargetSX; // 1 #  # 
		uint16_t TargetSY; // 1 #  # 
		uint16_t TargetEX; // 1 #  # 
		uint16_t TargetEY; // 1 #  # 
		uint16_t SlantRange; // 10 #  # 
		int32_t TimeStamp; // 1 #  # 
		float Lat; // 1 #  # 
		float Lon; // 1 #  # 
		float Alt; // 1 #  # 
		uint16_t Reserved_0; // 1 #  # 
		uint16_t Reserved_1; // 1 #  # 
		uint16_t Reserved_2; // 1 #  # 
		uint16_t Reserved_3; // 1 #  # 
		uint16_t Reserved_4; // 1 #  # 
		uint16_t Reserved_5; // 1 #  # 
		uint16_t Reserved_6; // 1 #  # 
		uint16_t Reserved_7; // 1 #  # 
		uint16_t Reserved_8; // 1 #  # 
		uint16_t Reserved_9; // 1 #  # 
		uint16_t Reserved_10; // 1 #  # 
		uint16_t Reserved_11; // 1 #  # 
		uint16_t Reserved_12; // 1 #  # 
		uint16_t Reserved_13; // 1 #  # 
		uint16_t Reserved_14; // 1 #  # 
		uint16_t Reserved_15; // 1 #  # 
		uint16_t Reserved_16; // 1 #  # 
		uint16_t Reserved_17; // 1 #  # 
		uint16_t Reserved_18; // 1 #  # 
		uint16_t Reserved_19; // 1 #  # 
		uint16_t Reserved_20; // 1 #  # 
		uint16_t Reserved_21; // 1 #  # 
		uint16_t Reserved_22; // 1 #  # 
		uint16_t Reserved_23; // 1 #  # 
		uint16_t Reserved_24; // 1 #  # 
		uint16_t Reserved_25; // 1 #  # 
		uint16_t Reserved_26; // 1 #  # 
		uint16_t Reserved_27; // 1 #  # 
		uint16_t Reserved_28; // 1 #  # 
	};


#pragma pack()


