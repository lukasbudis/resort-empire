> Čitateľná kópia `poziadavky_v0.6.docx` vygenerovaná cez `pandoc -t gfm`. Platí docx; pri zmene požiadaviek pribudne nová verzia oboch súborov.

# 1. Rozsah

**Záväzný rozsah:**

- jeden typ hosťa (vystupuje ako dopyt, nie ako jednotlivci) a päť needov,

- päť budov: tri od začiatku, Pool a Bungalows ako odomykateľné oblasti,

- Worker-i, ktorých je vždy o jedného menej ako budov,

- osem skriptovaných vĺn,

- obrazovky S1, S2, S3, S4, S5 a S9,

- meny Coins a Energy, ukazovateľ Resort Value,

- ukladanie postupu a nová hra po skončení prototypu,

- krátke textové nápovedy namiesto tutoriálu (NFR-09).

**Nezáväzné rozšírenia (ideas):**

- mini-event Sudden rush,

- animácie a efekty (napr. príchod lode, odchod hostí po vlne, oslava odomknutia), etc.

- rewarded video za Energy,

- telemetria pre playtest.

# 2. Funkčné požiadavky

## 2.1 Vlna

<table>
<colgroup>
<col style="width: 10%" />
<col style="width: 71%" />
<col style="width: 17%" />
</colgroup>
<thead>
<tr>
<th><strong>ID</strong></th>
<th><strong>Požiadavka</strong></th>
<th><strong>Zdroj</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>FR-01</strong></td>
<td><p>Hra sa riadi stavmi awaiting wave → wave incoming → wave active → wave resolving → wave complete → upgrade → awaiting wave znovu. Počas aktívnej vlny môže hra prejsť do wave paused a späť (FR-32).</p>
<ul>
<li><p>Hráč spustí vlnu tlačidlom Start Wave na S1.</p></li>
<li><p>Po 8. vlne sa cyklus končí (resp. koniec prototypu)</p></li>
</ul></td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>FR-02</strong></td>
<td>Vlna má fázu príchodu s nastaviteľnou dĺžkou (predvolene 90 s), počas ktorej dopyt prichádza rovnomerne, a fázu dobehu bez nového dopytu. Vlna sa skončí, keď je príchod uzavretý a žiadny need nemá nikoho v rade ani v obsluhe.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-03</strong></td>
<td>Hra obsahuje 8 skriptovaných vĺn. Objem každého needu v každej vlne je daný konfiguráciou (predvolene tabuľka vĺn klienta).</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-04</strong></td>
<td>V stave AWAITING_WAVE pred 1. až 8. vlnou hráč vidí forecast nasledujúcej vlny: needy a ich objem.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-05</strong></td>
<td><p>Po dokončení 8. vlny sa hra vráti na S1, kde namiesto Start Wave a forecastu zobrazí oznam o konci prototypu a tlačidlo New Game.</p>
<ul>
<li><p>New Game vymaže uložený postup a spustí hru v počiatočnom stave (FR-07).</p></li>
</ul></td>
<td>Tím</td>
</tr>
</tbody>
</table>

## 2.2 Hostia, needy a budovy

