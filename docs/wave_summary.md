# SCRUM-139 – čas needov v BOTTLENECK a zhrnutie vlny

Simulácia počas vlny počíta pre každý need, koľko krokov strávil v stave BOTTLENECK. Z toho vzniká zhrnutie vlny pre obrazovku S4 (What went well, Needs improvement, FR-20) a pre návrh vylepšenia na S5 (FR-25).

## Rozhranie pre S4 a S5

`GameSimulation` aj `SimulationRunner` poskytujú:

```csharp
public WaveSummary GetWaveSummary();
```

`WaveSummary` má tieto vlastnosti iba na čítanie:

```csharp
public IReadOnlyDictionary<NeedType, int> BottleneckTicks { get; }
public IReadOnlyList<NeedType> WentWell { get; }
public NeedType? NeedsImprovement { get; }
public bool HadBottleneck { get; }
public bool AllNeedsHadBottleneck { get; }
```

| Vlastnosť | Význam |
|---|---|
| `BottleneckTicks` | počet krokov simulácie, ktoré need strávil v BOTTLENECK; jeden krok je 0,1 s |
| `WentWell` | needy, ktoré v BOTTLENECK neboli vôbec, v poradí zobrazenia na S2 |
| `NeedsImprovement` | need, ktorý bol v BOTTLENECK najdlhšie; `null`, ak nebol žiadny |
| `HadBottleneck` | `false` znamená, že vlna prebehla bez bottlenecku a S4 to má uviesť |
| `AllNeedsHadBottleneck` | `true` znamená, že v BOTTLENECK boli všetky needy; `WentWell` vtedy obsahuje len ten, ktorý v ňom bol najkratšie |

## Pravidlá

- Krok sa započíta, ak je need na jeho konci v stave BOTTLENECK. RECOVERING sa nepočíta.
- Pri rovnakom čase vyhráva need, ktorý je skôr v poradí zobrazenia (Rooms, Drinks, Food, Relax, Ocean View).
- Ten istý need nie je naraz najlepší aj najhorší. Ak má vlna jediný need a ten bol v BOTTLENECK, `WentWell` je prázdny.
- Need s nulovým dopytom vo vlne sa v zhrnutí neuvádza.
- Pauza čas nepridáva. Fast mode mení len trvanie obsluhy, krok ostáva 0,1 s.
- Zhrnutie je snapshot. Čítajte ho pri `WaveCompleted`; platí až do spustenia ďalšej vlny a už získaný objekt sa potom nemení.
- Pred prvou vlnou `GetWaveSummary()` vyvolá `InvalidOperationException`.

## Použitie na S4

```csharp
WaveSummary summary = runner.GetWaveSummary();

if (!summary.HadBottleneck)
{
    // Vlna bez bottlenecku: vypis vsetky needy z summary.WentWell.
}
else
{
    NeedType worst = summary.NeedsImprovement.Value;
    string buildingId = runner.GetBuildingIdForNeed(worst); // budova pre navrh na S5
}
```

Priebežný stav počas vlny je v `GameSimulation.BottleneckTicksThisWave`. Pri udalosti `NeedStateChanged` už obsahuje aj práve skončený krok.
