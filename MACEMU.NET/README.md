# MACEMU 4.0c — port .NET 8 (WinForms, solo Windows)

Porto fedele dell'emulatore DOS MAC-1 (Borland C 16/32 bit, 1996) in .NET 8.
L'eseguibile è `MACEMU64.EXE` nella root (i binari DOS originali sono in
`BIN_ORIGINALI/`). È self-contained (~150MB: runtime .NET integrato, gira
senza installare nulla; il bundle single-file è già compresso
internamente — UPX/trimming non riducono oltre: il trimming è vietato
da .NET per WinForms, errore NETSDK1175).

## Struttura

- `MacEmu.Core/` — logica pura, nessun riferimento UI (port 1:1 dei `.C/.H`):
  `Machine.cs` (CPU 4 fasi + SysBus + datapath), `Mir.cs`, `State.cs`
  (registri/latch/CBus/MPC/CStore/Memoria/MAR-MBR), `Devices.cs`
  (tastiera/stampante/timer), `Disassembler.cs`, `Loaders.cs` (`.MAC`/`.MIC`),
  `Asm/MacAssembler.cs` (port di `MASM.EXE`), `Asm/MicroAssembler.cs`
  (port di `MICA.EXE`, valida `CSTORE.PRE` → `CSTORE.MIC`).
- `MacEmu.UI/` — replica dell'interfaccia DOS: `TextScreen.cs` (VRAM 80x50),
  `EmuForm*.cs` (main menu + running menu + dialoghi), `BcfLoader.cs` (splash `.BCF`),
  `Vga.cs` (palette/attributi). Gli asset originali (`MACRO/`, `MICRO/`,
  `MACDOS.MAC`, `*.BCF`) sono copiati accanto all'exe.
- `MacEmu.Tests/` — 17 test di fedeltà (xUnit).

## Build / run

```powershell
dotnet build MacEmu.sln
dotnet test MacEmu.Tests/MacEmu.Tests.csproj
dotnet publish MacEmu.UI/MacEmu.UI.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

Serve il runtime .NET 8 Desktop. Tasti identici all'originale
(F1 RUN, F2 micro, F3 macro, F4–F6 registri/breakpoint, F7/F8/F9/F10 video,
PgUp/PgDn scroll, Alt+I/R/J/A, ESC indietro).

## Fedeltà verificata (17/17 test)

- Assembler macro byte-identico per 9 `.ASM`→`.MAC` + `MACDOS.ASM` (origin `0x10`).
- Microassembler byte-identico per tutte le 116 microword di `CSTORE.MIC`
  (incluso swap A/B su conflitto bus MAR/MBR e `halt` con `SH=3`).
- Emulatore: `FIBO`, `CIRCLES`, `FLOWERS` girano fino a `HALT` con microprogramma
  e S.O. originali.

## Note

- Finestra 1280x960 (non più tutto schermo); testo 80x50 a pixel 1:1
  (1280x800) centrato, grafica MEMDISPLAY 640x400 (x2 NearestNeighbor).
- Font testo: `Px437 IBM EGA 8x8` (i glifi 8x8 del modo 80x50) da
  *The Ultimate Oldschool PC Font Pack v2.2* di VileR (int10h.org),
  licenza **CC BY-SA 4.0** (vedi `assets/Fonts/LICENSE-int10h.txt`),
  caricato privatamente senza installazione.
- `TEST.MAC` è stale (24/01/1996) rispetto a `TEST.ASM` (03/02/1996, rework
  readint v4.0): non è byte-uguagliabile; l'ASM viene comunque assemblato
  correttamente (`CALL readint` = `E013`, come `INSTR.ASM` dimostra).
- `LENTO`/`SCHED`/`DEMOTASK` non terminano per design (loop + trap tastiera);
  esclusi dai run-test.
- Timing RUN: batch di microistruzioni per tick 30 ms scalato su `rates`
  (l'originale andava a velocità CPU DOS); `NODISPLAY` = massima velocità.
- `MEMDISPLAY`: pixel 64x64 scalati sopra `MEMVIEW.BCF` (stesso offset 48,68).
- Bell stampante ignorato; `BlankScreen`/`Blinking` non replicati (splash statico).
- `MASM.EXE` originale resta utilizzabile via DOSBox; l'assembler integrato
  produce gli stessi `.MAC` (verificato dai test).
