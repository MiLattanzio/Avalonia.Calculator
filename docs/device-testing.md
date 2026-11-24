# Verifiche del 24 novembre 2026

## Port iniziale 0.1.1 — ambiente e artefatto

- Fairphone 5, dispositivo `FP5`, AArch64; Ubuntu Touch `24.04-2.2`, build 3, canale `24.04-2.x/arm64/android9plus/stable`.
- Utente della sessione `phablet`, UID 32011; grid Lomiri 24, scala Avalonia 3. Questi sono dati osservati, non valori fissati nel launcher.
- Avalonia 12.1.6, UTDotNet.Avalonia/Core/UbuntuTouch/Diagnostics 0.1.6-preview.1 da nuget.org, SDK .NET 10.0.404; runtime pubblicato .NET 10.0.15.
- X11/XWayland. Il motore C++ ARM64 è compilato nel container Ubuntu 24.04; nessun runtime .NET installato nel sistema del telefono.
- Pacchetto finale: `artifacts/click/avalonia.calculator.milattanzio_0.1.1_arm64.click`, 42.230.608 byte.
- SHA-256 Click: `a2da7bcc55843bf2a21285a48d00438ca4e523e03f95e28a7c81ad44324ef094`.
- Immagine di build: `sha256:0a69f2c6c52a9d33516ce210eb5cb374ca93905985d62820d9be5a351647b5ba`.
- Assembly gestito SHA-256: `51de6bf671d36edbfb10ca06627af50c0984a0144c71f07e1da5f79c56e4df5f`.
- Motore ARM64 SHA-256: `0ac495a078574ed33b2ae4fa9ebcb307cf69aeedc67fc96605fa79491f659bc5`.

Gli ultimi due hash sono stati confrontati con i file effettivamente installati sotto `/opt/click.ubuntu.com/avalonia.calculator.milattanzio/0.1.1/app/`: coincidono. Il processo finale osservato era PID 54350 con profilo `avalonia.calculator.milattanzio_calculator_0.1.1 (enforce)`. Il solo gruppo aggiuntivo è `networking`, necessario ai cambi BCE.

## Verifiche sul computer

| Verifica | Esito |
| --- | --- |
| Restore soluzione con `--locked-mode` | Pass |
| Build Release della soluzione | Pass, 0 errori e 0 avvisi |
| Suite eseguibile di regressione | 37 verifiche passate, nessun errore |
| Precisione originale | 0,1 + 0,2; somma su 30 cifre; scientifica con precedenza e parentesi |
| Motore | Percentuali, trigonometria, fattoriale, radice, errori e recupero, memoria, basi, AND, shift, cronologia |
| Input numerico | Validazione atomica, separatori, copia di numeri raggruppati e notazione esponenziale |
| Unità e date | Identità di 154 unità, round trip di tutte le coppie per categoria, temperature, miglia/km, bisestili e ordine di sottrazione |
| Valute | Parser e tassi incrociati, rifiuto di NaN, fonte/data preservate dopo il `TextChanged` differito di Avalonia |
| Avalonia headless | Tasti collegati al motore, navigazione in 17 modalità, conversione su editing e dimensioni portrait/landscape |
| Desktop reale | Avvio X11 nella sessione del computer, rendering PNG e uscita automatica senza errori |
| ARM64 | Publish self-contained riuscito; `libCalculatorEngine.so` ELF AArch64 |

La suite si esegue con `dotnet run --project tests/Calculator.Tests -c Release`. Non è un progetto VSTest: `dotnet test` non esegue queste verifiche.

## Verifiche sul telefono

Le interazioni sono state effettuate con **tocchi iniettati tramite evdev**, usando il touchscreen `goodix_ts` già accessibile all'utente della sessione. Non sono prove effettuate da una persona sul vetro. Non sono stati modificati permessi, gruppi, rootfs o impostazioni di blocco/rotazione del telefono.

