## 1. Základná terminológia

Tieto presné výrazy používajte pri písaní tried, enumov, databáz a UI prvkov:

*   **`Wave`** *(Vlna)*: vlna hostí, ktorá príde, prebehne a skončí (~90 s). Jadro hry.
*   **`Need`** *(Potreba)*: typ potreby hostí: Rooms, Drinks, Food, Relax, Ocean View. Zobrazuje sa agregovane (Food 2/6). Posledné dva pribudnú až s unlockom svojej budovy.
*   **`Service / budova`** : to, čo need obsluhuje (Restaurant obsluhuje Food). Pozor na dve ubytovacie: Reception = hotelový blok → Rooms; Bungalows = premium chatky pri oceáne → Ocean View.
*   **`Bottleneck`** :need, ktorý sa neplní dosť rýchlo — vzniká rad. Toto je „problém", ktorý hráč rieši.
*   **`Staff`** :pracovník, ktorého hráč priradí na preťaženú službu, aby ju zrýchlil.
*   **`Wave Satisfaction`** :jedno % za celú vlnu. Klesá, kým trvá bottleneck.
*   **`Coins`** *(Mince)*: Hlavná soft mena určená na upgrady, unlocky a najímanie.
*   **`Energy`** *(Energia)*: Bonusová boost mena na dočasné zrýchlenie obsluhy v budovách.
*   **`Speed Up`** / **`Boost`** *(Zrýchlenie)*: Akcia alebo stav, kedy sa minie Energy a service_time budovy klesne na polovicu.
*   **`Resort Value`** *(Hodnota rezortu)*: statusové číslo, len rastie. Prestíž rezortu
*   **`Wave Satisfaction`** *(Spokojnosť z vlny)*: Celková akumulovaná história čakania vyhodnotená na konci vlny (0–100 %).
*   **`Rating`** *(Hodnotenie)*: Finálne hviezdičkové vyhodnotenie vlny (1★ až 5★, alebo Perfect).
*   **`Manager`** *(Manažér)*: Menovaný špecialista pevne viazaný na konkrétnu wave budovu, prináša pasívny násobič rýchlosti.
*   **`Specialist`** *(Špecialista)*: Menovaný VIP pracovník bez fixnej budovy (napr. Butler, Captain) slúžiaci ako roster predpoklad pre VIP.

---

## 2. Stavové stroje (Finite State Machines & States)

### 2.1 Stavy vĺn (`WaveState`)
Implementujte presne tieto stavy pre životný cyklus manažéra vĺn:
*   **`AWAITING_WAVE`**: Rezort je v pokojovom stave a čaká na spustenie vlny. *Poznámka: Nikdy pre tento stav nepoužívajte výraz IDLE.*
*   **`WAVE_ACTIVE`**: Aktívne okno hrateľnosti. Pokrýva fázu rovnomerného príchodu (arrival) aj fázu dobehu (cleanup backlogov).
*   **`WAVE_PAUSED`**: Stav, kedy je aplikácia na pozadí; všetky časovače a procesy zamrznú.
*   **`WAVE_COMPLETE`**: Výsledková obrazovka zobrazujúca získané hviezdičky, Coins, Energy a breakdown úspechu.
*   **`UPGRADE`**: Medzi-vlnový stav určený na nakupovanie vylepšení budov a unlockov pred ďalšou vlnou.

### 2.2 Stavy potrieb a budov (`NeedState`)
Implementujte presne tieto stavy pre sledovanie radov (queue) jednotlivých potrieb:
*   **`LOCKED`**: Budova/need je uzamknutá, nezobrazuje sa na paneli a negeneruje žiadny demand.
*   **`OK`**: Pokojný stav; rad čakajúcich je stabilný a pod kritickou hranicou warningu.
*   **`BOTTLENECK`**: Rad čakajúcich prekročil threshold a rástol dlhšie ako stanovený čas hysterézie (vyvolá warning).
*   **`RECOVERING`**: Pozitívny signál pre hráča; rad sa po reakcii (presun Workera/boost) aktívne zmenšuje.
*   **`SATISFIED`**: Príchod je uzavretý, rad je prázdny a pri pulte už nikto nesedí (need je úspešne vybavený).

