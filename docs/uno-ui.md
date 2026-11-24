# Corrispondenza con Uno.Calculator mobile

Riferimento: checkout Uno `11e981cdc6dcf448ecff8e87c382abd0559f3dce`.
La 0.1.2 sostituisce la prima UI del port, che aveva menu a tendina, pannelli
aggiuntivi, colori scuri e tasti arrotondati. È rimosso il campo «Inserisci numero».

Il riferimento per Ubuntu Touch è il layout compatto/mobile degli XAML, non la
finestra Windows con titolo, acrylic e cronologia sempre affiancata.

| XAML Uno | Equivalente Avalonia |
| --- | --- |
| `Views/MainPage.xaml` | `Views/MainWindow.axaml`: header 40 DIP, titolo con offset 52, menu laterale 256 DIP, margine superiore 3 DIP |
| `Styles.xaml` | `Views/UnoStyles.axaml`: tasti senza raggi, padding zero, numeri semibold, operatori leggeri, 1 DIP di margine per tasto |
| `Views/Calculator.xaml` | `Views/CalculatorView.cs`: righe standard `20*:72*:0:32*:308*`, scientifica `20*:72*:32*:32*:276*`, programmatore `20*:72*:96*:32*:268*` |
| `Styles.xaml / CalculationResultStyleM` | Risultato destro, gutter 12 DIP, Viewbox alto 75 DIP e allineato in alto; nessun editor supplementare |
| `Views/CalculatorStandardOperators.xaml`, `NumberPad.xaml` | Tastiera 6×4: `% √ x² 1/x`, `CE C backspace ÷`, tre righe numeriche, `± 0 . =` |
| `Views/CalculatorScientificOperators.xaml`, `CalculatorScientificAngleButtons.xaml` | Tastiera mobile 7×5, seconda funzione, DEG/RAD/GRAD, HYP e F-E; niente expander aggiuntivi |
| `Views/CalculatorProgrammer*` | Tastiera 6×6, quattro righe HEX/DEC/OCT/BIN, word size ciclica, editor bit con gruppi di quattro e indici |
| `Views/Calculator.xaml` history/memory flyouts | Icona cronologia e M⌄; pannello sovrapposto su mobile, dock solo con almeno 560×500 DIP |
| `Views/UnitConverter.xaml` | `Views/ConverterViews.cs`: valori e unità in due righe, tastierino 3 colonne con gutter 0,25*, proporzioni `56*:32*:56*:32*:Auto:272*` |
| `Views/DateCalculator.xaml` | Selettore fra differenza e aggiunta/sottrazione; un solo modulo alla volta |

Le icone matematiche e di navigazione provengono dal `CalcMDL2.ttf` originale.
La variante mobile usa un fondo pieno invece del backdrop acrylic di Windows:
`#E6E6E6` in chiaro e `#1F1F1F` in scuro. I tasti mantengono superfici al 40%
per gli operatori e all’80% per i numeri, bianche in chiaro e nere in scuro.
Dalla 0.1.5 la palette segue automaticamente il tema del dispositivo; testi,
icone, stati disabilitati e pannelli usano risorse dinamiche. Le dimensioni sono logiche e vengono scalate da Lomiri tramite il bridge.

Dalla 0.1.3 le etichette e gli errori seguono la lingua della sessione, riutilizzando
le risorse Uno. Il motore usa i separatori regionali, inclusi memoria e cronologia;
le tastiere numeriche conservano l’ordine Uno anche nelle lingue da destra a sinistra. Selawik è incluso per avere la stessa tipografia
su desktop Linux e UT; non è il font nativo di Android o iOS. I popup dei selettori
e delle date restano controlli Avalonia. Non si dichiara un'identità pixel per pixel
fra sistemi operativi diversi. La modalità valute BCE e il tasso manuale sono
estensioni del port, con il layout dei convertitori originali.

## Verifica

La suite headless controlla posizione dei tasti, assenza dell'editor rimosso,
menu e pannello cronologia mobile, dock largo, seconda funzione/HYP, basi e bit,
tastierino dei convertitori, oltre alle regressioni del motore. Le catture reali
delle viste e le prove del telefono sono registrate in `docs/device-testing.md`.