| Verifica | Risultato osservato |
| --- | --- |
| Installazione e avvio Click | Pass: servizio `com.lomiri.click.Install`, avvio Lomiri, processo confinato |
| Calcolo touch | `12 + 34 = 46`, ripetuto sul pacchetto finale; `fp5-final.png` |
| Menu modalità | Apertura, scorrimento e scelta tramite tocchi; overlay senza perdita della finestra |
| Maliit numerico | Focus sul campo, ricezione di `57`, backspace, nuova cifra `2`, applicazione del numero `52` al motore |
| Occlusione | Il contenuto scorrevole riserva l'area notificata da Maliit e il campo resta visibile; ritorna al layout completo dopo "Usa numero" |
| Selezione e menu testo | Pressione prolungata, maniglie e menu originale Avalonia Cut/Copy/Paste con dimensioni touch |
| Calcolatrice → Qt | Copia del risultato `52` dal Click; il probe Qt ha letto esattamente `52`, log `CALCULATOR_TO_QT=true` |
| Qt → calcolatrice | Il probe ha copiato `314.159`; Paste dal menu della TextBox ha sostituito la selezione con quel valore |
| Ritorno da un'altra app | Ritorno dal probe Qt, campo ancora utilizzabile e menu incolla funzionante |
| Cambi BCE | Download HTTPS effettuato dal Click; conversione EUR/USD e fonte/data mostrate. Ripetuto sul pacchetto finale dopo la correzione del `TextChanged` differito |

Le prove Maliit e Qt sono state eseguite sul primo Click 0.1.0 con lo stesso NuGet, stessi servizi e stessa registrazione dei campi del Click finale. Sulla build finale 0.1.1 sono stati ripetuti avvio, controllo hash/confinamento, calcolo touch, navigazione e aggiornamento BCE. Non si attribuisce una nuova esecuzione delle prove Qt a ogni ricompilazione intermedia.

Le catture della finestra sono ottenute da X11. Il socket del compositor della sessione `/run/user/32011/mir_socket` ha consentito catture, ma non comprendeva la superficie della tastiera del compositor di sistema; `/run/mir_socket` restituiva `Failed to obtain buffer stream from screencast`. **Non c'è una nuova cattura della tastiera completa**: il test Maliit è sostenuto dall'input ricevuto, dall'editing e dall'occlusione osservati, non da una dichiarazione di visibilità basata sul solo acknowledgement.

## Packaging e limiti

`click-review` termina con codice 2 per le sole verifiche `security:policy_version_exists` e `security:policy_groups_exists`: la versione del reviewer presente nell'immagine non conosce la policy `2404.2`. È rimasto anche il warning del validatore desktop sull'icona SVG relativa. Installazione e AppArmor enforce non equivalgono ad approvazione OpenStore.

In alcune invocazioni il comando `lomiri-app-launch` ha scritto `Lost our connection with the registry` dopo `Started`; l'app era comunque attiva. L'avvio è stato verificato con processo, profilo, hash e interazione, non con il solo messaggio del launcher. Nei log grafici compaiono avvisi del driver gralloc/Zink; il rendering e le operazioni provate sono riusciti.

Non verificati in questa sessione: rotazione fisica e tastiera in landscape, blocco/sblocco, sospensione reale, sessioni lunghe, scambio con Morph, altre lingue IME e altri dispositivi. Le verifiche storiche del bridge non sono riportate come prove di questa app.

## Evidenze locali

`artifacts/verification/` (ignorato da Git) contiene `tests.log`, `build.log`, `restore.log`, `click-build.log`, `final-install.log`, `final-device.log`, `native-clipboard.log`, `desktop.png`, `fp5-final.png`, `fp5-native-input.png`, `fp5-native-accepted.png`, `fp5-paste-menu.png`, `fp5-qt-to-calculator.png` e `fp5-currency-final.png`. `artifacts/click/` contiene pacchetto, SHA256SUMS, review e identità dell'immagine. `tests/ClipboardProbe.qml` conserva il probe nativo usato.


## Aggiornamento UI mobile 0.1.2

La UI è ricostruita a partire dagli XAML mobile Uno; la mappa dei file e le misure
sono in `uno-ui.md`. Rimosso completamente il campo «Inserisci numero», riportati
menu laterale, icone originali, display destro, tasti piatti e griglie delle singole
modalità. Copia/incolla è nel menu contestuale del risultato; i convertitori hanno
il proprio tastierino. Il tasto ± dei convertitori è visibile solo per temperatura,
come `CurrentCategory.NegateVisibility` dell'originale.