---

## 3. Konfiguračné parametre

Tieto premenné priamo mapujú kapitolu 4.8 v Build dokumente a **musia sa presne zhodovať** v kóde, JSON konfiguráciách aj vyvažovacom skripte `wave_sim.py`:

### 3.1 Čas a tok (Timing & Flow)
*   **`wave_timer`** *(float, sekundy)*: Pevný čas, počas ktorého prichádza nový demand (zatvára príchod, nie vlnu).
*   **`arrival_rate`** *(float, jednotky/s)*: Rýchlosť, akou sa uvoľňujú nové jednotky potrieb do dopytu.
*   **`service_time`** *(float, sekundy)*: Základný čas potrebný na obslúženie jedného hosťa pri plnej efektivite budovy.
*   **`patience_grace`** *(float, sekundy)*: Čas čakania v rade, do ktorého je príspevok jednotky do spokojnosti plných 100 %.
*   **`patience_floor_after`** *(float, sekundy)*: Čas, po ktorom príspevok čakajúcej jednotky klesne na absolútne minimum.

### 3.2 Objem a sledovanie (Volume & Tracking)
*   **`demand_total`** *(int)*: Celkový objem jednotiek danej potreby, ktoré vlna vygeneruje.
*   **`demand_arrived`** *(int)*: Počet jednotiek dopytu, ktoré už reálne do daného času dorazili.
*   **`filled`** *(int)*: Počet jednotiek, ktoré budova úspešne obslúžila.
*   **`backlog`** *(int)*: Odvodená hodnota vyjadrujúca aktuálny rad čakajúcich pred pultom (`demand_arrived - filled`).
*   **`capacity`** *(int)*: Počet súbežných miest (stoličiek/slotov) pri pulte, ktoré určuje level budovy.

### 3.3 Násobiče a prahy (Multipliers & Thresholds)
*   **`utilization_staffed`** *(float)*: Multiplier rýchlosti obsluhy, ak je priradený Worker (Pevne: `1.0`).
*   **`utilization_empty`** *(float)*: Degradovaná rýchlosť budovy, ak beží sama bez Workera (Default: `0.33`).
*   **`worker_slots`** *(int)*: Maximálny počet slotov pre Workerov v jednej budove (v prototype fixne 1).
*   **`manager_multiplier`** *(float)*: Násobič rýchlosti budovy dodaný prideleným Manažérom.
*   **`bottleneck_threshold`** *(int)*: Počet čakajúcich v rade, nad ktorý sa spúšťa riziko bottlenecku.
*   **`bottleneck_hysteresis_s`** *(float, sekundy)*: Časový filter na overenie rastu radu, bráni blikaniu warningu v UI.

### 3.4 Mini-eventy (Mini-Events)
*   **`rush_wave`** / **`rush_time_s`**: Index vlny a čas spustenia mini-eventu *Sudden Rush* (Nával).
*   **`rush_units`** / **`rush_duration_s`**: Extra objem jednotiek a trvanie návalu *Sudden Rush*.

### 3.5 Progresia a ekonomika (Progression & Finance)
*   **`unit_floor`** *(float, %)*: Spodná podlaha spokojnosti pre jednu konkrétnu dlho čakajúcu jednotku (TUNING: `25 %`).
*   **`wave_floor`** *(float, %)*: hard minimum celkovej Wave Satisfaction, ochrana pred 0★ (TUNING: `40 %` = 1★).
*   **`rating_thresholds`** *(float array, %)*: Hranice spokojnosti pre hviezdičky [1★, 2★, 3★, 4★, 5★, Perfect].
*   **`coins_per_unit`** *(int)*: Základná odmena v Coins za každú úspešne vybavenú jednotku vlny.
*   **`coins_rating_mult`** *(float array)*: Násobiče finančnej odmeny na základe dosiahnutého hviezdičkového hodnotenia.
*   **`upgrade_cost`** / **`unlock_cost`** *(int arrays)*: Ceny upgradov a odomknutia nových oblastí v Coins.
*   **`worker_cap_ratio`** *(float)*: Limit percentuálneho stropu najatých Workerov voči celkovému počtu slotov.
