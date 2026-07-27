using UnityEngine;

public class Guest
{
    public string id;
    public string characterId; // 단골(regular_slots)인 경우 지정된 캐릭터 id. 랜덤 손님이면 null.
    public string targetCocktailId;

    public int difficultyLevel = 0; // random_waves.json/regular_slots.json의 tier
    public string personality; // random_waves.json 전용, 단골은 null
    public float tipMultiplier = 1f; // personalities.json의 tip_mult
    public float patienceMultiplier = 1f; // personalities.json의 patience_mult. hasPatience가 false면 의미 없음.
    public bool hasPatience = true; // 단골(regular_slots) 손님은 인내심 개념이 없어 false.
    public int maxRounds = 1;
    public float delaySec; // 이 손님이 앞선 손님에 이어 등장하기까지의 대기 시간(초)

    public bool isRegular;

    public int orderRound = 0;

    public bool isDrunk = false;
}