- Click finale: `avalonia.calculator.milattanzio_0.1.2_arm64.click`, 42.332.188 byte.
- SHA-256: `c77704d71acf9fd7e1bddadb69242124df37c545577b4474d5caf44cdcccc126`.
- Assembly ARM64: `6945ba8fdc20873d819adb4b4cbbe0504b2b0da74de8b5fe5c40185c07a4416c`.
- Motore ARM64 invariato: `0ac495a078574ed33b2ae4fa9ebcb307cf69aeedc67fc96605fa79491f659bc5`.
- Build Release: 0 errori, 0 avvisi. Suite: **44 verifiche passate**.
- Installazione finale verificata: PID 55455, profilo `avalonia.calculator.milattanzio_calculator_0.1.2 (enforce)`; hash del DLL e del motore sul telefono identici agli artefatti locali.
- Ripetuti sul Click finale: `12 + 34 = 46`, cronologia, menu laterale, scientifica, programmatore e conversione `12 pollici = 1 piede`. La calcolatrice è lasciata aperta in Standard sul risultato 46.
- Nuove regressioni UI: posizione dei tasti standard; assenza dell'editor rimosso;
  drawer mobile; seconda funzione e HYP; basi/bit; tastierino convertitori; stato
  DEG e QWORD coerente col motore dopo la navigazione; dock largo e ritorno stretto.

Le prove della UI sul Fairphone usano ancora tocchi evdev, non dita sul vetro.
Nelle build 0.1.2 di verifica sono stati osservati `12 + 34 = 46`, `2² = 4`,
pressione prolungata sul risultato, **calcolatrice → Qt: 4** e **Qt → calcolatrice:
314.159** direttamente nel display. Il probe adattato alla prova del valore 4 è
conservato in `artifacts/verification/CalculatorClipboardProbe4.qml`, con log
`ui-native-clipboard.log` (`CALCULATOR_TO_QT=true`). L'incolla sul nuovo display è
visibile in `fp5-uno-pasted.png`. Non serve più aprire un editor ausiliario.

Le prove clipboard precedono le ultime rifiniture del menu e la sincronizzazione
DEG/QWORD: i servizi clipboard e i relativi handler non sono cambiati dopo tali
prove. Sul Click finale sono ripetute navigazione, layout mobile, calcolo e
convertitore. Rimangono i limiti di acquisizione del compositor già descritti
sopra e l'errore del reviewer sulla policy 2404.2; l'app è installata con AppArmor
enforce, senza modifiche al sistema.

Evidenze 0.1.2: `ui-build.log`, `ui-tests.log`, `ui-click-build.log`,
`ui-final-install.log`, `ui-final-device.log`, `ui-final-touch.log`,
`fp5-uno-standard.png`, `fp5-uno-calculation.png`, `fp5-uno-navigation.png`,
`fp5-uno-history.png`, `fp5-uno-scientific.png`, `fp5-uno-programmer.png`,
`fp5-uno-converter.png`, `fp5-uno-final.png`, `fp5-uno-result-menu.png`,
`fp5-uno-pasted.png`. Le copie `.preview.png` sono ridotte solo per ispezione;
i PNG completi conservano le dimensioni X11 1224×2595.


## Localizzazione 0.1.3 — 24 novembre 2026

- Importate le 65 risorse Uno; testi e formati inizializzati dalla sessione Lomiri.
  Sul Fairphone: `LANGUAGE=it_IT`, `LANG=it_IT.UTF-8`,
  `LC_NUMERIC=it_IT.UTF-8`, `LC_TIME=it_IT.UTF-8`. La locale inglese della
  shell ADB non viene usata per decidere la lingua dell'app.
- **62 verifiche automatiche superate**: risorse/unità delle 65 lingue con
  fallback, selezione POSIX, cifre Unicode, aritmetica e precisione in sette
  lingue; UI mobile, cronologia, valute e date in cinque lingue.
- Rendering desktop a 408×740 DIP: italiano (lunghezza), francese (date),
  arabo (menu RTL) e giapponese (valute), con glifi leggibili.
- Touch Fairphone: `0,1 + 0,2 = 0,3`; memoria, Copia/Incolla Content Hub e
  cronologia conservano `0,3`. Divisione per zero mostra
  «Impossibile dividere per zero». Calcolo date mostra «novembre» e «Stesse date».
