using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GhostWidget;

public partial class MainWindow : Window
{
    private readonly Random random = new();
    private readonly DispatcherTimer movementTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly DispatcherTimer blinkTimer = new() { Interval = TimeSpan.FromSeconds(3.1) };
    private readonly DispatcherTimer speechTimer = new() { Interval = TimeSpan.FromSeconds(2.7) };
    private readonly DispatcherTimer contentTimer = new();
    private readonly DispatcherTimer captureTimer = new();
    private readonly DispatcherTimer battleTimer = new();
    private readonly DispatcherTimer raiseTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly GhostSpecies[] ghosts =
    [
        // Only these five share the exact supplied base silhouette as palette variants.
        new("빨강이", "기본 유령", "#F30100", "보통", .18, "Base", "용감한 장난꾸러기"),
        new("분홍이", "기본 유령", "#E600EC", "보통", .20, "Base", "다정한 수다쟁이"),
        new("하늘이", "기본 유령", "#19D7DD", "보통", .22, "Base", "호기심 많은 탐험가"),
        new("보라비", "기본 유령", "#8B42E8", "보통", .24, "Base", "느긋한 몽상가"),
        new("민티", "기본 유령", "#79E4B7", "보통", .25, "Base", "상냥한 돌봄이"),
        // Every remaining entry has its own themed silhouette or outline.
        new("몽글이", "동그란 유령", "#4AD5D0", "보통", .27, "Round", "포근한 먹보"),
        new("호박이", "잭오랜턴 유령", "#FF9C25", "보통", .30, "Pumpkin", "겁 많지만 흥 많은 파티광"),
        new("검은수염", "해적 유령", "#F04B45", "희귀", .40, "Pirate", "허풍쟁이 선장"),
        new("촛농이", "촛불 유령", "#FFF2C2", "희귀", .38, "Candle", "조용한 이야기꾼"),
        new("이끼", "이끼 유령", "#62B44B", "보통", .27, "Moss", "말 느린 숲지기"),
        new("루나", "달빛 유령", "#F5D65C", "희귀", .42, "Moon", "새벽을 좋아하는 시인"),
        new("별콩", "별가루 유령", "#75C8FF", "희귀", .36, "Star", "칭찬에 약한 반짝이"),
        new("뭉게", "구름 유령", "#D9DCE8", "보통", .29, "Cloud", "졸린 낙천가"),
        new("까망이", "그림자 유령", "#45425B", "희귀", .45, "Shadow", "낯가리는 관찰자"),
        new("로지", "장미 유령", "#FF5D8F", "희귀", .40, "Rose", "새침한 로맨티스트"),
        new("박쥐", "박쥐 유령", "#5D55BC", "희귀", .44, "Bat", "도도한 야행성"),
        new("해롱", "해파리 유령", "#38C8EC", "희귀", .43, "Jelly", "둥둥 떠다니는 낙천가"),
        new("사박", "모래 유령", "#D6A863", "희귀", .43, "Sand", "신중한 고고학자"),
        new("밤비", "마녀 유령", "#734F96", "영웅", .58, "Witch", "엉뚱한 마법사"),
        new("만월", "전설 달 유령", "#F3E7A1", "전설", .72, "Legend", "품위 있는 수호자"),
        new("이불이", "시트 유령", "#F4F4F4", "보통", .29, "New1", "숨바꼭질을 좋아하는 겁쟁이"),
        new("해골콩", "해골 유령", "#E5E5E5", "보통", .32, "New2", "딸깍거리는 수다쟁이"),
        new("붕붕이", "미이라 유령", "#E7D5B0", "희귀", .38, "New3", "붕대를 정리하는 완벽주의자"),
        new("뿔콩", "도깨비 유령", "#EF4A3E", "희귀", .41, "New4", "장난을 좋아하는 말썽꾸러기"),
        new("리퍼", "사신 유령", "#3C3746", "희귀", .45, "New5", "조용히 길을 안내하는 안내자"),
        new("냥혼", "고양이 유령", "#F28A42", "보통", .33, "New6", "창가 낮잠을 좋아하는 고양이"),
        new("멍혼", "강아지 유령", "#B77845", "보통", .34, "New7", "꼬리를 흔드는 충성파"),
        new("문문", "문어 유령", "#35D0C6", "희귀", .42, "New8", "손이 많은 발명가"),
        new("접시령", "UFO 유령", "#A8A8B8", "희귀", .44, "New9", "별을 수집하는 여행자"),
        new("텔레령", "TV 유령", "#8B70CE", "희귀", .44, "New10", "심야 방송을 좋아하는 DJ"),
        new("책벌레", "마법책 유령", "#E7E0C4", "보통", .36, "New11", "주문을 중얼거리는 독서가"),
        new("주전자", "티포트 유령", "#F3A5C4", "보통", .34, "New12", "따뜻한 차를 권하는 주인"),
        new("풍선콩", "풍선 유령", "#F44A4A", "보통", .31, "New13", "높은 곳을 꿈꾸는 낙천가"),
        new("물약이", "플라스크 유령", "#76D98A", "희귀", .40, "New14", "실험을 사랑하는 연금술사"),
        new("케이키", "케이크 유령", "#F2A1B5", "희귀", .39, "New15", "축하를 기다리는 파티광"),
        new("버섯이", "버섯 유령", "#F15656", "보통", .35, "New16", "비 오는 날을 좋아하는 숲지기"),
        new("산호령", "산호 유령", "#F18C7A", "희귀", .42, "New17", "물속 노래를 부르는 가수"),
        new("눈콩", "눈사람 유령", "#F7F7F7", "보통", .34, "New18", "추운 날만 신나는 친구"),
        new("우산령", "우산 유령", "#448DDD", "희귀", .39, "New19", "빗소리를 모으는 우체부"),
        new("등불이", "랜턴 유령", "#F0C54B", "희귀", .40, "New20", "길을 밝혀 주는 안내등"),
        new("종이학", "종이학 유령", "#EF87BE", "희귀", .42, "New21", "소원을 배달하는 비행가"),
        new("거미령", "거미 유령", "#41414E", "희귀", .43, "New22", "뜨개질이 취미인 장인"),
        new("달팽혼", "달팽이 유령", "#A97844", "보통", .35, "New23", "천천히 여행하는 기록가"),
        new("용꼬리", "작은 용 유령", "#74D989", "영웅", .55, "New24", "불꽃 대신 재채기하는 용"),
        new("왕관콩", "왕족 유령", "#F1D15F", "영웅", .56, "New25", "예절을 중시하는 꼬마 왕"),
        new("기사령", "기사 유령", "#AEB8C6", "영웅", .57, "New26", "용감한 견습 수호자"),
        new("광대령", "광대 유령", "#A34CD5", "영웅", .54, "New27", "농담을 모으는 공연가"),
        new("나비령", "나비 유령", "#F18DC6", "영웅", .53, "New28", "꽃밭을 순찰하는 무희"),
        new("수정령", "수정 유령", "#8CD7FF", "영웅", .58, "New29", "빛을 굴절시키는 보석가"),
        new("유성령", "유성 유령", "#F3D638", "전설", .70, "New30", "소원을 싣고 달리는 전령"),
        // Boss-tier entries: excluded from capture/gacha/roster until first defeated in a boss duel (see PickWildEnemyIndex/TryRollBossEncounter).
        new("화롯불 군주", "화로 군주 유령", "#FF6A1A", "전설", .90, "BossHearth", "말이 없는, 그러나 타오르는 눈빛의 감시자", IsBoss: true),
        new("태고이끼정령", "숲의 정령 유령", "#2F6B34", "전설", 1.05, "BossMoss", "몇 백 년째 같은 자리를 지키는 숲의 오래된 정령", IsBoss: true),
        new("물안개 여왕", "물안개 여왕 유령", "#1AA8C4", "전설", 1.15, "BossMist", "느긋하지만 화나면 파도처럼 밀려오는", IsBoss: true),
        new("새벽별 무녀", "여명의 무녀 유령", "#FFE8A3", "전설", 1.20, "BossDawn", "가장 먼저 뜨는 별의 빛을 두른 새벽의 무녀", IsBoss: true),
        new("칠흑의 문지기", "문지기 유령", "#3D2B5C", "전설", 1.30, "BossGate", "말없이 미소 짓는, 안개 저편에서 온 문지기", IsBoss: true),
        new("화산대제", "용암 대제 유령", "#FF3300", "전설", 1.50, "BossVolcano", "흑요석 갑주 사이로 용암이 배어 나오는 대제", IsBoss: true),
        new("폭풍해왕", "폭풍의 왕 유령", "#0B4F6C", "전설", 1.55, "BossStorm", "머리 위로 파도가 부서지는 폭풍의 왕", IsBoss: true),
        new("고목수호신", "고목 수호신 유령", "#4B3621", "전설", 1.65, "BossTree", "숲 그 자체가 된 수호신", IsBoss: true),
        new("천상의 심판자", "천상의 심판자 유령", "#FFF4D6", "전설", 1.75, "BossCelestial", "별빛 후광을 두르고 죄를 가르는 하늘 그 자체", IsBoss: true),
        new("적막의 왕", "적막의 왕 유령", "#2B1640", "전설", 1.85, "BossVoid", "소용돌이치는 정적을 두른 문 너머의 존재", IsBoss: true)
    ];

