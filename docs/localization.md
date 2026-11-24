# Localizzazione (0.1.3)

L'app sceglie la lingua all'avvio dalla **sessione che la lancia**. Su Ubuntu
Touch il launcher Lomiri passa `LANGUAGE`, `LANG`, `LC_NUMERIC` e `LC_TIME`;
la shell ADB può avere impostazioni diverse. Non si modifica la lingua del telefono.
Dopo un cambio di lingua di sistema, chiudere e riaprire l'app.

## Risorse e formati

`Localization/Resources` incorpora le 65 lingue del checkout Uno indicato in
`third-party/SOURCES.md`. Menu, modalità, 154 unità e simboli, cronologia,
memoria, date, messaggi di errore e nomi accessibili usano le chiavi originali.
Le due etichette aggiuntive (Taglia e Tasso manuale) sono in `ExtraStrings.cs`.
Anche il nome del launcher Click è localizzato.

Il fallback cerca la variante esatta, poi la lingua/script supportati, infine
`en-US`. Le singole risorse assenti nell'originale (ad esempio parte della
traduzione Hausa) usano l'inglese. I nomi dei comandi matematici e i codici ISO
delle valute conservano la notazione originale. Le traduzioni importate non
sono state sottoposte a una nuova revisione linguistica di tutte le 65 lingue.

- Lingua: preferenze `LANGUAGE` separate da `:`, poi `LC_ALL` / `LC_MESSAGES` / `LANG`.
  La locale messaggi `C` o `POSIX` forza l'inglese, come nell'ambiente POSIX.
- Numeri: `LC_ALL`, poi `LC_NUMERIC`, poi `LANG`.
- Date: `LC_ALL`, poi `LC_TIME`, poi `LANG`. Il calendario resta gregoriano,
  coerente con il calcolo date e i selettori Avalonia; ordine, mesi e formato
  della data seguono la regione.
- Accettati identificatori POSIX con `_`, suffisso `.UTF-8` e modificatore `@latin`.
- Per diagnosi, `--locale=fr-FR` imposta temporaneamente tutte e tre le categorie.

`Locale` inizializza le culture .NET prima di creare controlli e motore. La API
C++ riceve un dizionario UTF-8 con errori e simboli, separatore decimale,
separatore delle migliaia e raggruppamento (compreso quello indiano).
I calcoli della calcolatrice non vengono convertiti in `double`: resta la
precisione del motore originale anche nel copia/incolla, nelle memorie e nella
cronologia. I convertitori continuano a usare i fattori `double` originali del port.

La clipboard esporta il risultato senza raggruppamento, mantenendo il
separatore decimale locale. L'incolla convalida l'intera stringa prima di
modificare il calcolo: numeri raggruppati, notazione esponenziale, cifre Unicode
e basi del programmatore. I separatori devono seguire la regione attiva:
`1.234` significa 1234 in italiano e 1,234 in inglese. Il tastierino visualizza
il separatore locale; la tastiera fisica accetta punto, virgola e `٫` come
comando di separatore decimale.

Per arabo, persiano ed ebraico la shell segue la direzione RTL. Numeri,
espressioni e tastierini conservano l'ordine LTR; gli errori usano la direzione
della lingua. Selawik e CalcMDL2 restano i font Uno; i glifi non presenti usano
il fallback dei font installati nel sistema.

## Rigenerazione e verifica

```sh
python3 scripts/import-localizations.py ../Uno.Calculator
dotnet run --project tests/Calculator.Tests -c Release
```

L'importazione è necessaria solo quando si aggiornano i sorgenti delle
traduzioni. Build e runtime non dipendono dal checkout Uno.

La suite verifica la copertura di tutte le unità nelle 65 risorse (con fallback),
la selezione POSIX e regionale, la normalizzazione Unicode, precisione a 32
cifre, errori, memoria e cronologia in sette lingue; controlla la UI mobile,
valute e date in italiano, francese, tedesco, arabo e giapponese. Prove visive
e risultati sul dispositivo sono in `device-testing.md`.