- Valute sul telefono: valore e rapporto `1,0939`, data `23/11/2026`,
  campo del tasso manuale precompilato con la virgola.
- Corrette due regressioni evidenziate dai test: separatore C++ non inizializzato
  prima di `SettingsChanged` e richiamo ricorsivo della cronologia durante
  l'aggiornamento della selezione. Ignorati i marcatori direzionali isolati
  nell'input della tastiera; accettate le cifre decimali Unicode.

Pacchetto finale `avalonia.calculator.milattanzio_0.1.3_arm64.click`,
42.795.788 byte:

```text
Click SHA-256: 41228bab782a7d4422df883462d063301e732c6d86c3522102f647625ef5d730
DLL SHA-256:   70118596511400c708d40580b65f23c692bb5e37d48b7da3596911e455542bfe
Engine SHA-256:19f17e63f3e6f7a5fe70dc59f124049daece346f22207c61a4e927ec19de3186
```

Hash installati uguali agli artefatti locali; profilo
`avalonia.calculator.milattanzio_calculator_0.1.3 (enforce)`.
Il reviewer Click restituisce ancora 2 perché non conosce `ubuntu/2404.2`;
compilazione e installazione sul telefono sono riuscite. L'utility di lancio
ha stampato anche «Lost our connection with the registry» dopo «Started»;
il processo dell'app è rimasto attivo e le prove touch sono riuscite.

Evidenze in `artifacts/verification`: `localization-tests.log`,
`localization-click-build.log`, `localization-install.log`,
`localization-device-state.log`, `localization-fp5-*.png`,
`localization-ar-menu.png`, `localization-fr-dates.png`,
`localization-ja-currency.png` e `localization-it-length.png`.
I limiti del fallback e dei calendari sono descritti in `localization.md`.


## Ripristino della tastiera 0.1.4 — 24 novembre 2026

Riprodotto il problema della 0.1.3: aprendo e chiudendo il tasso manuale,
la tastiera lasciava nella shell l'ultimo margine inferiore ricevuto da Maliit.
Il popup poteva staccare il campo dalla finestra prima dell'evento di area vuota;
`HideKeyboardAsync` da solo non rilasciava la sessione né azzerava il layout.

La gestione comune in `PhoneIntegration` ora ripristina subito il padding
originale e usa `DeactivateAsync` del NuGet UTDotNet esistente. La chiusura del
flyout rilascia l'input prima di staccare il campo. La stessa pulizia si applica
ai cambi di modalità, ai campi nascosti/rimossi, alla perdita di focus e alla
disattivazione della finestra. Le notifiche tardive della vecchia sessione non
possono ripristinare il margine; il passaggio fra editor attivi resta consentito.

**71 verifiche automatiche superate**, di cui nove nuove prove con il vero
`MaliitInputBridge` del pacchetto e una sessione nativa simulata: chiusura con
clic esterno ed Escape, hide senza callback, riapertura mentre il vecchio hide
è in corso, eventi tardivi, navigazione in tutte le 17 modalità, anni/mesi/giorni,
editor nascosto o staccato, hide/disconnessione del server, disattivazione,
ridimensionamento e menu clipboard.

Sul Fairphone, dopo la chiusura del tasso vuoto, l'intera cattura 1224×2595
è identica pixel per pixel a quella precedente all'apertura. Dopo aver digitato
`4` sulla tastiera nativa, chiusure e riaperture conservano il tasso e ripristinano
il layout completo; anche il ritorno alla modalità Standard mantiene tutta
l'altezza disponibile. Digitato inoltre `4` su Maliit nei campi anni, mesi e
giorni, verificando la conservazione dei valori e il risultato `28/03/2031`.
Il passaggio a differenza date, la riapertura degli editor e la navigazione
verso Standard e Lunghezza non lasciano spazi vuoti. La sessione finale
resta in Standard con `12 + 34 = 46`, senza riavvii durante queste prove.
Il confronto delle catture è registrato in `keyboard-device-comparison.json`.

Pacchetto `avalonia.calculator.milattanzio_0.1.4_arm64.click`, 42.797.316 byte:

