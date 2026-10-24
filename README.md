# resort-empire
Resort Empire: Paradise Tycoon – tímový projekt (Softvérové inžinierstvo, UKF), Unity 2D, Android 8.0 a novší, referenčné zariadanie je: Xiami POCO X5 Pro 5G

Pravidlá práce s Gitom (GitFlow): [CONTRIBUTING.md](CONTRIBUTING.md)

## Unity

- Verzia **6000.3.26f1** (Unity 6.3 LTS), šablóna Universal 2D (URP s 2D rendererom).
- Presnú verziu nainštaluješ v Unity Hub cez **Installs → Install Editor**. Ak v zozname nie je, nájdeš ju na záložke **Archive** cez odkaz na download archive.
- Projekt otvor v Unity Hub cez **Add → Add project from disk** a vyber priečinok repozitára.
- Iná verzia Unity projekt pri otvorení prevedie a zmení súbory v `ProjectSettings` a `Packages`, preto otváraj projekt len v tejto verzii.
- Na build APK treba v Unity Hub doinštalovať modul Android Build Support (SCRUM-109).

## Build APK

1. Otvor **File → Build Profiles → Android**. Ak Android nie je aktívny profil, daj **Switch Platform**.
2. V **Scene List** musí byť hlavná scéna zaškrtnutá. Ak chýba, otvor ju a daj **Add Open Scenes**.
3. **Build App Bundle (Google Play)** nechaj vypnuté, inak vznikne `.aab` namiesto `.apk`.
4. **Development Build** zapni pri testovaní. Pre build na odovzdanie ho vypni.
5. Klikni na **Build** a ulož APK do priečinka `Builds/` v koreni projektu (vedľa `Assets`, nie v ňom), napr. `ResortEmpire-0.1.apk`.

`Builds/` ignoruje `.gitignore`. Ak sa APK objaví medzi zmenami v GitHub Desktop, necommituj ho. Prvý build trvá niekoľko minút, ďalšie sú rýchlejšie.

Nastavenia ako package name, verzia a orientácia sú uložené v `ProjectSettings`, takže ich po klonovaní netreba nastavovať znova.

## Inštalácia do telefónu

- **Build na PC:** skopíruj APK do telefónu (Google Drive, e-mail, USB), otvor ho a povoľ inštaláciu z neznámych zdrojov.
- **Káblom cez Build And Run:**
  1. V telefóne zapni vývojárske možnosti: *Nastavenia → Informácie o telefóne → 7× ťukni na Číslo zostavy*.
  2. Vo vývojárskych možnostiach zapni **USB ladenie**.
  3. Povoľ inštaláciu aplikácií cez USB. Nastavenie sa líši podľa výrobcu:
     - **Xiaomi, Redmi, POCO:** vo vývojárskych možnostiach zapni **Inštalovať cez USB** (*Install via USB*). Telefón zvyčajne vyžaduje prihlásenie do Xiaomi účtu a vloženú SIM kartu.
     - **Samsung:** v *Nastavenia → Zabezpečenie a súkromie* vypni **Automatický blokovač** (*Auto Blocker*).
     - **Oppo, Realme, OnePlus:** vo vývojárskych možnostiach povoľ inštaláciu cez USB, prípadne zapni *Disable permission monitoring*.
  4. Pripoj telefón k PC a potvrď na ňom povolenie.
  5. V Build Profiles vyber telefón v **Run Device** a daj **Build And Run**. Ak sa na telefóne zobrazí potvrdenie inštalácie, potvrď ho hneď, inak ju telefón po pár sekundách zruší. Unity potom hru nainštaluje a spustí.
