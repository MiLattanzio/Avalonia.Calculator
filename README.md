# Avalonia Calculator

Porting Linux desktop e Ubuntu Touch di Uno.Calculator, con interfaccia Avalonia derivata dagli XAML mobile originali e il motore originale C++ Microsoft/Uno (CalcManager, CalcEngine e RatPack). Il progetto è autonomo: non richiede un checkout Uno o UTDotNet per compilare.

## Funzioni

- Standard con valutazione immediata; scientifica con precedenza, parentesi, trigonometria, funzioni inverse/iperboliche, potenze e fattoriale.
- Programmatore con HEX/DEC/OCT/BIN, 8/16/32/64 bit, operatori logici, shift, rotazioni ed editor dei 64 bit.
- Memorie multiple, aggiunta/sottrazione, richiamo e cancellazione; cronologia delle modalità standard e scientifica.
- Calcolo della differenza fra date e aggiunta/sottrazione di anni, mesi e giorni, con calendario gregoriano.
- 154 unità originali in 12 categorie, con nomi localizzati e conversioni affini per le temperature.
- Tema chiaro/scuro automatico: segue Lomiri su Ubuntu Touch e il tema di piattaforma sul desktop, anche durante l’uso. Vedere `docs/theme.md`.
- Lingua automatica dalla sessione del dispositivo: 65 risorse Uno, con fallback inglese per le voci mancanti; testi, errori, unità, accessibilità, numeri, date e menu copia/incolla. Dettagli in `docs/localization.md`.
- Valute: aggiornamento esplicito dei [tassi BCE](https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml), data del tasso visibile e possibilità di inserire un tasso manuale.
- Tastierino touch e tastiera desktop; copia/incolla dal menu del risultato (pressione prolungata o clic destro) e Ctrl+C/Ctrl+V. Nessun campo numerico aggiuntivo.

Su Ubuntu Touch l'app usa i pacchetti pubblici `UTDotNet.Avalonia` **0.1.6-preview.1**: Maliit, clipboard Content Hub, spazio per la tastiera, scala Lomiri e popup dentro la finestra. Sul desktop mantiene l'input Avalonia ordinario. Il percorso telefonico è X11/XWayland.

## Compilazione e avvio

Requisiti: Linux x64, .NET SDK 10, GCC/G++ con C++17 e le librerie desktop richieste da Avalonia. Le versioni Avalonia sono allineate a **12.1.6**; i file `packages.lock.json` fissano il grafo NuGet.

```sh
dotnet restore Avalonia.Calculator.slnx --locked-mode
dotnet build Avalonia.Calculator.slnx -c Release --no-restore
dotnet run --project src/Calculator -c Release --no-build
```

Aprire `Avalonia.Calculator.slnx` in Rider. La build .NET richiama `scripts/build-native.sh` e copia `libCalculatorEngine.so` nell'output. Non serve installarla nel sistema. Il primo build C++ richiede più tempo; i successivi riutilizzano l'artefatto se i sorgenti non sono cambiati.

La suite di regressione è un eseguibile .NET che termina con codice diverso da zero al primo errore:

```sh
dotnet run --project tests/Calculator.Tests -c Release
```

Comprende aritmetica e precisione, precedenza, errori, memoria, basi, storia, copia/incolla numerico, tutte le coppie di conversione, date, parser BCE e la UI Avalonia headless. Non sostituisce le prove del dispositivo.

Per acquisire la sola finestra desktop:

```sh
mkdir -p artifacts/verification
dotnet run --project src/Calculator -c Release -- \
  --screenshot="$PWD/artifacts/verification/desktop.png" --exit-after-screenshot
```

## Pacchetto Ubuntu Touch

Requisiti aggiuntivi: Docker, `desktop-file-validate`, immagine `clickable/amd64-ut24.04-2.x-arm64:latest` e ADB per l'installazione. `CLICK_BUILD_IMAGE` può fissare un'immagine diversa o un digest.

```sh
bash scripts/build-click.sh
```

Lo script compila il motore con il cross compiler dell'immagine Ubuntu 24.04, pubblica .NET `linux-arm64` self-contained senza trimming/AOT/single-file, costruisce il Click e registra hash, immagine e revisione. Il telefono non richiede .NET installato.

Output: `artifacts/click/avalonia.calculator.milattanzio_0.1.5_arm64.click`.

**Il reviewer disponibile non conosce la policy 2404.2**: il pacchetto viene costruito, ma lo script restituisce il codice 2 della review. Consultare `artifacts/click/review.txt`; non interpretare quel codice come una compilazione fallita, né l'installazione come approvazione OpenStore. Il validatore desktop segnala inoltre l'icona SVG relativa prevista dal packaging Click.

```sh
adb devices -l
bash scripts/install-click.sh SERIAL
```

Il pacchetto usa il profilo AppArmor standard, con il solo gruppo aggiuntivo `networking` per la BCE. Nessuna modifica alla rootfs, ai gruppi dell'utente o alle policy di sistema. Il launcher riceve display, sessione e scala da Lomiri e imposta `RESOURCE_NAME` per l'identità X11 confinata.

## Struttura e provenienza

- `native/CalcManager`: motore Microsoft/Uno, adattato per GCC Linux e AArch64.
- `native/CalculatorBridge.cpp`: API C con ownership esplicita, snapshot JSON e stringhe UTF-8; evita i vecchi wrapper Uno dipendenti da UWP e dalla dimensione di `wchar_t`.
- `src/Calculator/Engine`: adattatore gestito, comandi originali, catalogo unità e calcolo date.
- `src/Calculator/Views`: shell e stili AXAML, griglie dinamiche C#; non contiene dipendenze Uno/UWP.
- `src/Calculator/Services`: integrazione UTDotNet opt-in sul telefono e cambi BCE.
- `third-party`: licenze e provenienza dei sorgenti riutilizzati.
- `docs/device-testing.md`: risultati e limiti delle verifiche sul Fairphone 5.

Il motore nativo va usato da un solo thread UI. La UI traduce i layout compatti/mobile degli XAML Uno: intestazione da 40 DIP, menu da 256 DIP, righe proporzionali, tasti piatti e font originale delle icone. Su UT usa lo sfondo pieno del ramo mobile; la cronologia è un overlay nelle finestre strette. La corrispondenza fra file è descritta in `docs/uno-ui.md`. Cronologia e memorie sono mantenute per la sessione corrente; non c'è persistenza fra riavvii. Calendari diversi dal gregoriano e i precedenti target Android/iOS/WebAssembly non sono inclusi in questa versione Linux. La clipboard del bridge è testuale; Wayland nativo, AOT e trimming non sono abilitati.