    private Point target;
    private Point dragCursorStart;
    private Point dragWindowStart;
    private Point lastDragCursor;
    private DateTime lastDragSampleAt;
    private DateTime dragStartedAt;
    private Vector flingVelocity;
    private Vector recentDragVelocity;
    private Point velocitySampleCursor;
    private DateTime velocitySampleAt;
    private DateTime lastDragMotionAt;
    private readonly List<DragSample> dragSamples = [];
    private bool dragging;
    private bool resting;
    private bool didDrag;
    private int affection = 46;
    private int energy = 82;
    private int level = 1;
    private int ghostIndex;
    private int luna = 120;
    private int orbs = 3;
    private int wins;
    private int winStreak;
    private int duelsSinceBoss;
    private double pendingBattleCooldownMinutes;
    private readonly HashSet<int> unlockedGhosts = [0];
    private int experience;
    private FarmWindow? farmWindow;
    private SummonSelectionWindow? summonSelectionWindow;
    private DecorShopWindow? decorShopWindow;
    private readonly List<SummonedGhostWindow> summonedGhosts = [];
    private readonly HashSet<int> ownedDecor = [];
    private DecorPlacementSave[] pendingDecorPlacements = [];
    private DecorWindow? targetDecorItem;
    private DecorWindow? lastVisitedDecor;
    private DateTime lastVisitedDecorAt = DateTime.MinValue;
    private bool decorPlaySessionActive;
    private double globalGhostSize = 1;
    private DesktopNotificationWindow? contentNotification;
    private TrayIcon? trayIcon;
    private bool captureReady;
    private bool battleReady;
    private DateTime raiseReadyAt = DateTime.MinValue;
    private DateTime lastHeartbeatRedraw = DateTime.MinValue;
    private readonly Dictionary<string, string[]> dialogueLines = new()
    {
        ["빨강이"] = ["Boo! 내가 먼저 갈게!", "장난 한 번만 치고 올게!"],
        ["분홍이"] = ["오늘 있었던 일 들려줄까?", "같이 있으면 기분이 몽글몽글해!"],
        ["하늘이"] = ["저기 반짝이는 건 뭐지?", "새로운 길을 찾아볼래?"],
        ["보라비"] = ["구름 위는 어떤 촉감일까…", "조금만 더 둥둥 떠 있을래."],
        ["민티"] = ["피곤하면 내 옆에서 쉬어.", "오늘도 잘하고 있어!"],
        ["몽글이"] = ["달과자 냄새가 나는 것 같아!", "폭신한 곳에서 뒹굴고 싶어."],
        ["호박이"] = ["무서운 얘기… 아니, 재밌는 얘기 해줄까?", "파티 불빛은 내가 맡을게!"],
        ["검은수염"] = ["일곱 바다를 떠돌던 선장이라고!", "보물 지도는… 어딘가에 있겠지!"],
        ["촛농이"] = ["불빛은 작은 약속 같아.", "조용한 밤이 제일 좋아."],
        ["이끼"] = ["천천히 가도 숲은 기다려.", "…새 잎이 났어."],
        ["루나"] = ["달이 예쁜 밤이네.", "새벽에는 비밀이 많아."],
        ["별콩"] = ["나 오늘 반짝여?", "한 번만 더 칭찬해줘!"],
        ["뭉게"] = ["하암… 구름 침대가 생각나.", "급할 것 없지, 둥둥."],
        ["까망이"] = ["…여기 있어.", "멀리서 지켜보고 있었어."],
        ["로지"] = ["내 꽃잎, 예쁘다고 말해줘.", "흥, 그래도 같이 걷는 건 허락할게."],
        ["박쥐"] = ["낮은 너무 밝아.", "밤 산책이라면 동행하지."],
        ["해롱"] = ["물결처럼 살랑살랑~", "급하면 물에 뜨기 어렵대!"],
        ["사박"] = ["발밑의 모래에도 이야기가 있어.", "천천히 보면 길을 찾을 수 있어."],
        ["밤비"] = ["에헴, 오늘의 마법은 대성공이야!", "빗자루는… 잠깐 어디 갔지?"],
        ["만월"] = ["내 빛이 길을 비춰 줄게.", "달이 지켜보고 있단다."]
    };