<table>
<colgroup>
<col style="width: 10%" />
<col style="width: 71%" />
<col style="width: 17%" />
</colgroup>
<thead>
<tr>
<th><strong>ID</strong></th>
<th><strong>Požiadavka</strong></th>
<th><strong>Zdroj</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>FR-06</strong></td>
<td>Všetci hostia sú jedného typu (bežný turista, v podkladoch Casual Tourist) a správajú sa rovnako: vytvárajú dopyt po needoch Rooms, Drinks, Food, Relax a Ocean View. Hra ich nesleduje ani nezobrazuje ako jednotlivcov, len ako dopyt po needoch. Iné typy hostí (napr. VIP) nie sú v rozsahu.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-07</strong></td>
<td>Hra začína budovami reception (need rooms), restaurant (food) a bar (drinks) na leveli 1, dvoma workermi a staveniskami pre pool (relax) a bungalows (ocean view).</td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>FR-08</strong></td>
<td>Budova má nastaviteľný počet miest podľa levelu. Jednotky needu sa usádzajú na voľné miesta v poradí príchodu. Obsluha jednej jednotky trvá nastaviteľný čas a jej priebeh sa mení podľa FR-13 a FR-15. Ak sú miesta plné, jednotky čakajú v rade.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-09</strong></td>
<td><p>Každý need má stav OK, BOTTLENECK, RECOVERING alebo SATISFIED. Rýchlosť obsluhy budovy = počet miest × podiel výkonu ÷ aktuálny čas obsluhy; rýchlosť príchodu needu = jeho objem vo vlne ÷ dĺžka príchodu. Do radu sa počítajú len čakajúci, nie sediaci. Prechody:</p>
<ul>
<li><p>do bottleneck: príchod ešte nie je uzavretý, rad čakajúcich je nad nastaviteľným prahom a rýchlosť obsluhy je nižšia ako rýchlosť príchodu,</p></li>
<li><p>do recovering: rýchlosť obsluhy je vyššia ako rýchlosť príchodu, alebo je príchod uzavretý,</p></li>
<li><p>do ok: rad klesne na prah alebo pod neho,</p></li>
<li><p>do satisfied: príchod je uzavretý a need nemá nikoho v rade ani v obsluhe.</p></li>
</ul></td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>FR-10</strong></td>
<td>Need budovy, ktorú hráč ešte nekúpil, sa v dopyte nevyskytuje ani nezobrazuje.</td>
<td>Podklady</td>
</tr>
</tbody>
</table>

## 2.3 Worker-i

| **ID** | **Požiadavka** | **Zdroj** |
|----|----|----|
| **FR-11** | Hráč presúva workera medzi budovami podľa potreby, zadarmo. | Podklady |
| **FR-12** | Worker-ov je vždy o jedného menej ako budov (2 → 3 → 4) a každá budova má jedno miesto pre Worker-a. Nový Worker príde s odomknutou budovou a začne na nej. | Podklady |
| **FR-13** | Budova bez Worker-a obsluhuje pomalšie (nastaviteľný podiel výkonu, predvolene 0,33), nikdy nie nulovou rýchlosťou. | Podklady |
| **FR-14** | Worker-ov možno presúvať pred vlnou aj počas nej, vrátane fázy dobehu. Rozmiestnenie sa medzi vlnami zachová. | Podklady |
| **FR-15** | Presun sa prejaví okamžite: rýchlosť obsluhy sa zmení na oboch budovách, aj pre hostí, ktorí už sedia, a zdrojová budova je viditeľne bez Worker-a. | Podklady |
| **FR-16** | Hráč vidí, ktoré budovy sú obsadené Worker-om a ktoré sú prázdne: na mape, na paneli S2 aj na obrazovke S3. | Podklady |

## 2.4 Hodnotenie vlny

<table>
<colgroup>
<col style="width: 10%" />
<col style="width: 71%" />
<col style="width: 17%" />
</colgroup>
<thead>
<tr>
<th><strong>ID</strong></th>
<th><strong>Požiadavka</strong></th>
<th><strong>Zdroj</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>FR-17</strong></td>
<td><p>Wave Satisfaction sa počíta na konci vlny z čakania každej jednotky needu. Čakanie je čas od príchodu jednotky po jej usadenie; čas obsluhy sa nepočíta.</p>
<ul>
<li><p>príspevok jednotky je 100 %, ak čakala najviac nastaviteľnú dolnú hranicu (predvolene 10 s); medzi dolnou a hornou hranicou (predvolene 30 s) lineárne klesá na podlahu jednotky (predvolene 25 %) a pri dlhšom čakaní sa jej rovná,</p></li>
<li><p>výsledok je vážený priemer príspevkov s nastaviteľnými váhami needov (predvolene Ocean View 1,5, ostatné needy 1),</p></li>
<li><p>výsledok je najmenej podlaha vlny (predvolene 40 %).</p></li>
</ul></td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-18</strong></td>
<td>Rating 1★ až 5★ a Perfect sa určí z Wave Satisfaction podľa nastaviteľných prahov. Najhorší výsledok je 1★, nie 0.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-19</strong></td>
<td>Počas vlny žiadny hosť neodíde, resp. každá jednotka needu sa musí obslúžiť.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-20</strong></td>
<td><p>Po vlne sa zobrazí:</p>
<ul>
<li><p>rating a wave satisfaction,</p></li>
<li><p>počet obslúžených jednotiek needov,</p></li>
<li><p>získané coins a energy,</p></li>
<li><p>zhrnutie: What went well (needy, ktoré neboli v BOTTLENECK) a Needs improvement (need, ktorý bol v BOTTLENECK najdlhšie); ak žiadny need nebol v BOTTLENECK, S4 to uvedie, a ak v ňom boli všetky, What went well uvedie ten, ktorý v ňom bol najkratšie.</p></li>
</ul></td>
<td>Podklady, Tím</td>
</tr>
</tbody>
</table>

