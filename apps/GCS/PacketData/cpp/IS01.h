#pragma once
#include "type.h"

#pragma pack(1)


	struct IS01 // LittleEndian
	{
		uint16_t Length; // 1 #  # 
		uint16_t SeqNum; // 1 #  # 
		uint16_t Cmd; // 1 #  # 
		uint16_t Status; // 1 #  # 
		uint16_t TargetSize; // 1 #  # 
		uint16_t SlantRange; // 1 #  # 
		int32_t MslLat; // 1E-05 #  # 
		int32_t MslLon; // 1E-05 #  # 
		int32_t MslHeight; // 10 #  # 
		int16_t MslVelN; // 1 #  # 
		int16_t MslVelE; // 1 #  # 
		int16_t MslVelD; // 1 #  # 
		int16_t Roll; // 1 #  # 
		int16_t Pitch; // 1 #  # 
		int16_t Yaw; // 1 #  # 
		int16_t RollRate; // 0.1 #  # 
		int16_t PitchRate; // 0.1 #  # 
		int16_t YawRate; // 0.1 #  # 
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
		int16_t Reserved2_16; // 1 #  # 
		int16_t Reserved2_17; // 1 #  # 
		int16_t Reserved2_18; // 1 #  # 
		int16_t Reserved2_19; // 1 #  # 
		int16_t Reserved2_20; // 1 #  # 
		int16_t Reserved2_21; // 1 #  # 
		int16_t Reserved2_22; // 1 #  # 
		int16_t Reserved2_23; // 1 #  # 
		int16_t Reserved2_24; // 1 #  # 
		int16_t Reserved2_25; // 1 #  # 
		int16_t Reserved2_26; // 1 #  # 
		int16_t Reserved2_27; // 1 #  # 
		int16_t Reserved2_28; // 1 #  # 
		int16_t Reserved2_29; // 1 #  # 
		int16_t Reserved2_30; // 1 #  # 
		int16_t Reserved2_31; // 1 #  # 
	};


#pragma pack()


