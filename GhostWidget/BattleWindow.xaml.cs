using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace GhostWidget;

public partial class BattleWindow : Window
{
    private readonly MainWindow companion;
    private readonly FarmWindow? panel;
    private readonly Random random = new();
    private readonly int enemyIndex;
    private readonly GhostSpecies player;
    private readonly GhostSpecies enemy;
    private readonly Element playerElement;
    private readonly Element enemyElement;
    private readonly BattleMove[] playerMoves;
    private readonly BattleMove[] enemyMoves;
    private readonly int[] playerUsesRemaining;
    private readonly int[] enemyUsesRemaining;
    private readonly SideStatus playerStatus = new();
    private readonly SideStatus enemyStatus = new();
    private int playerHp;
    private int enemyHp;
    private readonly int playerMaxHp;
    private readonly int enemyMaxHp;
    private bool finished;
    private bool awaitingPlayerMove;
    private bool playerActsFirstThisRound;
    private bool enemyEnraged;

    internal BattleWindow(MainWindow companion, FarmWindow? panel, int enemyIndex)
    {
        InitializeComponent();
        this.companion = companion;
        this.panel = panel;
        this.enemyIndex = enemyIndex;
        FarmSnapshot snapshot = companion.GetFarmSnapshot();
        player = companion.GetSpecies(snapshot.SelectedIndex);
        enemy = companion.GetSpecies(enemyIndex);
        playerElement = BattleTypes.ElementOf(player);
        enemyElement = BattleTypes.ElementOf(enemy);
        playerMoves = companion.GetMoves(player);
        enemyMoves = companion.GetMoves(enemy);
        playerUsesRemaining = playerMoves.Select(move => move.Uses).ToArray();
        enemyUsesRemaining = enemyMoves.Select(move => move.Uses).ToArray();
        playerMaxHp = playerHp = 38 + snapshot.Level * 4;
        enemyMaxHp = enemyHp = 35 + (int)(enemy.Difficulty * 28) + (enemy.IsBoss ? 40 : 0);
        PlayerArt.ColorCode = player.Color; PlayerArt.Form = player.Form;
        EnemyArt.ColorCode = enemy.Color; EnemyArt.Form = enemy.Form;
        PlayerNameText.Text = $"{player.Name}  Lv.{snapshot.Level}";
        EnemyNameText.Text = enemy.IsBoss ? enemy.Name : $"야생 {enemy.Name}";
        if (enemy.IsBoss)
        {
            HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x14, 0x38));
            HeaderTitleText.Text = "⚠ 보스 결투";
            HeaderSubtitleText.Text = $"{enemy.Name}이(가) 나타났다! 신중하게 기술을 고르세요";
        }
        for (int index = 0; index < playerMoves.Length; index++)
        {
            Button button = new() { Style = (Style)FindResource("MoveButton"), Tag = index };
            button.Click += Move_Click;
            MoveGrid.Children.Add(button);
        }
        RefreshHealth();
        RefreshStatusIcons();
        BeginRound();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!Environment.GetCommandLineArgs().Any(a => a is "--battle-preview" || a.StartsWith("--boss-preview", StringComparison.Ordinal))) return;
        await Task.Delay(300);
        RenderTargetBitmap bitmap = new((int)BattleRoot.ActualWidth, (int)BattleRoot.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(BattleRoot);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "battle-preview.png"));
        encoder.Save(stream);
        Close();
        companion.Close();
    }

    private void BeginRound()
    {
        if (finished) return;
        playerActsFirstThisRound = RollTurnOrder();
        if (playerActsFirstThisRound)
        {
            TurnText.Text = "⚡ 내가 선공!";
            awaitingPlayerMove = true;
            RefreshMoveButtons();
            return;
        }
        TurnText.Text = $"⚡ {enemy.Name} 선공!";
        awaitingPlayerMove = false;
        RefreshMoveButtons();
        ResolveEnemyOpeningTurn();
    }

    private async void ResolveEnemyOpeningTurn()
    {
        await Task.Delay(650);
        if (finished) return;
        bool ko = RunTurn(isPlayer: false);
        if (finished || ko) return;
        await Task.Delay(420);
        awaitingPlayerMove = true;
        TurnText.Text = "내 차례";
        RefreshMoveButtons();
    }

    private bool RollTurnOrder()
    {
        double playerSpeed = BattleTypes.SpeedOf(player) + random.Next(-10, 11);
        double enemySpeed = BattleTypes.SpeedOf(enemy) + random.Next(-10, 11);
        return playerSpeed >= enemySpeed;
    }

    private async void Move_Click(object sender, RoutedEventArgs e)
    {
        if (finished || !awaitingPlayerMove || sender is not Button { Tag: int index } || playerUsesRemaining[index] <= 0) return;
        awaitingPlayerMove = false;
        RefreshMoveButtons();
        bool ko = RunTurn(isPlayer: true, index);
        if (finished || ko) return;
        if (playerActsFirstThisRound)
        {
            TurnText.Text = $"{enemy.Name}의 차례";
            await Task.Delay(520);
            if (finished) return;
            bool koEnemy = RunTurn(isPlayer: false);
            if (finished || koEnemy) return;
        }
        if (EndOfRoundStatusTicks()) return;
        TurnText.Text = "다음 라운드…";
        await Task.Delay(360);
        BeginRound();
    }

    /// <returns>True if this action ended the duel (a side reached 0 HP).</returns>
    private bool RunTurn(bool isPlayer, int? chosenIndex = null)
    {
        BattleMove[] moves = isPlayer ? playerMoves : enemyMoves;
        int[] uses = isPlayer ? playerUsesRemaining : enemyUsesRemaining;
        SideStatus attackerStatus = isPlayer ? playerStatus : enemyStatus;
        SideStatus defenderStatus = isPlayer ? enemyStatus : playerStatus;
        Element defenderElement = isPlayer ? enemyElement : playerElement;
        string attackerName = isPlayer ? player.Name : enemy.Name;
        string defenderName = isPlayer ? enemy.Name : player.Name;

        if (attackerStatus.Paralyzed)
        {
            attackerStatus.Paralyzed = false;
            RefreshStatusIcons();
            BattleLogText.Text = $"{attackerName}이(가) 마비되어 움직이지 못했다!";
            return false;
        }

        int index = chosenIndex ?? BattleTypes.ChooseEnemyMoveIndex(moves, uses, enemyHp, enemyMaxHp, playerElement, random);
        if (index < 0) index = Array.FindIndex(uses, remaining => remaining > 0);
        BattleMove move = moves[index];
        uses[index]--;

        double multiplier = BattleTypes.GetMultiplier(move.Element, defenderElement);
        int dealt = (int)Math.Round((move.Power + random.Next(0, 7)) * multiplier);
        if (!isPlayer && enemyEnraged) dealt = (int)Math.Round(dealt * 1.2);
        if (defenderStatus.Shielded)
        {
            dealt = (int)Math.Round(dealt * (1 - defenderStatus.ShieldReduction));
            defenderStatus.Shielded = false;
        }
        dealt = Math.Max(1, dealt);

        if (isPlayer) enemyHp = Math.Max(0, enemyHp - dealt); else playerHp = Math.Max(0, playerHp - dealt);

        string effectLine = multiplier > 1.0 ? "\n효과는 굉장했다!" : multiplier < 1.0 ? "\n효과가 별로인 듯하다…" : "";
        BattleLogText.Text = $"{attackerName}의 {move.Name}! {defenderName}에게 {dealt} 피해!{effectLine}";
        RefreshHealth();
        PlayHitReaction(isPlayer, multiplier > 1.0);
        PopDamageNumber(dealt, isPlayer);

        switch (move.Effect)
        {
            case EffectKind.Burn:
                defenderStatus.BurnTurns = 2;
                defenderStatus.BurnMagnitude = move.EffectMagnitude;
                break;
            case EffectKind.Paralyze:
                if (random.NextDouble() * 100 < move.EffectMagnitude) defenderStatus.Paralyzed = true;
                break;
            case EffectKind.Shield:
                attackerStatus.Shielded = true;
                attackerStatus.ShieldReduction = move.EffectMagnitude / 100.0;
                break;
            case EffectKind.Heal:
                if (isPlayer) playerHp = Math.Min(playerMaxHp, playerHp + move.EffectMagnitude);
                else enemyHp = Math.Min(enemyMaxHp, enemyHp + move.EffectMagnitude);
                RefreshHealth();
                break;
        }
        RefreshStatusIcons();
        RefreshMoveButtons();

        if (isPlayer && enemy.IsBoss && !enemyEnraged && enemyHp > 0 && enemyHp <= enemyMaxHp * 0.3)
        {
            enemyEnraged = true;
            BattleLogText.Text += "\n적이 격앙했다!";
        }

        if (enemyHp <= 0) { Finish(true); return true; }
        if (playerHp <= 0) { Finish(false); return true; }
        return false;
    }

    private bool EndOfRoundStatusTicks()
    {
        if (playerStatus.BurnTurns > 0)
        {
            playerStatus.BurnTurns--;
            playerHp = Math.Max(0, playerHp - playerStatus.BurnMagnitude);
            BattleLogText.Text = $"{player.Name}이(가) 화상 피해를 입었다!";
            RefreshHealth();
            RefreshStatusIcons();
            if (playerHp <= 0) { Finish(false); return true; }
        }
        if (enemyStatus.BurnTurns > 0)
        {
            enemyStatus.BurnTurns--;
            enemyHp = Math.Max(0, enemyHp - enemyStatus.BurnMagnitude);
            BattleLogText.Text = $"{enemy.Name}이(가) 화상 피해를 입었다!";
            RefreshHealth();
            RefreshStatusIcons();
            if (enemyHp <= 0) { Finish(true); return true; }
        }
        return false;
    }

    private async void Finish(bool victory)
    {
        finished = true;
        string reward = companion.CompleteBattle(victory, enemyIndex);
        TurnText.Text = victory ? "승리!" : "패배…";
        BattleLogText.Text = reward;
        panel?.ShowResult(reward);
        RefreshMoveButtons();
        if (victory)
        {
            await Task.Delay(enemy.IsBoss ? 2000 : 1100);
            Close();
        }
    }

    private void RefreshMoveButtons()
    {
        for (int index = 0; index < playerMoves.Length && index < MoveGrid.Children.Count; index++)
        {
            if (MoveGrid.Children[index] is not Button button) continue;
            BattleMove move = playerMoves[index];
            button.Content = $"{move.Name}\n{BattleTypes.ElementLabel(move.Element)}{move.Power} · {playerUsesRemaining[index]}/{move.Uses}";
            button.IsEnabled = awaitingPlayerMove && !finished && playerUsesRemaining[index] > 0;
        }
    }

    private void RefreshStatusIcons()
    {
        PlayerBurnIcon.Visibility = playerStatus.BurnTurns > 0 ? Visibility.Visible : Visibility.Collapsed;
        PlayerParalyzeIcon.Visibility = playerStatus.Paralyzed ? Visibility.Visible : Visibility.Collapsed;
        PlayerShieldIcon.Visibility = playerStatus.Shielded ? Visibility.Visible : Visibility.Collapsed;
        EnemyBurnIcon.Visibility = enemyStatus.BurnTurns > 0 ? Visibility.Visible : Visibility.Collapsed;
        EnemyParalyzeIcon.Visibility = enemyStatus.Paralyzed ? Visibility.Visible : Visibility.Collapsed;
        EnemyShieldIcon.Visibility = enemyStatus.Shielded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshHealth()
    {
        PlayerHpBar.Maximum = playerMaxHp; PlayerHpBar.Value = playerHp; PlayerHpText.Text = $"HP {playerHp} / {playerMaxHp}";
        EnemyHpBar.Maximum = enemyMaxHp; EnemyHpBar.Value = enemyHp; EnemyHpText.Text = $"HP {enemyHp} / {enemyMaxHp}";
    }

    private void PlayHitReaction(bool defenderIsEnemy, bool strong)
    {
        GhostArtwork art = defenderIsEnemy ? EnemyArt : PlayerArt;
        TranslateTransform shake = defenderIsEnemy ? EnemyShakeTransform : PlayerShakeTransform;
        art.PlayHitReaction(strong);
        double magnitude = strong ? 14 : 8;
        DoubleAnimationUsingKeyFrames animation = new() { Duration = TimeSpan.FromMilliseconds(320) };
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(-magnitude, KeyTime.FromPercent(0.2)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(magnitude * 0.6, KeyTime.FromPercent(0.45)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(-magnitude * 0.3, KeyTime.FromPercent(0.7)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
        shake.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void PopDamageNumber(int amount, bool onEnemy)
    {
        TextBlock text = new()
        {
            Text = $"-{amount}", Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.Bold,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 4, ShadowDepth = 0, Opacity = .6 }
        };
        double left = onEnemy ? 96 : 300;
        double top = onEnemy ? 66 : 176;
        Canvas.SetLeft(text, left);
        Canvas.SetTop(text, top);
        EffectLayer.Children.Add(text);
        DoubleAnimation rise = new(top, top - 34, TimeSpan.FromMilliseconds(720)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        DoubleAnimation vanish = new(1, 0, TimeSpan.FromMilliseconds(720)) { BeginTime = TimeSpan.FromMilliseconds(160) };
        vanish.Completed += (_, _) => EffectLayer.Children.Remove(text);
        text.BeginAnimation(Canvas.TopProperty, rise);
        text.BeginAnimation(OpacityProperty, vanish);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { panel?.RefreshState(); base.OnClosed(e); }

    private sealed class SideStatus
    {
        public bool Paralyzed;
        public int BurnTurns;
        public int BurnMagnitude;
        public bool Shielded;
        public double ShieldReduction;
    }
}