## 2.5 Odmeny a stav rezortu

| **ID** | **Požiadavka** | **Zdroj** |
|----|----|----|
| **FR-21** | Za vlnu hráč dostane Coins = počet jednotiek vlny × Coins za jednotku × násobič ratingu (nastaviteľné). | Podklady |
| **FR-22** | Obrazovka S1 zobrazuje stav Coins, Energy a Resort Value. Resort Value rastie s vylepšeniami budov a odomknutými oblasťami a nikdy neklesá. | Podklady, Tím |

## 2.6 Vylepšenia budov

<table>
<colgroup>
<col style="width: 10%" />
<col style="width: 71%" />
<col style="width: 17%" />
</colgroup>
<thead>
<tr>
<th><strong>ID</strong></th>
<th><strong>Požiadavka</strong></th>
<th><strong>Zdroj</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>FR-23</strong></td>
<td>Budova má 3 levely. Vylepšenie zvýši počet miest (predvolene 3 → 5 → 7), stojí Coins a prejaví sa okamžite.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-24</strong></td>
<td>Vylepšenie je dostupné len mimo vlny, teda v stave AWAITING_WAVE a na obrazovke S5. Počas vlny (od Start Wave po jej uzavretie) sa možnosť vylepšenia nezobrazuje.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-25</strong></td>
<td><p>Po dokončení vlny hra navrhne vylepšenie budovy, ktorej need bol vo vlne najdlhšie v stave BOTTLENECK.</p>
<ul>
<li><p>Ak vo vlne nebol žiadny bottleneck alebo je budova na najvyššom leveli, toto sa nezobrazí.</p></li>
</ul></td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>FR-26</strong></td>
<td>V stave awaiting wave ťuknutie na budovu ponúkne možné vylepšenie a presun workera.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-27</strong></td>
<td>Počet miest každej budovy je pri budove zobrazený (napr. číslom alebo ikonami).</td>
<td>Podklady</td>
</tr>
</tbody>
</table>

## 2.7 Odomykanie oblastí

| **ID** | **Požiadavka** | **Zdroj** |
|----|----|----|
| **FR-28** | Pool sa sprístupní po dokončení 3. vlny a Bungalows po dokončení 6. vlny. Sprístupnenú oblasť hráč kúpi za coins, pričom jej kúpou pribudne budova, jej need a jeden worker. | Podklady |
| **FR-29** | Budovy stoja na vopred určených miestach. Staveniská pool a bungalows sú na mape od začiatku a ukazujú, či sú zamknuté alebo sprístupnené. | Podklady |
| **FR-30** | Ak hráč sprístupnenú oblasť hneď nekúpi, ťuknutím na jej stavenisko v stave awaiting wave sa znovu otvorí screen s ponukou na kúpu. Počas vlny sa oblasť kúpiť nedá. | Podklady |
| **FR-31** | Táto obrazovka ukáže, čo oblasť pridá (need a workera) a jej cenu. Ak hráč nemá dosť coins, kúpa sa nedá potvrdiť. Vždy sa dá vždy zavrieť bez kúpy a hra pokračuje ďalej. | Podklady |

## 2.8 Pauza a uloženie

