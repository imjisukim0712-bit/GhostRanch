namespace GhostWidget;

internal enum DecorCategory { Toy, Hideout, Rest }

internal readonly record struct DecorSpecies(
    string Name, DecorCategory Category, string Description, string PlayLine, int Price, string Art);

internal readonly record struct DecorPlacementSave(int DecorIndex, double X, double Y);

/// <summary>The 15 purchasable desktop decorations ghosts occasionally wander over to and play at.
/// Purely cosmetic: buying and placing them never changes affection/energy/experience.</summary>
internal static class DecorCatalog
{
    internal static readonly DecorSpecies[] Items =
    [
        new("축구공", DecorCategory.Toy, "통통 튀는 낡은 가죽 공. 발끝으로 톡톡 건드리고 싶어진다.", "슛! …골인!", 15, "SoccerBall"),
        new("실뭉치", DecorCategory.Toy, "풀어헤치고 싶은 충동을 부르는 빨간 실뭉치.", "데굴데굴~ 어디까지 굴러갈까?", 15, "YarnBall"),
        new("프리스비", DecorCategory.Toy, "누가 먼저 물어올지 내기하고 싶은 원반.", "휙— 하늘 높이 날아간다!", 18, "Frisbee"),
        new("장난감 자동차", DecorCategory.Toy, "태엽을 감으면 혼자 붕붕 굴러갈 것 같다.", "부릉부릉! 내가 운전할게!", 20, "ToyCar"),
        new("비눗방울 기계", DecorCategory.Toy, "쉬지 않고 몽글몽글한 비눗방울을 뿜어낸다.", "방울 안에 내가 비친다!", 22, "BubbleMachine"),

        new("낡은 커튼", DecorCategory.Hideout, "살짝 들춰보면 그림자 하나가 숨어있을 것 같다.", "커튼 뒤에 숨었다, 못 찾겠지?", 24, "Curtain"),
        new("속 빈 고목", DecorCategory.Hideout, "구멍 속에 몸을 웅크리면 딱 맞을 크기.", "여기 딱 맞아, 편안해!", 26, "TreeStump"),
        new("낡은 무덤", DecorCategory.Hideout, "이끼 낀 오래된 묘비. 유령이라면 한 번쯤 기대고 싶은 자리.", "여기가… 제일 편안해.", 28, "Grave"),
        new("다락방 트렁크", DecorCategory.Hideout, "삐걱이는 뚜껑 안에 무엇이 숨어있을까.", "안에서 몰래 훔쳐보는 중…", 30, "AtticTrunk"),
        new("어두운 동굴", DecorCategory.Hideout, "어둡고 서늘한 입구. 낮잠 자기 딱 좋아 보인다.", "쉿… 아무도 못 찾을 거야.", 32, "Cave"),

        new("그물 해먹", DecorCategory.Rest, "나무 사이에 걸린 그물 침대. 흔들거리며 낮잠 자기 좋다.", "살랑살랑… 기분 좋아.", 30, "Hammock"),
        new("모닥불", DecorCategory.Rest, "타닥타닥 타오르는 모닥불 주위가 제일 아늑하다.", "따뜻하다… 여기서 좀 더 있을래.", 35, "Campfire"),
        new("야외 헬스장", DecorCategory.Rest, "작은 철봉과 아령 세트. 웬일로 운동 의욕이 솟는다.", "하나, 둘! 하나, 둘!", 40, "Gym"),
        new("노천카페 테이블", DecorCategory.Rest, "작은 파라솔 아래 티타임을 즐기기 좋은 자리.", "오늘의 티타임, 시작할까?", 42, "CafeTable"),
        new("노천 온천", DecorCategory.Rest, "김이 모락모락 올라오는 노천탕. 몸이 녹아내릴 것 같다.", "으어… 사르르 녹는다…", 45, "HotSpring"),
    ];
}
