# agents.md — note per modificare MACEMU.NET

Port .NET 8 WinForms (solo Windows) dell'emulatore DOS MAC-1 (1996).
Exe rilasciato: `../MACEMU64.EXE` (self-contained). Originali DOS in `../BIN_ORIGINALI/`.

## Struttura

- `MacEmu.Core/` — logica pura, zero UI: `Machine.cs` (CPU 4 fasi + SysBus +
  datapath), `Mir.cs`, `State.cs`, `Devices.cs`, `Disassembler.cs`,
  `Loaders.cs`, `Asm/MacAssembler.cs` (ex MASM.EXE), `Asm/MicroAssembler.cs`.
- `MacEmu.UI/` — `EmuForm*.cs` (stati + menu + run), `EmuFormDialog.cs`
  (dialoghi a stati), `TextScreen.cs` (VRAM 80x50), `BcfLoader.cs`,
  `EmuFormDrawText.generated.cs` (**generato, non editare a mano**).
- `MacEmu.Tests/` — 17 test di fedeltà, asset copiati in output (mai path assoluti).
- `assets/Fonts/` — `Px437_IBM_EGA_8x8.ttf` (CC BY-SA 4.0, vedi LICENSE-int10h.txt).

## Regole di fedeltà

1. Ogni comportamento copiato dall'originale deve citare `file:riga` di `../SOURCE/`.
2. Nuove porter: prima il test di fedeltà (byte-identico vs `.MAC`/`.MIC`), poi il codice.
3. Eccezioni note e documentate: `TEST.MAC` rigenerato il 2026-09-26 da `TEST.ASM`
   (era stale dal 24/01/1996, pre-rework readint v4.0) e verificato headless
   (input "2" → k=2, HALT; ATTENZIONE: il programma fa `n--` dopo readint,
   quindi lavora sugli indici 0..n-1),
   `LENTO`/`SCHED`/`DEMOTASK` infiniti per design, bug `SHIFTER.C` (HALT+spazzatura
   su CBus) corretto, stub memoria 252-255 solo se `MACDOS.MAC` manca.

## Gotcha critici

- **Encoding**: i `.cs` sono UTF-8 senza BOM (box drawing). PowerShell
  `Get-Content` li legge male (mostra `â”‚`): usare sempre
  `[IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)`. Il compilatore C# li
  legge bene, non "riparare" i caratteri.
- **Rigenerare DrawText** da `../SOURCE/VISUAL.C` (sostituisce
  `EmuFormDrawText.generated.cs`, asserisce 50 righe x 80 col):
  ```powershell
  $e = [Text.Encoding]::GetEncoding(437)
  $t = $e.GetString([IO.File]::ReadAllBytes('..\SOURCE\VISUAL.C'))
  # estrai printc(0,y++,...) -> $rows; su rows[1]: -replace '"VER"','4.0c'
  # e -replace '\("COMP"\)  -','(.NET)   -'; verifica Length -eq 80 per ogni riga
  # emetti DrawTitleRow() (riga 1 + "A" a 71,1) e DrawTextBase() (altre 49 +
  # frecce ↑ U+2191 a 31,33,35,37,38,39,40,41,42,45,49,53,59 sulla riga 7)
  # scrivi UTF-8 senza BOM.
  ```
  La riga 1 NON è nel base: la disegna `DrawText()` via `DrawTitleRow()`
  (il supercar la ridisegna live).
- **Tastiera**: frecce/F-keys/Alt non arrivano sempre a `KeyDown` → tutto passa
  da `DispatchKey`, chiamato sia da `ProcessCmdKey` che da `OnKeyDown`, con
  guardia dedup 100ms. Caratteri via `KeyPress`; in RUN ignorare `\r` e spazio
  (gestiti da DispatchKey: CR→tastiera emulata per readint, Spazio=STOP).
- **Testo**: glyph-atlas 16x16 blittato a multipli esatti (mai posizionare con
  advance misurati). Font a 16px = celle 16x16 esatte (misurato via test).
  `OnPaint` centra 1:1 senza stretch; finestra `ClientSize` 1280x800.
- **MEMDISPLAY**: `video` originale è `short*` → ogni word = 2 pixel:
  viewport **128x64 a (96,68)**, pixel sx = byte basso. Palette 256 colori
  presa da `MEMVIEW.BCF` (come il DAC reale). Upscale x4 NearestNeighbor.
- **Dialoghi**: macchine a stati (`IDialog`); Invio conferma, ESC annulla/chiude
  ovunque; layout a due righe tipo `Loaded N`/`instructions` (mai oltre col 77).
- **Mai path assoluti nei sorgenti**: UI usa `AppDomain.BaseDirectory`, test con
  `FindRoot()` + asset copiati in output via csproj.
- **Byte originali, non rendering**: i caratteri nei `.C` si verificano in esadecimale
  (`od -t x1`), mai fidarsi di come il terminale li mostra. Lezione: `Key()` in
  `MENU.C` usa DUE caratteri diversi (ombra sotto `0xDC` ▄, lato `0xDF` ▀).
- **Alt morti? Colpa esterna prima che nostra**: overlay/hotkey di sistema mangiano
  combo a livello OS (verificato: Alt+R mai recapitato su un PC). Diagnosi con
  harness a tasti veri (`keybd_event`) contro `EmuForm` reale + `IMessageFilter`
  (se il messaggio non è in coda, l'app non c'entra). Alt+G non è mai esistito
  (solo I/R/A/J da manuale).
- **Bell udibile**: stampante bell (7) → beep kernel 880Hz async (`Console.Beep`
  via ThreadPool, NON `SystemSounds` legato allo schema audio), skip se speaker
  occupato (niente throttle: sfasa i rimbalzi di Arcanoid).
- **Shell = git-bash**: niente cmdlet PowerShell inline; opzioni MSBuild con `-p:`,
  non `/p:`. Niente `zip`/`7z`: zip via `Expand-Archive`/`Compress-Archive`.

## Build / rilascio

```powershell
Get-Process MacEmu.UI,MACEMU64 -EA SilentlyContinue | Stop-Process -Force  # sblocca i file!
dotnet build MacEmu.sln
dotnet test MacEmu.Tests/MacEmu.Tests.csproj   # attesi 17/17
dotnet publish MacEmu.UI/MacEmu.UI.csproj -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true /p:InvariantGlobalization=true `
  /p:DebuggerSupport=false /p:MetadataUpdaterSupport=false /p:UseSystemResourceKeys=true
Copy-Item MacEmu.UI/bin/Release/net8.0-windows/win-x64/publish/MacEmu.UI.exe ../MACEMU64.EXE -Force
# ../Fonts/, ../MACRO/, ../MICRO/, ../*.BCF, ../MACDOS.MAC devono stare accanto all'exe
# Zip locale: Expand-Archive dello zip, sostituisci MACEMU64.EXE (+asset nuovi), Compress-Archive
# Se Copy-Item dice "busy": l'exe è aperto dall'UTENTE (screenshot/test) → NON killare,
# chiedi di chiuderlo e riprova.
# Release GitHub (gh autenticato come ciskje): tagga da HEAD e carica lo zip
# come asset, altrimenti la release resta ferma al commit vecchio.
```

Limiti noti: niente trimming con WinForms (errore NETSDK1175) → self-contained
~150MB; UPX inutile (bundle già compresso). Icona da `../icona.png` via
`assets/macemu.ico` (`ApplicationIcon` nel csproj).