| **ID** | **Požiadavka** | **Zdroj** |
|----|----|----|
| **FR-32** | Keď aplikácia počas vlny prejde na pozadie, vlna sa pozastaví (WAVE_PAUSED) a všetky časovače sa zastavia. Po návrate do aplikácie vlna pokračuje presne od miesta pozastavenia. Ak systém aplikáciu medzitým ukončí, platí FR-33. | Podklady, Tím |
| **FR-33** | Hra ukladá postup pri prechode medzi jednotlivými štádiami vlny a pri každej zmene mimo vlny: číslo vlny, coins, energy, resort Value, levely budov, odomknuté oblasti, rozmiestnenie workerov. Pri prechode do wave paused uloží aj celý stav rozohranej vlny, takže keď systém aplikáciu na pozadí ukončí, po spustení hra pokračuje presne od miesta pozastavenia. Ak sa aplikácia ukončí inak (napr. pádom), hra pokračuje od posledného uloženého stavu. | Podklady, Tím |

## 2.9 Obrazovky

<table>
<colgroup>
<col style="width: 10%" />
<col style="width: 71%" />
<col style="width: 17%" />
</colgroup>
<thead>
<tr>
<th><strong>ID</strong></th>
<th><strong>Požiadavka</strong></th>
<th><strong>Zdroj</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>FR-34</strong></td>
<td>Hra obsahuje obrazovky inšpirované podkladmi od klienta, S1 2 3 4 5 a 9. Označenia S1 až S9 sú prevzaté z podkladov klienta, S6 až S8 sú obrazovky VIP hostí, ktoré nie sú v rozsahu.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-35</strong></td>
<td><p>Tok obrazoviek:</p>
<ul>
<li><p>S2 sa otvorí sám po spustení vlny,</p></li>
<li><p>S3 sa vracia tam, odkiaľ bola otvorená (počas vlny do S2, pred vlnou do S1); ak sa vlna skončí pri otvorenej S3, S3 sa zavrie a zobrazí sa S4,</p></li>
<li><p>po S4 nasleduje S5, ak ju FR-25 nevylúči,</p></li>
<li><p>vo vlne, ktorou sa oblasť sprístupnila, sa pred návratom na S1 zobrazí S9.</p></li>
</ul></td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>FR-36</strong></td>
<td>Obrazovka S2 počas vlny zobrazuje každý aktívny need s počtom obslúžených a celkových jednotiek a s jeho stavom.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>FR-37</strong></td>
<td>Ťuknutie na need v S2 otvorí S3 pre budovu, ktorá ho obsluhuje. Ak má táto budova Worker-a, S3 to ukáže a presun na ňu neponúkne. Need v stave BOTTLENECK je v S2 zvýraznený. Keď need prejde do BOTTLENECK, S2 krátko zobrazí hlášku hostí (napr. „Food takes a bit too long.“); keď prejde do RECOVERING, upozornenie sa zmení na pozitívnu hlášku.</td>
<td>Podklady</td>
</tr>
</tbody>
</table>

## 2.10 Energy

| **ID** | **Požiadavka** | **Zdroj** |
|----|----|----|
| **FR-38** | Energy je voliteľná mena na dočasné zrýchlenie budovy (boost). Nikdy nie je podmienkou: každú vlnu možno dokončiť bez nej a rezort s budovami na najvyššom leveli dosiahne v každej vlne 5★ aj bez Energy. | Podklady |
| **FR-39** | Keď je need v stave BOTTLENECK alebo RECOVERING, S2 pri ňom ponúkne tlačidlo Speed Up za Energy (predvolene 3). Boost na nastaviteľný čas (predvolene 15 s) skráti čas obsluhy budovy na polovicu (nastaviteľné); jeho začiatok aj koniec platí okamžite aj pre jednotky, ktoré už sedia, a počet miest nemení. Funguje v príchode aj v dobehu. Na jednu budovu sa dá použiť najviac raz za vlnu a po použití sa tlačidlo na nej do konca vlny nezobrazí. Počas boostu S2 ukazuje jeho zostávajúci čas. | Podklady, Tím |
| **FR-40** | Za dokončenú vlnu hráč dostane Energy podľa ratingu (predvolene 1★: 0, 2★: 1, 3★: 2, 4★: 3, 5★ aj Perfect: 5; nastaviteľné). | Podklady, Tím |
| **FR-41** | Energy nemá strop, nevracia sa a nepribúda časom ani offline. Ak hráč nemá dosť Energy, Speed Up sa nedá použiť, hra ukáže, koľko Energy chýba, a obsluha beží ďalej. | Podklady, Tím |

