# SCRUM-130 – čakanie jednotiek pre Wave Satisfaction

`BuildingService` pri príchode každej jednotky uloží jej ID a tik príchodu. Pri usadení vytvorí nemenný záznam `UnitWaitingTime`. Čas obsluhy sa do čakania nezapočítava a už uložený záznam sa po zrýchlení, spomalení ani dokončení obsluhy nemení.

## Rozhranie pre SCRUM-131

`GameSimulation` aj verejná fasáda `SimulationRunner` poskytujú:

```csharp
public IReadOnlyList<UnitWaitingTime> GetWaitingTimesThisWave(NeedType need);
```

Každý záznam má tieto vlastnosti iba na čítanie:

```csharp
public int UnitId { get; }
public long ArrivalTick { get; }
public long SeatedTick { get; }
public long WaitingTicks { get; }
public double WaitingSeconds { get; }
```

- `WaitingTicks = SeatedTick - ArrivalTick`.
- `WaitingSeconds = WaitingTicks × 0,1 s`.
- ID jednotky je jedinečné v rámci jednej `BuildingService`, nie naprieč rôznymi needmi. Časové značky používajú lokálnu os krokov danej obsluhy.
- Getter vráti nemenný snapshot usadených jednotiek aktuálnej/naposledy dokončenej vlny pre zvolený need. Pri ďalšom `StartWave` začne nový výber záznamov. Už získaný snapshot zostáva platný a nemení sa.
- Čakajúca, ešte neusadená jednotka vo výsledku zatiaľ nie je. Po dokončení celej vlny sú zaznamenané všetky prijaté jednotky, aj tie s nulovým čakaním.
- Pred spustením prvej vlny getter vyvolá `InvalidOperationException`; need mimo aktuálnej vlny vyvolá `ArgumentException`. Platný need s nulovým dopytom vráti prázdny zoznam.

Simon môže pri dokončení vlny pre každý need prečítať `WaitingSeconds` a použiť krivku príspevku podľa FR-17. Váhy needov ostávajú priradené needu, ktorým sa volal getter; tento task samotné hodnotenie nepočíta.

```csharp
var foodWaits = runner.GetWaitingTimesThisWave(NeedType.Food);
foreach (var sample in foodWaits)
{
    double waitingSeconds = sample.WaitingSeconds;
    // SCRUM-131 tu pouzije krivku prispevku jednotky a vahu needu Food.
}
```

Údaje prečítajte pri `WaveCompleted` alebo pred spustením ďalšej vlny. Ak ich chcete zobraziť neskôr, uchovajte získaný snapshot. Pri predčasnom prečítaní počas vlny zoznam ešte nemusí obsahovať všetkých čakajúcich.

## Samostatná obsluha a hranice tikov

`BuildingService.ElapsedTicks` je počet jej vykonaných krokov. Verejné `AddArrivals(count)` použije tento aktuálny čas. Napríklad tri jednotky prídu v tiku 0 k jednému miestu s obsluhou 10 tikov: čakania pri usadení sú 0, 10 a 20 tikov.

`GameSimulation` spracuje príchody z `WaveModel` pred krokom budov. Používa preto interné `AddArrivalsForStep(count)`, ktoré príchod označí tikom práve spracúvaného kroku. Ak sa v tomto kroku uvoľní miesto a jednotka sa usadí, čakanie je 0, nie umelo pridaný jeden tik. Poradie obsluhy, počet vykonaných prác a pôvodné časy dokončenia sa nemenia.

`BuildingService.WaitingTimes` je živý pohľad iba na čítanie na všetky zaznamenané usadenia počas životnosti obsluhy. Pre hodnotenie jednej vlny používajte getter na simulácii/runneri, ktorý oddelí záznamy od predchádzajúcich vĺn.

Pauza zastaví kroky simulácie aj obsluhy, takže nepridáva čakanie. Fast mode mení počet krokov potrebných na obsluhu; dĺžka jedného kroku zostáva 0,1 s. Zmena výkonu ovplyvní čas budúceho uvoľnenia miesta, ale nemení historické čakania.
