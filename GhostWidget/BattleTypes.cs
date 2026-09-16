using System.Windows.Media;

namespace GhostWidget;

internal enum Element { Fire, Water, Nature, Light, Dark }
internal enum EffectKind { None, Burn, Paralyze, Shield, Heal }

internal readonly record struct BattleMove(string Name, int Power, Element Element, EffectKind Effect, int EffectMagnitude, int Uses);

/// <summary>Pure battle math shared by MainWindow (roster/reward logic) and BattleWindow (turn resolution).</summary>
internal static class BattleTypes
{
    // Hand-themed forms keep an intentional element; every other form (Base/Round/New1-30)
    // falls back to a color-derived guess in ElementOf so the 36 generic species aren't blank.
    private static readonly Dictionary<string, Element> FormElementOverrides = new()
    {
        ["Pumpkin"] = Element.Fire, ["Candle"] = Element.Fire,
        ["Moss"] = Element.Nature, ["Rose"] = Element.Nature, ["Sand"] = Element.Nature,
        ["Jelly"] = Element.Water, ["Pirate"] = Element.Water,
        ["Moon"] = Element.Light, ["Star"] = Element.Light, ["Cloud"] = Element.Light, ["Legend"] = Element.Light,
        ["Shadow"] = Element.Dark, ["Bat"] = Element.Dark, ["Witch"] = Element.Dark,
        ["BossHearth"] = Element.Fire, ["BossVolcano"] = Element.Fire,
        ["BossMoss"] = Element.Nature, ["BossTree"] = Element.Nature,
        ["BossMist"] = Element.Water, ["BossStorm"] = Element.Water,
        ["BossDawn"] = Element.Light, ["BossCelestial"] = Element.Light,
        ["BossGate"] = Element.Dark, ["BossVoid"] = Element.Dark,
    };

    private static readonly Dictionary<string, (int Level, int Wins)> BossRequirements = new()
    {
        ["BossHearth"] = (4, 5), ["BossMoss"] = (6, 10), ["BossMist"] = (8, 15), ["BossDawn"] = (10, 20), ["BossGate"] = (12, 25),
        ["BossVolcano"] = (18, 40), ["BossStorm"] = (20, 45), ["BossTree"] = (24, 55), ["BossCelestial"] = (28, 65), ["BossVoid"] = (30, 70),
    };

    internal static Element ElementOf(GhostSpecies species)
    {
        if (FormElementOverrides.TryGetValue(species.Form, out Element element)) return element;
        (double hue, double saturation, double value) = ToHsv(species.Color);
        if (value <= 0.35) return Element.Dark;
        if (saturation <= 0.15) return Element.Light;
        if (hue is >= 340 or < 70) return Element.Fire;
        return hue < 165 ? Element.Nature : Element.Water;
    }

    internal static double SpeedOf(GhostSpecies species) => 40 + species.Difficulty * 60;

    internal static (int Level, int Wins) BossRequirement(string form) =>
        BossRequirements.TryGetValue(form, out (int Level, int Wins) requirement) ? requirement : (0, 0);

    internal static double GetMultiplier(Element attacker, Element defender)
    {
        if (attacker == defender) return 1.0;
        bool advantage = (attacker, defender) is
            (Element.Fire, Element.Nature) or (Element.Nature, Element.Water) or (Element.Water, Element.Fire) or
            (Element.Light, Element.Dark) or (Element.Dark, Element.Light);
        if (advantage) return 1.4;
        bool disadvantage = (attacker, defender) is
            (Element.Nature, Element.Fire) or (Element.Water, Element.Nature) or (Element.Fire, Element.Water);
        return disadvantage ? 0.7 : 1.0;
    }

    internal static string ElementLabel(Element element) => element switch
    {
        Element.Fire => "불", Element.Water => "물", Element.Nature => "자연", Element.Light => "빛", Element.Dark => "어둠", _ => "?"
    };

    internal static BattleMove[] BuildGenericMoveset(Element element)
    {
        string label = ElementLabel(element);
        return
        [
            new($"{label} 기운 모으기", 9, element, EffectKind.None, 0, 6),
            new($"{label} 강타", 14, element, EffectKind.None, 0, 4),
            new($"{label} 파동", 18, element, EffectKind.Burn, 5, 2),
        ];
    }

    internal static int ChooseEnemyMoveIndex(BattleMove[] moves, int[] usesRemaining, int currentHp, int maxHp, Element opponentElement, Random random)
    {
        List<int> available = [];
        for (int index = 0; index < moves.Length; index++) if (usesRemaining[index] > 0) available.Add(index);
        if (available.Count == 0) return -1;

        if (currentHp <= maxHp * 0.3)
        {
            int defensive = available.FirstOrDefault(i => moves[i].Effect is EffectKind.Shield or EffectKind.Heal, -1);
            if (defensive >= 0) return defensive;
        }

        List<int> superEffective = available.Where(i => GetMultiplier(moves[i].Element, opponentElement) > 1.0).ToList();
        List<int> pool = superEffective.Count > 0 ? superEffective : available;

        int totalWeight = Math.Max(1, pool.Sum(i => moves[i].Power));
        int roll = random.Next(totalWeight);
        int cumulative = 0;
        foreach (int index in pool)
        {
            cumulative += moves[index].Power;
            if (roll < cumulative) return index;
        }
        return pool[^1];
    }

    private static (double Hue, double Saturation, double Value) ToHsv(string hex)
    {
        Color color;
        try { color = (Color)ColorConverter.ConvertFromString(hex)!; }
        catch { return (0, 0, 0.5); }
        double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        double hue = delta == 0 ? 0
            : max == r ? 60 * (((g - b) / delta) % 6)
            : max == g ? 60 * (((b - r) / delta) + 2)
            : 60 * (((r - g) / delta) + 4);
        if (hue < 0) hue += 360;
        double saturation = max == 0 ? 0 : delta / max;
        return (hue, saturation, max);
    }
}
