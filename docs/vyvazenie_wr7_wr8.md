# Vyváženie vĺn: pravidlá WR7 a WR8

Test `Assets/Tests/WaveBalanceRulesTests.cs` (SCRUM-172, NFR-10) simuluje každú z ôsmich vĺn z `config.json` bez grafiky. Používa rovnaký kód ako hra: `GameSimulation`, `GameCycle` a `WaveSatisfactionCalculator`.

Rozmiestnenie Worker-ov je statické ako vo `wave_sim.py` z Build dokumentu: Worker-i sa rozmiestnia pred vlnou, počas vlny sa nepresúvajú a Energy sa nepoužije. Test skúsi každú budovu, ktorá môže ostať bez Worker-a, a hodnotí najlepšie rozmiestnenie. Pool je dostupný od 4. vlny a Bungalows od 7. vlny.

- **WR7:** vo vlnách 3, 5, 6 a 8 ani najlepšie rozmiestnenie s budovami na leveli 1 nedosiahne 5★.
- **WR8:** s budovami na leveli 3 dosiahne najlepšie rozmiestnenie aspoň 5★ v každej vlne.

## Výsledky

Konfigurácia `config.json` je z `develop` `3fbe1d2`.

| Vlna | Level 1: bez Worker-a | Level 1: výsledok | Build dokument, bez zásahu | Level 3: výsledok |
|---|---|---|---|---|
| 1 | ktorákoľvek | 100 % Perfect | Perfect | 100 % Perfect |
| 2 | Restaurant | 96 % 5★ | 4★ (s mini-eventom Sudden rush) | 100 % Perfect |
| 3 | Restaurant | 86 % 4★ | 4★ | 100 % Perfect |
| 4 | Pool | 100 % Perfect | Perfect | 100 % Perfect |
| 5 | Pool | 89 % 4★ | 4★ | 100 % Perfect |
| 6 | Pool | 85 % 4★ | 4★ | 100 % Perfect |
| 7 | Bungalows | 95 % 5★ | 5★ | 100 % Perfect |
| 8 | BeachBar | 75 % 3★ | 3★ | 100 % Perfect |

Obe pravidlá platia. Výsledky sa zhodujú so stĺpcom „bez zásahu“ v Build dokumente (§4.8) okrem vlny 2. Tam dokument počíta s mini-eventom Sudden rush, ktorý hra nemá.

## Na čo dať pozor

- **Vlna 5 má pri WR7 rezervu 1 bod:** 89 % pri hranici 5★ na 90 %. Prevrátiť ju môže zmena `service_time`, kapacít, `unstaffedPerformanceMultiplier` alebo tabuľky vĺn. Test to zachytí.
- **WR8 platí len pri správnom rozmiestnení.** Pri najhoršom rozmiestnení na leveli 3 skončia vlny 6 a 8 na 89 % a 87 %, teda 4★.
- **Testy vedia zlyhať:**
  - obsluha 6 s namiesto 12 s zhodí WR7 vo všetkých štyroch vlnách,
  - 3 miesta na leveli 3 zhodia WR8 vo vlnách 3, 5, 6 a 8,
  - výber najhoršieho rozmiestnenia namiesto najlepšieho zhodí WR8 vo vlnách 6 a 8.

## Ako vypísať všetky rozmiestnenia

Test `PrintBalanceTable` je označený `[Explicit]`, preto sa v bežnom behu preskočí. Vypíše tabuľku všetkých rozmiestnení všetkých vĺn na oboch leveloch.

1. V Unity otvor Window → General → Test Runner → EditMode.
2. Vyber `WaveBalanceRulesTests` → `PrintBalanceTable` a klikni na Run Selected.
3. Tabuľka sa ukáže vo výstupe testu a v Console.
