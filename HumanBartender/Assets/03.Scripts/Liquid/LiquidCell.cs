using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// 각 픽셀 셀의 데이터를 담는 구조체
    /// </summary>
    [System.Serializable]
    public struct LiquidCell
    {
        public LiquidType liquidType;   // 액체 종류
        public Color32 color;           // 현재 표시 색상
        public float density;           // 밀도 (무거울수록 아래로)
        public float mixRatio;          // 혼합 비율 (0 = 순수, 1 = 완전 혼합)
        public float velocityX;         // X 속도 (교반/흔들기용)
        public float velocityY;         // Y 속도

        public bool IsEmpty => liquidType == LiquidType.Empty;

        public static LiquidCell Empty => new LiquidCell
        {
            liquidType = LiquidType.Empty,
            color = new Color32(0, 0, 0, 0),
            density = 0f,
            mixRatio = 0f,
            velocityX = 0f,
            velocityY = 0f
        };
    }

    /// <summary>
    /// 액체 종류 정의 - 프로젝트에 맞게 확장하세요
    /// </summary>
    public enum LiquidType
    {
        Empty = 0,
        Water,      // ★ 얼음이 녹아서 생긴 물
        Vodka,
        Rum,
        BlueCuracao,
        OrangeJuice,
        Grenadine,
        Kahlua,
        Baileys,
        Lime,
        Tonic,
        Cola,
        Gin,
        Sugar_syrup,
        LemonJuice,
        Vermouth,
        Coffee_liqueur,
        Mixed // 완전히 섞인 상태
    }   
}