```text
Click SHA-256: 26212c9c55735011bd286c0f66afd84787a56466484c5c6d9f5c5e8683a94d43
DLL SHA-256:   6b8d9ce8f486a73969fe36529b68f953fedab4aad9d00e1ea4a80e9a10c3bf6a
```

Il pacchetto usa ancora `UTDotNet.Avalonia` 0.1.6-preview.1; nessuna modifica ai
sorgenti di Uno, Avalonia o UTDotNet. Resta il limite già documentato del reviewer
Click, che non conosce la policy 2404.2. Evidenze in `artifacts/verification`:
`keyboard-tests.log`, `keyboard-click-build.log`, `keyboard-install.log`,
`keyboard-device-state.log` e `keyboard-fp5-*.png`. Le catture
`localization-fp5-keyboard-before-fix-*.png` mostrano la regressione originale.


## Tema automatico 0.1.5 — 24 novembre 2026

Il Fairphone usa `[General] theme=Lomiri.Components.Themes.SuruDark` in
`~/.config/lomiri-ui-toolkit/theme.ini`. La preferenza GNOME riporta invece
`default`: per Ubuntu Touch viene quindi letta quella del toolkit Lomiri,
accessibile anche dal profilo Click confinato, senza nuovi permessi.

**79 verifiche automatiche superate**, incluse otto prove sul tema: parsing e
percorsi attuali/legacy, aggiornamento a caldo, errori temporanei di lettura,
riattivazione, chiusura, conservazione dello stato, tutte le 17 modalità,
bit attivi/disabilitati e popup aperti. La prova iniziale sul telefono ha
evidenziato che XMir manteneva il focus X11 su `PointerRoot` (ID 1): il controllo
del tema non deve dipendere da `Window.IsActive`. Aggiunta una regressione per
una finestra visibile ma mai attivata; il timer si ferma quando viene nascosta
o chiusa, mentre Lomiri gestisce la sospensione del processo in background.

Prove touch con il pacchetto finale, senza `--theme` e senza riavvii durante i
cambi di preferenza (PID 59251):

- Avvio direttamente in scuro; `12 + 34 = 46` e salvataggio in memoria.
- Cambio temporaneo a `Ambiance`, poi ripristino esatto dei byte originali di
  `theme.ini`: stessa finestra, risultato e memoria. La cattura scura ripristinata
  è identica pixel per pixel a quella iniziale.
- Tasso manuale aperto, digitazione di `4` sulla tastiera Maliit e cambio
  scuro → chiaro → scuro: popup, testo e conversione restano presenti e leggibili.
- Chiusura, riapertura e seconda chiusura del dialogo: stesso layout completo
  1224×2595. Il tastierino dopo la chiusura coincide pixel per pixel con quello
  precedente all'apertura; anche le due catture successive alla chiusura sono
  identiche. Nessuna regressione del ripristino tastiera della 0.1.4.
- Controllo visivo di menu, Scientifica, Programmatore e Calcolo della data.
  Sessione lasciata in Standard, con il tema scuro originale del telefono.

Pacchetto `avalonia.calculator.milattanzio_0.1.5_arm64.click`, 42.797.674 byte:

```text
Click SHA-256: b2f2606395bf5c06cf43eca20c1b594099dee8643cdaeecf958ba9da35c6d30f
DLL SHA-256:   94bb0fe6940cbcf7bead2a929e31054a925595b5285a6a9b362c10c2ef42815b
Engine SHA-256:19f17e63f3e6f7a5fe70dc59f124049daece346f22207c61a4e927ec19de3186
```

Hash installati uguali agli artefatti locali; profilo
`avalonia.calculator.milattanzio_calculator_0.1.5 (enforce)`.
Resta il limite del reviewer Click già documentato per la policy `2404.2`.
Il messaggio dell'utility di lancio «Lost our connection with the registry»
non ha interrotto il processo dell'app, rimasto attivo durante tutte le prove.

Evidenze in `artifacts/verification`: `theme-tests.log`, `theme-click-build.log`,
`theme-install.log`, `theme-device-state.log`, `theme-device-comparison.json`,
`theme-fp5-*-comparison.json`, `theme-fp5-*.png` e `theme-dark-standard.png`.
Le preferenze originali usate per il ripristino sono salvate nei file
`theme-fp5-*-original.ini`. Dettagli della gestione in `theme.md`.
