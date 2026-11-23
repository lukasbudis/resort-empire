# SCRUM-128 – rozhranie Workerov pre S3 a mapu

UI používa existujúci `ResortEmpire.SimulationRunner` v scéne. Model je inicializovaný v jeho `Awake()` z konfigurácie a zahŕňa iba dostupné budovy (`initiallyAvailableBuildingIds`). Prípravu UI vykonajte v `Start()`/pri otvorení S3, po inicializácii runnera. Rozmiestnenie zostáva zachované medzi vlnami.

## Presné verejné rozhranie

```csharp
public IReadOnlyDictionary<string, string> WorkerAssignments { get; }
public bool HasWorker(string buildingId);
public string GetWorkerId(string buildingId);
public string GetWorkerBuildingId(string workerId);
public string GetBuildingIdForNeed(NeedType need);
public bool CanMoveWorker(string fromBuildingId, string toBuildingId);
public bool TryMoveWorker(string fromBuildingId, string toBuildingId);
public event Action<string, string, string> WorkerMoved;
// Poradie argumentov: workerId, fromBuildingId, toBuildingId.
```

| Člen | Správanie |
|---|---|
| `WorkerAssignments` | Živý pohľad iba na čítanie: kľúč je ID dostupnej budovy, hodnota je ID Workera alebo `null` pri prázdnej budove. Pred inicializáciou je celý pohľad `null`. Počet Workerov je o jedného nižší než počet dostupných budov. |
| `HasWorker` | `true`, ak dostupná budova má Workera. Pre neznámu/zamknutú budovu, `null` alebo neinicializovanú simuláciu vracia `false`. |
| `GetWorkerId` | ID Workera v budove alebo `null`, ak je budova prázdna. Neznáma/zamknutá budova vyvolá `ArgumentException`; neinicializovaná simulácia `InvalidOperationException`. |
| `GetWorkerBuildingId` | ID aktuálnej budovy Workera. Neznámy Worker vyvolá `ArgumentException`; neinicializovaná simulácia `InvalidOperationException`. |
| `GetBuildingIdForNeed` | Preklad needu zo S2 na dostupnú budovu pre S3. Pri nedostupnom/neznámom neede alebo pred inicializáciou vracia `null`. |
| `CanMoveWorker` | Overí, že zdroj má Workera a cieľ je iná, dostupná a prázdna budova. Neplatné vstupy vrátane `null` vracajú `false`. |
| `TryMoveWorker` | Znova overí podmienky a vráti `true` iba po úspešnom presune. Neplatný presun vráti `false`, bez zmeny výkonu a udalosti. Presun je zadarmo, nemení konfiguráciu ani simulačný čas. |
| `WorkerMoved` | Jedna udalosť po úspešnom presune. Pri jej prijatí už sú oba výkony aj obe priradenia aktuálne. Inicializácia a odmietnutý presun udalosť nevyvolajú. |

ID sa rozlišujú podľa veľkosti písmen: `Reception`, `Restaurant`, `BeachBar`, `Worker1`, `Worker2`. `Pool` a `Bungalows` sa do presunov nezapočítavajú, kým nie sú dostupné. Správa odomykania budov je nadväzujúca práca.

## Postup pre Marekovo S3

1. Pri ťuknutí na need získaj cieľ cez `runner.GetBuildingIdForNeed(need)`. Pri `null` S3 pre túto budovu neotváraj.
2. Ak `runner.HasWorker(targetId)` vráti `true`, zobraz obsadenie a presun na túto budovu neponúkaj.
3. Zo `WorkerAssignments` vyber obsadené zdrojové budovy a tlačidlá povoľ podľa `CanMoveWorker(sourceId, targetId)`.
4. Pri potvrdení zavolaj `TryMoveWorker(sourceId, targetId)`. Over jeho výsledok, pretože stav sa mohol od vykreslenia UI zmeniť.
5. Pri `WorkerMoved` obnov zobrazenie zo živého pohľadu. Pri zatvorení/zničení UI odhlás handler. Rovnakú udalosť môže Simon použiť na presun vizuálu Workera na mape.

```csharp
// targetId pochadza z GetBuildingIdForNeed pri otvoreni S3.
bool moved = runner.TryMoveWorker(sourceId, targetId);
if (!moved)
{
    // Obnov zoznam zdrojov a dostupnost tlacidiel podla aktualneho modelu.
}
```

Presun funguje pred vlnou (`AwaitingWave`), počas príchodu, dobehu aj pri pozastavenej simulácii. Na runneri je zakázaný po ukončení všetkých vĺn (`AllWavesCompleted`). Pri pauze presun nezvyšuje tiky ani priebeh obsluhy. Stav needu sa podľa SCRUM-129 prepočíta v najbližšom nepozastavenom kroku; S3 môže rozmiestnenie obnoviť ihneď z `WorkerMoved`.

## Výkon a fast mode

Zdrojová budova dostane `unstaffedPerformanceMultiplier` z konfigurácie, cieľová výkon `1f`. Používa sa `BuildingService.SetPerformanceMultiplier`, takže sediaci nestratia vykonanú prácu. `GameSimulation.UpdateFastMode()` mení časy obsluhy pri zachovaní `service.PerformanceMultiplier`; pôvodné rozmiestnenie z konfiguračného súboru už výkon neprepíše.

`GameSimulation` poskytuje rovnaké metódy a udalosť pre testy bez grafiky. Raz pred prvou vlnou sa volá:

```csharp
InitializeWorkers(
    IReadOnlyDictionary<string, NeedType> buildingNeeds,
    IEnumerable<WorkerInitialState> initialWorkers,
    float unstaffedPerformanceMultiplier);
```

Mapovanie obsahuje iba dostupné budovy s už registrovanou samostatnou obsluhou. Inicializácia odmieta duplicitné ID Workerov, dve priradenia na jednu budovu, neznáme budovy, neplatný podiel a nesprávny počet Workerov. Po inicializácii sa obsluhy nesmú nahrádzať cez `RegisterBuildingService`, aby rozmiestnenie a vlna používali rovnaké objekty. `StartWave` rozmiestnenie neresetuje.