    public MainWindow()
    {
        InitializeComponent();
        LoadState();
        movementTimer.Tick += MoveFrame;
        blinkTimer.Tick += Blink;
        speechTimer.Tick += (_, _) => HideSpeech();
        contentTimer.Tick += ContentTimer_Tick;
        captureTimer.Tick += CaptureTimer_Tick;
        battleTimer.Tick += BattleTimer_Tick;
        raiseTimer.Tick += RaiseTimer_Tick;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Rect area = VirtualDesktopBounds;
        Left = area.Right - Width - 30;
        Top = area.Bottom - Height - 18;
        ChooseDestination();
        ((Storyboard)FindResource("FloatStoryboard")).Begin(this, true);
        movementTimer.Start();
        blinkTimer.Start();
        UpdateInfo();
        trayIcon = new TrayIcon(GhostLayer.ContextMenu, GhostLayer, "유령사냥");

        if (Environment.GetCommandLineArgs().Contains("--farm-preview"))
        {
            farmWindow = new FarmWindow(this);
            farmWindow.Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--capture-preview"))
        {
            new CaptureGameWindow(this, null, 18).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--battle-preview"))
        {
            new BattleWindow(this, null, PickWildEnemyIndex(ghostIndex)).Show();
            return;
        }

        string? bossPreviewArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--boss-preview", StringComparison.Ordinal));
        if (bossPreviewArg is not null)
        {
            int[] bossIndexes = Enumerable.Range(0, ghosts.Length).Where(i => ghosts[i].IsBoss).ToArray();
            int bossOffset = bossPreviewArg.Contains('=') && int.TryParse(bossPreviewArg.AsSpan(bossPreviewArg.IndexOf('=') + 1), out int parsed) ? parsed : 0;
            new BattleWindow(this, null, bossIndexes[Math.Clamp(bossOffset, 0, bossIndexes.Length - 1)]).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--quest-preview"))
        {
            new QuestWindow(this, QuestKind.Timing).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--apple-preview"))
        {
            new QuestWindow(this, QuestKind.AppleSum).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--apple-drag-preview"))
        {
            new QuestWindow(this, QuestKind.AppleSum).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--hide-preview"))
        {
            new QuestWindow(this, QuestKind.HideAndSeek).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--variation-preview"))
        {
            new VariationBoardWindow(this).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--actual-comparison-preview"))
        {
            new ActualComparisonBoardWindow(this).Show();
            return;
        }

        if (Environment.GetCommandLineArgs().Contains("--preview"))
        {
            await Task.Delay(450);
            SavePreview();
            Close();
            return;
        }
        RestorePlacedDecor();
        ScheduleContentEvent(first: true);
        ScheduleCaptureEvent();
        ScheduleBattleEvent();
        raiseTimer.Start();
    }

    private void ScheduleContentEvent(bool first = false)
    {
        contentTimer.Stop();
        contentTimer.Interval = first ? TimeSpan.FromSeconds(random.Next(35, 61)) : TimeSpan.FromMinutes(4 + random.NextDouble() * 2);
        contentTimer.Start();
    }

    private void ScheduleCaptureEvent()
    {
        captureTimer.Stop();
        captureTimer.Interval = TimeSpan.FromMinutes(14 + random.NextDouble() * 2);
        captureTimer.Start();
    }

    private void ScheduleBattleEvent()
    {
        battleTimer.Stop();
        double penalty = pendingBattleCooldownMinutes;
        pendingBattleCooldownMinutes = 0;
        battleTimer.Interval = TimeSpan.FromMinutes(9 + random.NextDouble() * 2 + penalty);
        battleTimer.Start();
    }

    private void ContentTimer_Tick(object? sender, EventArgs e)
    {
        contentTimer.Stop();
        ScheduleContentEvent();
        ShowContentNotification(DesktopEventKind.Quest);
    }

    private void CaptureTimer_Tick(object? sender, EventArgs e)
    {
        captureTimer.Stop();
        ScheduleCaptureEvent();
        ShowContentNotification(DesktopEventKind.Capture);
    }

    private void BattleTimer_Tick(object? sender, EventArgs e)
    {
        battleTimer.Stop();
        ScheduleBattleEvent();
        ShowContentNotification(DesktopEventKind.Battle);
    }

    private void ShowContentNotification(DesktopEventKind kind)
    {
        if (kind == DesktopEventKind.Capture) captureReady = true;
        if (kind == DesktopEventKind.Battle) battleReady = true;
        RefreshFarm();
        if (contentNotification is { IsVisible: true }) return;
        contentNotification = new DesktopNotificationWindow(this, kind);
        contentNotification.Closed += (_, _) => contentNotification = null;
        contentNotification.Show();
    }

    private void RaiseTimer_Tick(object? sender, EventArgs e)
    {
        if (DateTime.Now >= raiseReadyAt)
        {
            raiseTimer.Stop();
            ShowSpeech("이제 키울 수 있어!");
        }
        farmWindow?.RefreshState();
    }

    private void MoveFrame(object? sender, EventArgs e)
    {
        HeartbeatRedraw();
        if (decorPlaySessionActive) return;
        if (resting) return;
        if (dragging)
        {
            SampleDragMotion();
            return;
        }
        if (flingVelocity.Length > .05)
        {
            MoveFling();
            return;
        }
        double dx = target.X - Left;
        double dy = target.Y - Top;
        // A stronger gaze makes the roaming direction readable at widget scale.
        MainGhostArtwork.SetGaze(dx >= 0 ? 14 : -14, 0);
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < 5)
        {
            if (targetDecorItem is { } item) PlayWithDecor(item);
            else
            {
                ChooseDestination();
                if (random.NextDouble() < 0.32)
                    Speak();
            }
            return;
        }

        double speed = energy < 15 ? 0.38 : 0.72;
        Left += dx / distance * speed;
        Top += dy / distance * speed;
    }

    // A borderless AllowsTransparency window can occasionally render fully blank after a
    // display/GPU hiccup (sleep, monitor change, driver reset) even though the process and
    // window are still alive. Roaming already forces a fresh frame every tick; while resting
    // the widget sits fully static, so nothing else would catch or correct that. This runs at
    // most once a second to self-heal without any visible cost.
    private void HeartbeatRedraw()
    {
        if (decorPlaySessionActive) return;
        if (DateTime.Now - lastHeartbeatRedraw < TimeSpan.FromSeconds(1)) return;
        lastHeartbeatRedraw = DateTime.Now;
        if (Visibility != Visibility.Visible) Visibility = Visibility.Visible;
        if (Opacity < 1) Opacity = 1;
        Rect area = VirtualDesktopBounds;
        if (Left + Width < area.Left || Left > area.Right || Top + Height < area.Top || Top > area.Bottom)
        {
            Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
            Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
        }
        InvalidateVisual();
        MainGhostArtwork.InvalidateVisual();
    }

    private void ChooseDestination()
    {
        targetDecorItem = null;
        DecorWindow[] candidates = DecorWindow.PlacedItems
            .Where(item => item != lastVisitedDecor || DateTime.Now - lastVisitedDecorAt > TimeSpan.FromSeconds(45))
            .ToArray();
        if (candidates.Length > 0 && random.NextDouble() < 0.18)
        {
            double DistanceSquaredTo(DecorWindow item) => (item.Left - Left) * (item.Left - Left) + (item.Top - Top) * (item.Top - Top);
            targetDecorItem = candidates.OrderBy(DistanceSquaredTo).First();
            target = targetDecorItem.GetApproachPoint(Width, Height);
            return;
        }
        Rect area = VirtualDesktopBounds;
        double maxX = Math.Max(area.Left + 30, area.Right - Width - 30);
        double maxY = Math.Max(area.Top + 30, area.Bottom - Height - 20);
        target = new Point(
            area.Left + 30 + random.NextDouble() * (maxX - area.Left - 30),
            area.Top + 30 + random.NextDouble() * (maxY - area.Top - 30));
    }

    // Purely cosmetic: no stat changes. Runs a short category-specific choreography (the item
    // itself moves for toys, the ghost hides for hideouts, the ghost rests for facilities) and
    // finishes with the existing Bounce/ShowSpeech reaction.
    private async void PlayWithDecor(DecorWindow item)
    {
        if (decorPlaySessionActive || item.IsPlaying) return;
        lastVisitedDecor = item;
        lastVisitedDecorAt = DateTime.Now;
        targetDecorItem = null;
        decorPlaySessionActive = true;
        item.IsPlaying = true;
        DecorSpecies species = DecorCatalog.Items[item.DecorIndex];
        try
        {
            switch (species.Category)
            {
                case DecorCategory.Toy: await DribbleSequence(item); break;
                case DecorCategory.Hideout: await HideSequence(item); break;
                default: await RestSequence(); break;
            }
        }
        finally
        {
            item.IsPlaying = false;
            decorPlaySessionActive = false;
        }
        Bounce();
        ShowSpeech(species.PlayLine);
        ChooseDestination();
    }

    // A legible kick-roll-chase cadence, not simultaneous motion: the ghost reaches the item,
    // visibly kicks it (Bounce as the kick, then the roll starts a beat later), chases the roll
    // down, and repeats — so it reads as dribbling, not just the item sliding into place once.
    private async Task DribbleSequence(DecorWindow item)
    {
        Rect area = VirtualDesktopBounds;
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 300);
        for (int rep = 0; rep < 4; rep++)
        {
            Bounce();
            await Task.Delay(110);
            double angle = random.NextDouble() * Math.PI * 2;
            double distance = 70 + random.NextDouble() * 55;
            Point roll = new(
                Math.Clamp(item.Left + Math.Cos(angle) * distance, area.Left + 8, area.Right - item.Width - 8),
                Math.Clamp(item.Top + Math.Sin(angle) * distance, area.Top + 8, area.Bottom - item.Height - 8));
            MainGhostArtwork.SetGaze(roll.X >= item.Left ? 14 : -14, 0);
            await item.AnimateToAsync(roll, 320);
            await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 260);
        }
        // One last firm touch, distinct from the dribbling rolls: trap it and send it home.
        Bounce();
        await Task.Delay(120);
        await item.AnimateToAsync(item.HomePosition, 460);
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 260);
    }

    // A shy little peekaboo: the ghost fades to a faint silhouette, then pops back.
    private async Task HideSequence(DecorWindow item)
    {
        MainGhostArtwork.SetGaze(item.Left >= Left ? 14 : -14, 0);
        Fade(this, .15, 260);
        await Task.Delay(900 + random.Next(400));
        Fade(this, 1, 220);
        await Task.Delay(240);
    }

    // Borrows the sleeping pose for a relaxed beat without touching the separate "잠깐 쉬기" sleep mode.
    private async Task RestSequence()
    {
        MainGhostArtwork.SetGaze(0, 0);
        MainGhostArtwork.IsSleeping = true;
        await Task.Delay(1100 + random.Next(500));
        MainGhostArtwork.IsSleeping = resting;
    }

    private async Task AnimateSelfToAsync(Point target, int milliseconds)
    {
        Point start = new(Left, Top);
        int steps = Math.Max(1, milliseconds / 16);
        for (int step = 1; step <= steps; step++)
        {
            double t = 1 - Math.Pow(1 - (double)step / steps, 3);
            Left = start.X + (target.X - start.X) * t;
            Top = start.Y + (target.Y - start.Y) * t;
            MainGhostArtwork.SetGaze(target.X >= start.X ? 14 : -14, 0);
            await Task.Delay(16);
        }
    }

    private void Ghost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (decorPlaySessionActive) return;
        if (resting)
        {
            WakeMainFromSleep();
            e.Handled = true;
            return;
        }
        if (e.ClickCount == 2)
        {
            Battle();
            e.Handled = true;
            return;
        }
        if (!GetCursorPos(out NativePoint cursor)) return;
        dragCursorStart = new Point(cursor.X, cursor.Y);
        dragWindowStart = new Point(Left, Top);
        lastDragCursor = dragCursorStart;
        lastDragSampleAt = DateTime.UtcNow;
        dragStartedAt = lastDragSampleAt;
        velocitySampleCursor = dragCursorStart;
        velocitySampleAt = lastDragSampleAt;
        lastDragMotionAt = lastDragSampleAt;
        recentDragVelocity = new Vector();
        flingVelocity = new Vector();
        dragSamples.Clear();
        RecordDragSample(dragCursorStart);
        dragging = true;
        didDrag = false;
        GhostLayer.CaptureMouse();
        e.Handled = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!GetCursorPos(out NativePoint cursor)) return;
        if (!dragging) return;
        double dx = cursor.X - dragCursorStart.X;
        double dy = cursor.Y - dragCursorStart.Y;
        if (Math.Abs(dx) + Math.Abs(dy) > 5) didDrag = true;
        Left = dragWindowStart.X + dx;
        Top = dragWindowStart.Y + dy;
        SampleDragVelocity(new Point(cursor.X, cursor.Y));
        RecordDragSample(new Point(cursor.X, cursor.Y));
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!dragging) return;
        dragging = false;
        GhostLayer.ReleaseMouseCapture();
        if (didDrag && FindNearbyDecor() is { } item)
        {
            flingVelocity = new Vector();
            PlayWithDecor(item);
            return;
        }
        if (!didDrag) Pet();
        else BeginFling();
    }

    private Rect GhostInteractionBounds()
    {
        const double margin = 24;
        return new Rect(Left - margin, Top - margin, Width + margin * 2, Height + margin * 2);
    }

    // Dropping the ghost on or near a placed decoration plays with it immediately, instead of
    // waiting on the ambient roam-AI chance in ChooseDestination.
    private DecorWindow? FindNearbyDecor() =>
        DecorWindow.PlacedItems.FirstOrDefault(item => GhostInteractionBounds().IntersectsWith(item.Bounds));

    /// <summary>Bringing the toy to the ghost (dragging a DecorWindow itself) works the same as
    /// bringing the ghost to the toy. Called by DecorWindow when the player drops it.</summary>
    internal bool TryPlayWithNearbyDecor(DecorWindow item)
    {
        if (resting || decorPlaySessionActive || item.IsPlaying) return false;
        if (!GhostInteractionBounds().IntersectsWith(item.Bounds)) return false;
        PlayWithDecor(item);
        return true;
    }

    private void Ghost_MouseLeave(object sender, MouseEventArgs e)
    {
        // Keep the current walking direction. A moving transparent window can emit MouseLeave repeatedly.
    }

    private void SampleDragVelocity(Point cursor)
    {
        double milliseconds = Math.Max(1, (DateTime.UtcNow - lastDragSampleAt).TotalMilliseconds);
        flingVelocity = new Vector((cursor.X - lastDragCursor.X) / milliseconds, (cursor.Y - lastDragCursor.Y) / milliseconds);
        lastDragCursor = cursor;
        lastDragSampleAt = DateTime.UtcNow;
    }

    private void BeginFling()
    {
        if (GetCursorPos(out NativePoint cursor) && new Point(cursor.X, cursor.Y) != lastDragCursor)
        {
            SampleDragVelocity(new Point(cursor.X, cursor.Y));
            RecordDragSample(new Point(cursor.X, cursor.Y));
        }
        SampleDragMotion();
        DateTime now = DateTime.UtcNow;
        bool isQuickFlick = (now - lastDragMotionAt).TotalMilliseconds <= 130
            && recentDragVelocity.Length >= .65;
        if (!isQuickFlick) { ChooseDestination(); return; }
        double launchSpeed = Math.Clamp(.45 + (recentDragVelocity.Length - .65) * 4.5, .45, 4.0);
        recentDragVelocity.Normalize();
        flingVelocity = recentDragVelocity * launchSpeed;
        ShowSpeech("휙—!");
    }

    private void SampleDragMotion()
    {
        if (!GetCursorPos(out NativePoint cursor)) return;
        Point current = new(cursor.X, cursor.Y);
        DateTime now = DateTime.UtcNow;
        double elapsed = Math.Max(1, (now - velocitySampleAt).TotalMilliseconds);
        if (elapsed < 12) return;
        Vector rawVelocity = (current - velocitySampleCursor) / elapsed;
        if (rawVelocity.Length > .01)
        {
            recentDragVelocity = recentDragVelocity * .28 + rawVelocity * .72;
            lastDragMotionAt = now;
        }
        else recentDragVelocity *= .65;
        velocitySampleCursor = current;
        velocitySampleAt = now;
    }

    private void RecordDragSample(Point cursor)
    {
        DateTime now = DateTime.UtcNow;
        dragSamples.Add(new DragSample(cursor, now));
        dragSamples.RemoveAll(item => (now - item.At).TotalMilliseconds > 220);
    }

    private void MoveFling()
    {
        Rect area = VirtualDesktopBounds;
        double minX = area.Left + 12;
        double minY = area.Top + 12;
        double maxX = Math.Max(minX, area.Right - Width - 12);
        double maxY = Math.Max(minY, area.Bottom - Height - 12);
        double nextX = Left + flingVelocity.X * 16;
        double nextY = Top + flingVelocity.Y * 16;
        if (nextX < minX || nextX > maxX)
        {
            nextX = Math.Clamp(nextX, minX, maxX);
            flingVelocity.X *= -.42;
        }
        if (nextY < minY || nextY > maxY)
        {
            nextY = Math.Clamp(nextY, minY, maxY);
            flingVelocity.Y *= -.42;
        }
        MainGhostArtwork.SetGaze(flingVelocity.X >= 0 ? 14 : -14, 0);
        Left = nextX;
        Top = nextY;
        flingVelocity *= .90;
        if (flingVelocity.Length < .05)
        {
            flingVelocity = new Vector();
            ChooseDestination();
        }
    }

    private void Pet()
    {
        affection = Math.Min(99, affection + 1);
        Speak();
        PopHeart();
        RefreshFarm();
    }

    private void Feed_Click(object sender, RoutedEventArgs e)
    {
        FeedGhost();
    }

    private void Train_Click(object sender, RoutedEventArgs e)
    {
        TrainGhost();
    }

    private void Battle_Click(object sender, RoutedEventArgs e) => Battle();

    private void Battle() => OpenBattle(null);

    private void Rest_Click(object sender, RoutedEventArgs e)
    {
        if (resting)
        {
            WakeAllFromSleep();
            return;
        }
        energy = Math.Min(100, energy + 18);
        EnterSleepMode();
    }

    private void EnterSleepMode()
    {
        resting = true;
        dragging = false;
        flingVelocity = new Vector();
        MainGhostArtwork.IsSleeping = true;
        ((Storyboard)FindResource("FloatStoryboard")).Stop(this);
        FloatTransform.Y = 0;
        SwayTransform.Angle = 0;

        Rect area = VirtualDesktopBounds;
        Left = Math.Max(area.Left + 12, area.Right - Width - 24);
        Top = Math.Max(area.Top + 12, area.Bottom - Height - 18);
        for (int index = 0; index < summonedGhosts.Count; index++)
        {
            SummonedGhostWindow friend = summonedGhosts[index];
            friend.EnterSleepMode(GetFriendSleepSpot(index, friend.Width, friend.Height));
        }

        ShowSpeech("Zzz… 건드리면 깨어날게.");
        UpdateInfo();
        farmWindow?.RefreshState();
    }

    private Point GetFriendSleepSpot(int index, double friendWidth, double friendHeight)
    {
        Rect area = VirtualDesktopBounds;
        const double gap = 12;
        int column = index % 2;
        int row = index / 2;
        double x = Left - gap - friendWidth * (column + 1) - gap * column;
        double y = area.Bottom - friendHeight - 18 - row * (friendHeight + gap);
        return new Point(
            Math.Clamp(x, area.Left + 8, Math.Max(area.Left + 8, area.Right - friendWidth - 8)),
            Math.Clamp(y, area.Top + 8, Math.Max(area.Top + 8, area.Bottom - friendHeight - 8)));
    }

    private void WakeMainFromSleep(bool showMessage = true)
    {
        if (!resting) return;
        resting = false;
        MainGhostArtwork.IsSleeping = false;
        ((Storyboard)FindResource("FloatStoryboard")).Begin(this, true);
        ChooseDestination();
        if (showMessage) ShowSpeech("잘 잤다!");
        UpdateInfo();
        farmWindow?.RefreshState();
    }

    private void WakeAllFromSleep()
    {
        WakeMainFromSleep(showMessage: false);
        foreach (SummonedGhostWindow friend in summonedGhosts) friend.WakeUp();
        ShowSpeech("다 같이 잘 잤다!");
    }

    private void ChangeGhost_Click(object sender, RoutedEventArgs e)
    {
        int[] available = unlockedGhosts.OrderBy(i => i).ToArray();
        int position = Array.IndexOf(available, ghostIndex);
        ghostIndex = available[(position + 1) % available.Length];
        ApplyGhostStyle();
    }

    private void Status_Click(object sender, RoutedEventArgs e) =>
        ShowSpeech($"Lv.{level}  ♥{affection}  ⚡{energy}");

    private void Talk_Click(object sender, RoutedEventArgs e) => Speak();

    private void SizeHuge_Click(object sender, RoutedEventArgs e) => SetGhostSize("초대형", 4.0);
    private void SizeLarge_Click(object sender, RoutedEventArgs e) => SetGhostSize("상", 1.0);
    private void SizeMedium_Click(object sender, RoutedEventArgs e) => SetGhostSize("중", .6);
    private void SizeSmall_Click(object sender, RoutedEventArgs e) => SetGhostSize("하", .25);

    private void SetGhostSize(string label, double scale)
    {
        globalGhostSize = scale;
        SizeTransform.ScaleX = SizeTransform.ScaleY = globalGhostSize;
        ResizeWidgetForGhostScale(scale);
        foreach (SummonedGhostWindow friend in summonedGhosts.ToArray()) friend.SetGhostScale(globalGhostSize);
        ShowSpeech($"크기 · {label}");
    }

    private void ResizeWidgetForGhostScale(double scale)
    {
        const double baseWidth = 250;
        const double baseHeight = 278;
        if (scale <= 1)
        {
            Width = baseWidth;
            Height = baseHeight;
            Root.Width = baseWidth;
            Root.Height = baseHeight;
            Canvas.SetLeft(GhostLayer, 25);
            Canvas.SetTop(GhostLayer, 53);
            Canvas.SetLeft(Speech, 8);
            Canvas.SetTop(Speech, 4);
            KeepWidgetInsideDesktop();
            return;
        }

        // The pet art is exactly four times the large scale.  The transparent host grows
        // only enough to contain it; keeping the root empty outside the ghost avoids a giant
        // invisible click target over the desktop.
        Width = 1000;
        Height = 1000;
        Root.Width = Width;
        Root.Height = Height;
        Canvas.SetLeft(GhostLayer, 400);
        Canvas.SetTop(GhostLayer, 420);
        Canvas.SetLeft(Speech, 380);
        Canvas.SetTop(Speech, 52);
        KeepWidgetInsideDesktop();
    }

    private void KeepWidgetInsideDesktop()
    {
        Rect area = VirtualDesktopBounds;
        Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
        Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenFarm_Click(object sender, RoutedEventArgs e)
    {
        if (farmWindow is { IsVisible: true })
        {
            farmWindow.Activate();
            return;
        }

        farmWindow = new FarmWindow(this);
        farmWindow.Closed += (_, _) => farmWindow = null;
        Rect area = SystemParameters.WorkArea;
        double preferredLeft = Left - farmWindow.Width - 12;
        farmWindow.Left = preferredLeft >= area.Left ? preferredLeft : Math.Min(area.Right - farmWindow.Width - 16, Left + Width + 12);
        farmWindow.Top = Math.Clamp(Top - 150, area.Top + 12, area.Bottom - farmWindow.Height - 12);
        farmWindow.Show();
    }

    private void SummonCaughtGhosts_Click(object sender, RoutedEventArgs e)
    {
        int[] candidates = unlockedGhosts.Where(index => index != ghostIndex).OrderBy(index => index).ToArray();
        if (candidates.Length == 0)
        {
            ShowSpeech("먼저 새로운 유령을 포획해봐!");
            return;
        }

        if (summonSelectionWindow is { IsVisible: true })
        {
            summonSelectionWindow.Activate();
            return;
        }

        HashSet<int> alreadySummoned = summonedGhosts
            .Where(window => window.IsVisible)
            .Select(window => window.SpeciesIndex)
            .ToHashSet();
        summonSelectionWindow = new SummonSelectionWindow(this, candidates, alreadySummoned);
        summonSelectionWindow.Closed += (_, _) => summonSelectionWindow = null;
        summonSelectionWindow.Left = Math.Clamp(Left - 150, SystemParameters.WorkArea.Left + 12, SystemParameters.WorkArea.Right - summonSelectionWindow.Width - 12);
        summonSelectionWindow.Top = Math.Clamp(Top - 190, SystemParameters.WorkArea.Top + 12, SystemParameters.WorkArea.Bottom - summonSelectionWindow.Height - 12);
        summonSelectionWindow.Show();
    }

    internal void ApplySummonSelection(IEnumerable<int> selectedIndexes)
    {
        HashSet<int> desired = selectedIndexes
            .Where(index => unlockedGhosts.Contains(index) && index != ghostIndex)
            .ToHashSet();

        // Rebuild the companion group on every confirmation. This prevents an old window
        // from remaining underneath a freshly summoned ghost at the same spawn location.
        foreach (SummonedGhostWindow companion in summonedGhosts.ToArray()) companion.Close();
        summonedGhosts.Clear();

        int[] ordered = desired.OrderBy(index => index).ToArray();
        for (int position = 0; position < ordered.Length; position++)
        {
            int index = ordered[position];
            GhostSpecies ghost = ghosts[index];
            SummonedGhostWindow companion = new(index, ghost.Name, ghost.Color, ghost.Form, globalGhostSize,
                GetCompanionSpawnPoint(position, ordered.Length), GetCompanionExclusionBounds);
            companion.Closed += (_, _) => summonedGhosts.Remove(companion);
            summonedGhosts.Add(companion);
            companion.Show();
            if (resting) companion.EnterSleepMode(GetFriendSleepSpot(position, companion.Width, companion.Height));
        }

        ShowSpeech(desired.Count == 0 ? "이번엔 혼자 놀아볼게." : $"친구 {desired.Count}마리와 함께야!");
    }

    private static Point GetCompanionSpawnPoint(int position, int total)
    {
        Rect area = SystemParameters.WorkArea;
        int columns = Math.Max(2, (int)Math.Ceiling(Math.Sqrt(total)));
        int rows = Math.Max(1, (int)Math.Ceiling(total / (double)columns));
        int column = position % columns;
        int row = position / columns;
        const double ghostWidth = 135;
        const double ghostHeight = 154;
        double x = area.Left + Math.Max(0, area.Width - ghostWidth) * ((column + .5) / columns);
        double y = area.Top + Math.Max(0, area.Height - ghostHeight) * ((row + .5) / rows);
        return new Point(x, y);
    }

    private Rect GetCompanionExclusionBounds()
    {
        // Keep companion windows clear of both the main ghost and its expanding speech bubble.
        const double margin = 18;
        return new Rect(Left - margin, Top - margin, Width + margin * 2, Height + margin * 2);
    }

    private void DismissSummonedGhosts_Click(object sender, RoutedEventArgs e)
    {
        foreach (SummonedGhostWindow companion in summonedGhosts.ToArray()) companion.Close();
        summonedGhosts.Clear();
        ShowSpeech("친구들이 우리로 돌아갔어.");
    }
    private void ShowSpeech(string text)
    {
        SpeechText.Text = text;
        speechTimer.Stop();
        Speech.Visibility = Visibility.Visible;
        Fade(Speech, 1, 120);
        speechTimer.Start();
    }

    private void Speak()
    {
        GhostSpecies ghost = ghosts[ghostIndex];
        // 새 도감 유령도 이동 중 대사를 합니다. 대사 표가 아직 없는 유령 때문에
        // 위젯 전체가 종료되지 않도록, 개별 대사가 없으면 성격 기반 기본 대사를 씁니다.
        string[] lines = dialogueLines.TryGetValue(ghost.Name, out string[]? registeredLines)
            && registeredLines.Length > 0
            ? registeredLines
            :
            [
                $"Boo! 난 {ghost.Personality} 기분이야.",
                $"{ghost.Name}이(가) 둥실둥실 산책 중이에요."
            ];
        ShowSpeech(lines[random.Next(lines.Length)]);
    }

    private void HideSpeech()
    {
        speechTimer.Stop();
        Fade(Speech, 0, 260);
    }

    private static void Fade(UIElement element, double to, int milliseconds)
    {
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(milliseconds))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }

    private void Blink(object? sender, EventArgs e)
    {
        blinkTimer.Interval = TimeSpan.FromSeconds(2.2 + random.NextDouble() * 3.4);
        MainGhostArtwork.Blink();
    }

    private void Bounce()
    {
        DoubleAnimationUsingKeyFrames bounce = new() { Duration = TimeSpan.FromMilliseconds(420) };
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(0)));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.12, KeyTime.FromPercent(0.35)));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0.96, KeyTime.FromPercent(0.62)));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(1)));
        GrowthTransform.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
    }

    private void PopHeart()
    {
        TextBlock heart = new()
        {
            Text = "♥", Foreground = Brushes.Red, FontSize = 23, FontWeight = FontWeights.Bold,
            RenderTransform = new RotateTransform(random.Next(-12, 13))
        };
        Canvas.SetLeft(heart, 106 + random.Next(-18, 19));
        Canvas.SetTop(heart, 128);
        ParticleLayer.Children.Add(heart);
        DoubleAnimation rise = new(128, 78, TimeSpan.FromMilliseconds(720)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        DoubleAnimation vanish = new(1, 0, TimeSpan.FromMilliseconds(720)) { BeginTime = TimeSpan.FromMilliseconds(180) };
        vanish.Completed += (_, _) => ParticleLayer.Children.Remove(heart);
        heart.BeginAnimation(Canvas.TopProperty, rise);
        heart.BeginAnimation(OpacityProperty, vanish);
    }

    private void UpdateInfo() => Title = $"유령 농장 — {ghosts[ghostIndex].Name} Lv.{level}";

    internal FarmSnapshot GetFarmSnapshot() => new(
        ghosts[ghostIndex].Name, ghosts[ghostIndex].Type, ghosts[ghostIndex].Color, ghosts[ghostIndex].Form, ghosts[ghostIndex].Personality, level, experience,
        affection, energy, luna, orbs, wins, unlockedGhosts.Count, ghostIndex, captureReady, battleReady,
        DateTime.Now >= raiseReadyAt, Math.Max(0, (int)Math.Ceiling((raiseReadyAt - DateTime.Now).TotalSeconds)));

    internal IReadOnlyList<GhostRosterItem> GetRoster() => ghosts
        .Select((ghost, index) => (ghost, index))
        .Where(item => !item.ghost.IsBoss || unlockedGhosts.Contains(item.index))
        .Select(item => new GhostRosterItem(item.index, item.ghost.Name, item.ghost.Type, item.ghost.Color, item.ghost.Form,
            item.ghost.Rarity, unlockedGhosts.Contains(item.index), item.index == ghostIndex,
            unlockedGhosts.Contains(item.index) ? item.ghost.Name : "???",
            1,
            item.index == ghostIndex ? "#17131C" : "Transparent"))
        .ToArray();

    internal void OpenCaptureGame(FarmWindow panel)
    {
        if (!captureReady) panel.ShowResult("야생 유령 알림이 오면 포획 버튼이 활성화돼요.");
        else StartCaptureFromNotification();
    }

    internal void StartCaptureFromNotification()
    {
        captureReady = false;
        RefreshFarm();
        if (orbs <= 0)
        {
            farmWindow?.ShowResult("봉인구가 부족해요. 퀘스트·결투 보상을 모아봐요.");
            return;
        }
        SpendOrb();
        int[] capturable = Enumerable.Range(1, ghosts.Length - 1).Where(i => !ghosts[i].IsBoss).ToArray();
        int[] locked = capturable.Where(i => !unlockedGhosts.Contains(i)).ToArray();
        int candidate = locked.Length > 0 ? locked[random.Next(locked.Length)] : capturable[random.Next(capturable.Length)];
        CaptureGameWindow game = new(this, farmWindow, candidate);
        if (farmWindow is { IsVisible: true }) game.Owner = farmWindow;
        game.ShowDialog();
    }

    internal GhostSpecies GetSpecies(int index) => ghosts[index];

    internal int GetCaptureHitPoints(int index) => ghosts[index].Rarity switch
    {
        "보통" => 2,
        "희귀" => 3,
        "영웅" => 4,
        _ => 5
    };

    internal bool SpendOrb()
    {
        if (orbs <= 0) return false;
        orbs--;
        RefreshFarm();
        return true;
    }

    internal bool ResolveCapture(int index)
    {
        bool isNew = unlockedGhosts.Add(index);
        if (isNew)
        {
            ghostIndex = index;
            affection = Math.Min(99, affection + 5);
            ApplyGhostStyle();
        }
        else
        {
            luna += 25;
            ShowSpeech("다시 만났네!");
        }
        RefreshFarm();
        return true;
    }

    internal string FeedGhost()
    {
        WakeMainFromSleep(showMessage: false);
        if (DateTime.Now < raiseReadyAt)
        {
            int seconds = Math.Max(1, (int)Math.Ceiling((raiseReadyAt - DateTime.Now).TotalSeconds));
            return $"달과자는 {seconds / 60}분 {seconds % 60}초 뒤에 다시 줄 수 있어요.";
        }
        if (luna < 10) return "루나가 부족해요. 결투에서 루나를 모아봐요.";
        luna -= 10;
        // 달과자는 돌봄 행동입니다. 경험치가 아니라 지친 유령을 회복시키는 역할만 합니다.
        affection = Math.Min(99, affection + 8);
        energy = Math.Min(100, energy + 32);
        raiseReadyAt = DateTime.Now.AddMinutes(30);
        raiseTimer.Start();
        ShowSpeech("냠! 맛있다!");
        PopHeart();
        RefreshFarm();
        return $"달과자를 먹였어요. 기력 +32 · 친밀도 +8";
    }

    internal string TrainGhost()
    {
        WakeMainFromSleep(showMessage: false);
        if (energy < 12) return "기력이 부족해요. 먼저 쉬게 해주세요.";
        energy -= 12;
        affection = Math.Min(99, affection + 2);
        experience += 18;
        CheckLevelUp();
        ShowSpeech("훈련 완료!");
        Bounce();
        RefreshFarm();
        return "부유 훈련 완료! 경험치 +18 · 친밀도 +2 · 기력 -12";
    }

    internal void OpenBattle(FarmWindow? panel)
    {
        if (!battleReady)
        {
            panel?.ShowResult("결투 초대 알림이 오면 결투 버튼이 활성화돼요.");
            ShowSpeech("다음 결투 초대를 기다리자!");
            return;
        }
        StartBattleFromNotification();
    }

    internal void StartBattleFromNotification()
    {
        battleReady = false;
        RefreshFarm();
        if (energy < 15)
        {
            farmWindow?.ShowResult("기력이 부족해요. 휴식이나 달과자가 필요해요.");
            ShowSpeech("지금은 조금 피곤해…");
            return;
        }
        int? bossIndex = TryRollBossEncounter();
        duelsSinceBoss = bossIndex is null ? duelsSinceBoss + 1 : 0;
        int enemyIndex = bossIndex ?? PickWildEnemyIndex(ghostIndex);
        BattleWindow battle = new(this, farmWindow, enemyIndex);
        if (farmWindow is { IsVisible: true }) battle.Owner = farmWindow;
        battle.ShowDialog();
    }

    internal int PickWildEnemyIndex(int excludeIndex)
    {
        int[] candidates = Enumerable.Range(0, ghosts.Length).Where(i => i != excludeIndex && !ghosts[i].IsBoss).ToArray();
        return candidates[random.Next(candidates.Length)];
    }

    private int? TryRollBossEncounter()
    {
        int[] eligible = Enumerable.Range(0, ghosts.Length)
            .Where(i => ghosts[i].IsBoss && !unlockedGhosts.Contains(i))
            .Where(i => { (int reqLevel, int reqWins) = BattleTypes.BossRequirement(ghosts[i].Form); return level >= reqLevel && wins >= reqWins; })
            .ToArray();
        if (eligible.Length == 0) return null;
        double chance = Math.Min(1.0, 0.10 + 0.06 * duelsSinceBoss);
        return random.NextDouble() < chance ? eligible[random.Next(eligible.Length)] : null;
    }

    internal void StartQuestFromNotification() => new QuestWindow(this).Show();

    internal string CompleteQuest(string questName, int reward)
    {
        luna += reward;
        affection = Math.Min(99, affection + 2);
        experience += 4;
        CheckLevelUp();
        ShowSpeech($"{questName} 끝! 고마워!");
        RefreshFarm();
        return $"퀘스트 완료! 루나 +{reward} · 경험치 +4";
    }

    internal string DrawGhostGacha()
    {
        const int price = 100;
        if (luna < price) return $"루나가 부족해요. 뽑기에는 {price}루나가 필요해요.";
        luna -= price;
        double roll = random.NextDouble();
        string rarity = roll < .62 ? "보통" : roll < .88 ? "희귀" : roll < .98 ? "영웅" : "전설";
        int[] pool = ghosts.Select((ghost, index) => (ghost, index))
            .Where(item => item.ghost.Rarity == rarity && (!item.ghost.IsBoss || unlockedGhosts.Contains(item.index)))
            .Select(item => item.index).ToArray();
        int index = pool[random.Next(pool.Length)];
        GhostSpecies prize = ghosts[index];
        if (unlockedGhosts.Add(index))
        {
            ghostIndex = index;
            ApplyGhostStyle();
            RefreshFarm();
            return $"뽑기 성공! {prize.Name} · {prize.Rarity} 유령을 만났어요!";
        }
        luna += 30;
        RefreshFarm();
        return $"{prize.Name}이(가) 또 나왔어요. 중복 보상 루나 +30";
    }

    internal int Luna => luna;
    internal bool OwnsDecor(int decorIndex) => ownedDecor.Contains(decorIndex);
    internal bool IsDecorPlaced(int decorIndex) => DecorWindow.PlacedItems.Any(item => item.DecorIndex == decorIndex);

    internal string PurchaseDecor(int decorIndex)
    {
        DecorSpecies species = DecorCatalog.Items[decorIndex];
        if (ownedDecor.Contains(decorIndex)) return "이미 가지고 있어요.";
        if (luna < species.Price) return $"루나가 부족해요. {species.Name}에는 {species.Price}루나가 필요해요.";
        luna -= species.Price;
        ownedDecor.Add(decorIndex);
        PlaceDecor(decorIndex);
        return $"{species.Name}을(를) 구매해서 데스크톱에 놓았어요!";
    }

    internal bool PlaceDecor(int decorIndex)
    {
        if (!ownedDecor.Contains(decorIndex) || IsDecorPlaced(decorIndex)) return false;
        new DecorWindow(this, decorIndex, ChooseDecorSpawnPoint(), SaveState).Show();
        SaveState();
        return true;
    }

    internal bool RemoveDecorPlacement(int decorIndex)
    {
        DecorWindow? window = DecorWindow.PlacedItems.FirstOrDefault(item => item.DecorIndex == decorIndex);
        if (window is null) return false;
        window.Close();
        return true;
    }

    private Point ChooseDecorSpawnPoint()
    {
        Rect area = VirtualDesktopBounds;
        double minX = area.Left + 8;
        double minY = area.Top + 8;
        double maxX = Math.Max(minX + 1, area.Right - DecorWindow.Size - 8);
        double maxY = Math.Max(minY + 1, area.Bottom - DecorWindow.Size - 8);
        Point fallback = new(minX + random.NextDouble() * (maxX - minX), minY + random.NextDouble() * (maxY - minY));
        for (int attempt = 0; attempt < 18; attempt++)
        {
            Point candidate = new(minX + random.NextDouble() * (maxX - minX), minY + random.NextDouble() * (maxY - minY));
            Rect candidateBounds = new(candidate.X, candidate.Y, DecorWindow.Size, DecorWindow.Size);
            if (!candidateBounds.IntersectsWith(GetCompanionExclusionBounds())
                && DecorWindow.PlacedItems.All(item => !candidateBounds.IntersectsWith(item.Bounds)))
                return candidate;
        }
        return fallback;
    }

    private void RestorePlacedDecor()
    {
        foreach (DecorPlacementSave placement in pendingDecorPlacements)
        {
            if (IsDecorPlaced(placement.DecorIndex)) continue;
            new DecorWindow(this, placement.DecorIndex, new Point(placement.X, placement.Y), SaveState).Show();
        }
        pendingDecorPlacements = [];
    }

    internal void OpenDecorShop()
    {
        if (decorShopWindow is { IsVisible: true }) { decorShopWindow.Activate(); return; }
        decorShopWindow = new DecorShopWindow(this);
        decorShopWindow.Closed += (_, _) => decorShopWindow = null;
        Rect area = SystemParameters.WorkArea;
        decorShopWindow.Left = Math.Clamp(Left - decorShopWindow.Width - 12, area.Left + 12, area.Right - decorShopWindow.Width - 12);
        decorShopWindow.Top = Math.Clamp(Top - 150, area.Top + 12, area.Bottom - decorShopWindow.Height - 12);
        decorShopWindow.Show();
    }

    private void OpenDecorShop_Click(object sender, RoutedEventArgs e) => OpenDecorShop();

    internal BattleMove[] GetMoves(GhostSpecies ghost) => ghost.Form switch
    {
        "Pumpkin" => [new("호박 굴리기", 10, Element.Fire, EffectKind.None, 0, 6), new("으스스 빛", 14, Element.Fire, EffectKind.None, 0, 4), new("할로윈 폭죽", 18, Element.Fire, EffectKind.Burn, 5, 2)],
        "Pirate" => [new("유령 포탄", 11, Element.Water, EffectKind.None, 0, 6), new("닻 던지기", 15, Element.Water, EffectKind.None, 0, 4), new("선장 돌진", 18, Element.Water, EffectKind.Paralyze, 30, 2)],
        "Candle" => [new("심지 불꽃", 10, Element.Fire, EffectKind.None, 0, 6), new("따뜻한 왁스", 9, Element.Fire, EffectKind.Shield, 50, 3), new("밤의 등불", 15, Element.Fire, EffectKind.Burn, 5, 4)],
        "Moss" => [new("덩굴 휘감기", 10, Element.Nature, EffectKind.None, 0, 6), new("이끼 방패", 9, Element.Nature, EffectKind.Shield, 45, 3), new("숲의 한숨", 12, Element.Nature, EffectKind.Heal, 12, 3)],
        "Moon" => [new("달가루", 11, Element.Light, EffectKind.None, 0, 6), new("초승 베기", 15, Element.Light, EffectKind.None, 0, 4), new("새벽 소원", 18, Element.Light, EffectKind.Paralyze, 30, 2)],
        "Star" => [new("반짝 가루", 10, Element.Light, EffectKind.None, 0, 6), new("별똥별", 15, Element.Light, EffectKind.None, 0, 4), new("소원 폭발", 18, Element.Light, EffectKind.Burn, 5, 2)],
        "Cloud" => [new("몽실 펀치", 10, Element.Light, EffectKind.None, 0, 6), new("보슬비", 9, Element.Light, EffectKind.Heal, 10, 3), new("천둥 구름", 17, Element.Light, EffectKind.Paralyze, 30, 3)],
        "Shadow" => [new("그림자 숨기", 9, Element.Dark, EffectKind.Shield, 45, 3), new("어둠 손길", 15, Element.Dark, EffectKind.None, 0, 4), new("밤의 장막", 18, Element.Dark, EffectKind.Paralyze, 30, 2)],
        "Rose" => [new("꽃잎 바람", 10, Element.Nature, EffectKind.None, 0, 6), new("가시 찌르기", 15, Element.Nature, EffectKind.Burn, 5, 4), new("장미 폭풍", 18, Element.Nature, EffectKind.None, 0, 2)],
        "Bat" => [new("박쥐 소리", 11, Element.Dark, EffectKind.Paralyze, 25, 4), new("날개 베기", 15, Element.Dark, EffectKind.None, 0, 4), new("밤 급강하", 18, Element.Dark, EffectKind.None, 0, 2)],
        "Jelly" => [new("말랑 촉수", 10, Element.Water, EffectKind.None, 0, 6), new("물방울 탄", 14, Element.Water, EffectKind.None, 0, 4), new("해류 춤", 17, Element.Water, EffectKind.Heal, 10, 3)],
        "Sand" => [new("모래 알갱이", 10, Element.Nature, EffectKind.None, 0, 6), new("사막 바람", 14, Element.Nature, EffectKind.Paralyze, 25, 4), new("고대의 모래", 18, Element.Nature, EffectKind.None, 0, 2)],
        "Witch" => [new("빗자루 휙", 11, Element.Dark, EffectKind.None, 0, 6), new("수상한 물약", 15, Element.Dark, EffectKind.Heal, 10, 3), new("별빛 주문", 19, Element.Dark, EffectKind.Paralyze, 30, 2)],
        "Legend" => [new("달의 숨결", 12, Element.Light, EffectKind.None, 0, 6), new("은하 고리", 16, Element.Light, EffectKind.Paralyze, 30, 3), new("만월 심판", 20, Element.Light, EffectKind.None, 0, 2)],
        "BossHearth" => [new("잉걸불 던지기", 14, Element.Fire, EffectKind.None, 0, 6), new("화염 소용돌이", 18, Element.Fire, EffectKind.Burn, 7, 4), new("숯불 방벽", 12, Element.Fire, EffectKind.Shield, 50, 3)],
        "BossMoss" => [new("덩굴 채찍", 15, Element.Nature, EffectKind.None, 0, 6), new("가시 감옥", 19, Element.Nature, EffectKind.Paralyze, 35, 4), new("숲의 치유", 10, Element.Nature, EffectKind.Heal, 16, 3)],
        "BossMist" => [new("물보라 채찍", 16, Element.Water, EffectKind.None, 0, 6), new("역류 소용돌이", 20, Element.Water, EffectKind.Paralyze, 35, 4), new("치유의 이슬", 10, Element.Water, EffectKind.Heal, 16, 3)],
        "BossDawn" => [new("별빛 화살", 16, Element.Light, EffectKind.None, 0, 6), new("유성 낙하", 20, Element.Light, EffectKind.Paralyze, 35, 4), new("여명의 축복", 10, Element.Light, EffectKind.Heal, 16, 3)],
        "BossGate" => [new("그림자 손아귀", 16, Element.Dark, EffectKind.None, 0, 6), new("칠흑의 낙인", 20, Element.Dark, EffectKind.Burn, 8, 4), new("밤의 장막", 13, Element.Dark, EffectKind.Shield, 55, 3)],
        "BossVolcano" => [new("마그마 해일", 20, Element.Fire, EffectKind.None, 0, 6), new("균열 폭발", 25, Element.Fire, EffectKind.Burn, 10, 4), new("용암 갑주", 15, Element.Fire, EffectKind.Shield, 60, 3)],
        "BossStorm" => [new("해일 강타", 21, Element.Water, EffectKind.None, 0, 6), new("소용돌이 감금", 25, Element.Water, EffectKind.Paralyze, 45, 4), new("폭풍 눈", 14, Element.Water, EffectKind.Heal, 20, 3)],
        "BossTree" => [new("뿌리 강타", 21, Element.Nature, EffectKind.None, 0, 6), new("고목의 저주", 25, Element.Nature, EffectKind.Burn, 10, 4), new("대지 갑주", 15, Element.Nature, EffectKind.Shield, 60, 3)],
        "BossCelestial" => [new("심판의 빛", 27, Element.Light, EffectKind.None, 0, 5), new("축복의 사슬", 22, Element.Light, EffectKind.Paralyze, 45, 4), new("천상의 가호", 15, Element.Light, EffectKind.Shield, 60, 3)],
        "BossVoid" => [new("적막의 낫", 29, Element.Dark, EffectKind.None, 0, 5), new("심연의 속박", 23, Element.Dark, EffectKind.Paralyze, 50, 4), new("위엄의 갑주", 16, Element.Dark, EffectKind.Shield, 65, 3)],
        _ => BattleTypes.BuildGenericMoveset(BattleTypes.ElementOf(ghost))
    };

    internal string CompleteBattle(bool victory, int enemyIndex)
    {
        energy -= 15;
        GhostSpecies foe = ghosts[enemyIndex];
        if (!victory)
        {
            winStreak = 0;
            affection = Math.Max(0, affection - 8);
            pendingBattleCooldownMinutes = 4;
            ShowSpeech("다음엔 이길 거야!");
            RefreshFarm();
            return "아쉽게 졌어요. 친밀도가 조금 떨어졌어요. 유령을 키우고 다시 도전해봐요.";
        }

        wins++;
        winStreak++;
        int streakBonus = Math.Min(winStreak - 1, 10) * 2;
        int lunaGain = (int)Math.Round(20 * (1 + foe.Difficulty)) + streakBonus;
        int expGain = (int)Math.Round(18 * (1 + foe.Difficulty));
        luna += lunaGain;
        experience += expGain;
        if (random.NextDouble() < Math.Min(0.85, 0.25 + foe.Difficulty * 0.35)) orbs++;
        affection = Math.Min(99, affection + 4);
        bool firstBossClear = foe.IsBoss && unlockedGhosts.Add(enemyIndex);
        if (firstBossClear)
        {
            ghostIndex = enemyIndex;
            ApplyGhostStyle();
            luna += 80;
            experience += 60;
            orbs += 2;
        }
        CheckLevelUp();
        ShowSpeech("이겼다! ⚔");
        Bounce();
        RefreshFarm();
        return firstBossClear
            ? $"🏆 보스를 무너뜨렸다! {foe.Name}이(가) 도감에 새겨졌습니다. 이제 뽑기에서도 만날 수 있어요!"
            : $"승리! 루나 +{lunaGain} · 경험치 +{expGain}";
    }

    internal string FarmRest()
    {
        energy = Math.Min(100, energy + 30);
        if (!resting) EnterSleepMode();
        RefreshFarm();
        return "구석 잠자리에서 쉬는 중이에요. 기력 +30";
    }

    internal void SelectGhost(int index)
    {
        if (!unlockedGhosts.Contains(index)) return;
        ghostIndex = index;
        ApplyGhostStyle();
        RefreshFarm();
    }

    private void CheckLevelUp()
    {
        while (experience >= level * 25)
        {
            experience -= level * 25;
            level++;
            ShowSpeech($"레벨 {level}!");
        }
        GrowthTransform.ScaleX = GrowthTransform.ScaleY = Math.Min(1.08, 1 + (level - 1) * 0.012);
    }

    private void ApplyGhostStyle()
    {
        MainGhostArtwork.ColorCode = ghosts[ghostIndex].Color;
        MainGhostArtwork.Form = ghosts[ghostIndex].Form;
        Title = $"유령 농장 — {ghosts[ghostIndex].Name}";
        ShowSpeech($"{ghosts[ghostIndex].Name} 등장!");
        UpdateInfo();
    }

    private void RefreshFarm()
    {
        UpdateInfo();
        SaveState();
        farmWindow?.RefreshState();
    }

    private string SavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GhostFarmWidget", "save.json");

    private void SaveState()
    {
        try
        {
            string? directory = Path.GetDirectoryName(SavePath);
            if (directory is not null) Directory.CreateDirectory(directory);
            GameSave save = new(level, experience, affection, energy, luna, orbs, wins, ghostIndex, unlockedGhosts.OrderBy(i => i).ToArray(), winStreak, duelsSinceBoss, raiseReadyAt.Ticks,
                ownedDecor.OrderBy(i => i).ToArray(),
                DecorWindow.PlacedItems.Select(w => new DecorPlacementSave(w.DecorIndex, w.Left, w.Top)).ToArray());
            File.WriteAllText(SavePath, JsonSerializer.Serialize(save));
        }
        catch { /* The widget remains playable when storage is unavailable. */ }
    }

    private void LoadState()
    {
        try
        {
            if (!File.Exists(SavePath)) return;
            GameSave? save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(SavePath));
            if (save is null) return;
            level = Math.Max(1, save.Level);
            experience = Math.Max(0, save.Experience);
            affection = Math.Clamp(save.Affection, 0, 99);
            energy = Math.Clamp(save.Energy, 0, 100);
            luna = Math.Max(0, save.Luna);
            orbs = Math.Max(0, save.Orbs);
            wins = Math.Max(0, save.Wins);
            winStreak = Math.Max(0, save.WinStreak);
            duelsSinceBoss = Math.Max(0, save.DuelsSinceBoss);
            unlockedGhosts.Clear();
            unlockedGhosts.Add(0);
            foreach (int index in save.UnlockedGhosts.Where(i => i >= 0 && i < ghosts.Length)) unlockedGhosts.Add(index);
            ghostIndex = unlockedGhosts.Contains(save.GhostIndex) ? save.GhostIndex : 0;
            raiseReadyAt = new DateTime(save.RaiseReadyAtTicks);
            ownedDecor.Clear();
            foreach (int index in (save.OwnedDecor ?? []).Where(i => i >= 0 && i < DecorCatalog.Items.Length)) ownedDecor.Add(index);
            pendingDecorPlacements = (save.PlacedDecor ?? [])
                .Where(p => p.DecorIndex >= 0 && p.DecorIndex < DecorCatalog.Items.Length).ToArray();
            ApplyGhostStyle();
        }
        catch { /* Ignore malformed saves and start from safe defaults. */ }
    }

    private void SavePreview()
    {
        RenderTargetBitmap bitmap = new((int)Root.ActualWidth, (int)Root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(Root);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "widget-preview.png"));
        encoder.Save(stream);
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        trayIcon?.Dispose();
        farmWindow?.Close();
        summonSelectionWindow?.Close();
        decorShopWindow?.Close();
        foreach (SummonedGhostWindow companion in summonedGhosts.ToArray()) companion.Close();
        movementTimer.Stop();
        blinkTimer.Stop();
        speechTimer.Stop();
        contentTimer.Stop();
        captureTimer.Stop();
        battleTimer.Stop();
        raiseTimer.Stop();
        SaveState();
        // DecorWindow.PlacedItems must still reflect what's on screen when SaveState() runs above,
        // so close them only after saving, not folded into the cleanup block with the other windows.
        foreach (DecorWindow item in DecorWindow.PlacedItems.ToArray()) item.Close();
        Application.Current.Shutdown();
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    internal static Rect VirtualDesktopBounds => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);

    private readonly record struct DragSample(Point Position, DateTime At);
}

internal readonly record struct FarmSnapshot(
    string Name, string Type, string Color, string Form, string Personality, int Level, int Experience, int Affection, int Energy,
    int Luna, int Orbs, int Wins, int UnlockedCount, int SelectedIndex, bool CaptureReady, bool BattleReady,
    bool RaiseReady, int RaiseSeconds);

internal readonly record struct GhostRosterItem(
    int Index, string Name, string Type, string Color, string Form, string Rarity, bool Unlocked, bool Selected,
    string DisplayName, double Opacity, string BorderColor);

internal readonly record struct GhostSpecies(
    string Name, string Type, string Color, string Rarity, double Difficulty, string Form, string Personality, bool IsBoss = false);

internal sealed record GameSave(
    int Level, int Experience, int Affection, int Energy, int Luna, int Orbs, int Wins,
    int GhostIndex, int[] UnlockedGhosts, int WinStreak = 0, int DuelsSinceBoss = 0, long RaiseReadyAtTicks = 0,
    int[]? OwnedDecor = null, DecorPlacementSave[]? PlacedDecor = null);