# 3. Nefunkčné požiadavky

<table>
<colgroup>
<col style="width: 10%" />
<col style="width: 71%" />
<col style="width: 17%" />
</colgroup>
<thead>
<tr>
<th><strong>ID</strong></th>
<th><strong>Požiadavka</strong></th>
<th><strong>Zdroj</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>NFR-01</strong></td>
<td>Hra je určená pre mobilné zariadenia v orientácii na šírku. Beží ako natívna aplikácia pre Android alebo ako webová aplikácia v prehliadači Chrome pre Android.</td>
<td>Tím</td>
</tr>
<tr>
<td><strong>NFR-02</strong></td>
<td>Hra nemá game over. Hráčovi nikdy neodoberie coins, energy, budovy, levely ani odomknuté oblasti. Coins ubúdajú len nákupom a energy len boostom, ktorý hráč sám spustí. Výnimkou je new game. Slabšia vlna dá len nižší rating a menej coins, vždy však aspoň 1★ a nenulové coins.</td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>NFR-03</strong></td>
<td>Každá obrazovka má najviac jeden typ zvýrazneného tlačidla.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>NFR-04</strong></td>
<td>Počas vlny sa nezobrazuje skóre spokojnosti ani odpočty trpezlivosti hostí.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>NFR-05</strong></td>
<td><p>Parametre sú na jednom mieste v konfiguračnom súbore a ich zmena nevyžaduje úpravu kódu:</p>
<ul>
<li><p>vlny: dĺžka príchodu, tabuľka vĺn,</p></li>
<li><p>budovy: počty miest, čas obsluhy, podiel výkonu bez workeraa, ceny vylepšení a odomknutí,</p></li>
<li><p>hodnotenie: hranice čakania, podlahy, váhy needov, prahy ratingu, prah bottlenecku,</p></li>
<li><p>ekonomika: coins za jednotku, násobiče ratingu, prírastky Resort Value, počiatočné hodnoty,</p></li>
<li><p>energy: cena, účinok a trvanie boostu, zisk podľa ratingu.</p></li>
</ul></td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>NFR-06</strong></td>
<td>Texty v hre sú primárne v anglickom jazyku.</td>
<td>Podklady</td>
</tr>
<tr>
<td><strong>NFR-07</strong></td>
<td>Priemerná snímková frekvencia počas celej 8. vlny s piatimi budovami je na referenčnom zariadení aspoň 30 FPS.</td>
<td>Tím</td>
</tr>
<tr>
<td><strong>NFR-08</strong></td>
<td>Interaktívne prvky majú dotykovú plochu aspoň 48 × 48 dp (vo webovej verzii 48 × 48 CSS px).</td>
<td>Podklady, Tím</td>
</tr>
<tr>
<td><strong>NFR-09</strong></td>
<td><p>Hra sa dá pochopiť bez externého návodu. V prípade nejednoznačnosti zobrazí krátke textové nápovedy, napr:</p>
<ul>
<li><p>pri prvom spustení hry: cieľ hry a ako spustiť vlnu,</p></li>
<li><p>pri prvom bottlenecku, ak hráč do nastaviteľného času (predvolene 10 s) žiadneho workera nepresunie: že ťuknutím na need môže presunúť workera,</p></li>
<li><p>čo vylepšenie mení,</p></li>
<li><p>čo nová oblasť prinesie,</p></li>
<li><p>etc.</p></li>
</ul></td>
<td>Tím</td>
</tr>
<tr>
<td><strong>NFR-10</strong></td>
<td>Predvolené hodnoty parametrov spĺňajú pravidlá klienta WR1, WR7 a WR8: podlaha jednotky &lt; podlaha vlny &lt; hranica 3★; od 3. vlny okrem vĺn 4 a 7 nedosiahne najlepšie rozmiestnenie bez presunov, vylepšení a Energy 5★; rezort s budovami na najvyššom leveli dosiahne v každej vlne 5★ bez Energy. Overuje sa simuláciou vĺn bez grafiky.</td>
<td>Podklady</td>
</tr>
</tbody>
</table>
