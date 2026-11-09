# Práca s Gitom

Repozitár používa GitFlow podľa metodiky predmetu, zjednodušený na dve trvalé vetvy a feature vetvy.

> **Pozor: vetvy `main` a `develop` nie sú technicky chránené.** Súkromný repozitár na pláne GitHub Free ochranu vetiev nemá. GitHub ťa preto nezastaví, keď pushneš priamo do `main` alebo `develop`, zmerguješ pull request bez schválenia alebo spravíš force push. Všetky pravidlá nižšie sú dohoda tímu:
>
> - pred každým pushom skontroluj vetvu: `git branch --show-current`,
> - nikdy nerob `git push --force` do `main` ani `develop`,
> - keď sa niečo dostane do `main` alebo `develop` omylom, napíš to tímu a oprav to cez `git revert` v pull requeste, nie prepisovaním histórie.

## Vetvy

| Vetva | Na čo slúži |
|---|---|
| `main` | Verzie odovzdané na konci šprintu. Každý stav má tag. |
| `develop` | Priebežná práca tímu, default vetva repozitára. |
| `feature/SCRUM-<číslo>-<popis>` | Jedna úloha z Jiry, napr. `feature/SCRUM-26-tri-budovy`. Vzniká z `develop` a vracia sa do `develop`. |

Do `main` ani do `develop` sa priamo nepushuje, všetko ide cez pull request.

## Postup pri úlohe

1. Aktuálny `develop`:
   ```
   git switch develop
   git pull
   ```
2. Feature vetva s kľúčom úlohy z Jiry:
   ```
   git switch -c feature/SCRUM-26-tri-budovy
   ```
3. Správa commitu začína kľúčom úlohy, napr. `SCRUM-26 Pridaj tri budovy na ostrov`.
4. Push vetvy:
   ```
   git push -u origin feature/SCRUM-26-tri-budovy
   ```
5. Pull request do `develop`. Názov začína kľúčom úlohy. Pred žiadosťou o review spusti Edit Mode testy v Unity Test Runneri.
6. Pull request zmerguj až po schválení od iného člena tímu; autor nemôže schváliť vlastný. GitHub to nevynúti, je to dohoda.
7. Po schválení merge cez merge commit (squash a rebase sú v repozitári vypnuté), potom feature vetvu zmaž.

## Koniec šprintu

1. Pull request z `develop` do `main`, schválenie a merge.
2. Na `main` tag verzie a GitHub Release s APK z tohto šprintu, napr.:
   ```
   git switch main
   git pull
   git tag -a v0.1 -m "Sprint 0"
   git push origin v0.1
   ```

## Release a hotfix vetvy

GitFlow z prednášky má aj vetvy `release/*` a `hotfix/*`. Zatiaľ ich nepoužívame:

- `release/*` vzniká z `develop` na stabilizáciu pred odovzdaním a merguje sa do `main` aj späť do `develop`,
- `hotfix/*` vzniká z `main` pri kritickej chybe v odovzdanej verzii a merguje sa do `main` aj do `develop`.

## Pravidlá

- `git config user.email` musí byť e-mail prepojený s tvojím GitHub účtom. Inak sa commity nezapočítajú do grafu Contributors, podľa ktorého sa dá hodnotiť aktivita.
- Jednu scénu (`.unity`) naraz upravuje len jeden človek. Prácu delte do prefabov, lebo konflikty v scénach sa riešia ťažko.
- Priečinky `Library`, `Temp`, `Logs` a buildy do repozitára nepatria; rieši to `.gitignore`.
