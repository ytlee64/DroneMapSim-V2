#include "NavBase.h"

INavBase::INavBase() 
    : bAutoNav(false)
    , Waypoints{
    { 1, FVector(-10000.0f, -10000, 10000.0f), 1500.0f },
    { 2, FVector(10000.0f, -10000.0f, 10000.0f), 1500.0f },
    { 3, FVector(10000.0f,  10000.0f, 10000.0f), 1500.0f },
    { 4, FVector(-10000.0f,  10000.0f, 10000.0f), 1500.0f }
}
{
    // 본문은 비워두거나 추가 초기화 로직 작성
}