# Provenienza

## Uno / Microsoft Calculator

- Repository: https://github.com/unoplatform/calculator
- Checkout di origine: `/home/mi/RiderProjects/Uno.Calculator`
- Commit: `11e981cdc6dcf448ecff8e87c382abd0559f3dce`
- Licenza MIT in `Uno.Calculator.LICENSE`; notice originale in `Uno.Calculator.NOTICE.txt`.

Riutilizzati `src/CalcManager` (CalcEngine, RatPack, manager, cronologia e comandi), `Calculator.Shared/CalcManager/Command.cs`, risorse `Resources.resw` e `CEngineStrings.resw` delle 65 lingue, e fattori di `DataLoaders/UnitConverterDataLoader.cs`. `native/EngineResources.h` e `Engine/UnitCatalog.cs` contengono i dati estratti, senza dipendenze da UWP.

Adattamenti nativi: riconoscimento `__linux__`, architettura `__aarch64__`, include standard espliciti nel PCH. La nuova API UTF-8 `CalculatorBridge.cpp` sostituisce `CCalcManager`, che non è incluso. Dalla 0.1.3 la API riceve le risorse tradotte e i separatori regionali per istanza; l'esportazione omette i separatori delle migliaia mantenendo il separatore decimale locale. `CalcEngine.h` inizializza il separatore decimale prima di `SettingsChanged` e offre il ripristino delle stringhe statiche fra istanze localizzate. Le risorse JSON incorporate sono riproducibili con `scripts/import-localizations.py`. `ExtraStrings.cs` aggiunge le due etichette del port assenti in Uno (Taglia e Tasso manuale).

## UTDotNet-Bridge

- Repository: https://github.com/MiLattanzio/UTDotNet-Bridge
- NuGet: `UTDotNet.Avalonia` 0.1.6-preview.1 e relative dipendenze transitive, scaricati da nuget.org.
- `PhoneTextMenu.cs` adattato dal campione `samples/UTDotNet.InputDemo/PhoneTextMenu.cs`; licenza MIT in `UTDotNet-Bridge.LICENSE`.
- Packaging e integrazione seguono la consegna applicativa e il README del tag `nuget-v0.1.6-preview.1`.

Nessun `ProjectReference` verso il repository del bridge e nessuna modifica ai sorgenti del framework Avalonia.

## Interfaccia Uno mobile e font (0.1.2)

Le griglie e gli stati compatti derivano dagli XAML dello stesso commit Uno:
`MainPage.xaml`, `Calculator.xaml`, `CalculatorStandardOperators.xaml`,
`CalculatorScientificOperators.xaml`, `CalculatorScientificAngleButtons.xaml`,
`CalculatorProgrammerOperators.xaml`, `CalculatorProgrammerRadixOperators.xaml`,
`CalculatorProgrammerDisplayPanel.xaml`, `CalculatorProgrammerBitFlipPanel.xaml`,
`NumberPad.xaml`, `UnitConverter.xaml`, `DateCalculator.xaml` e `Styles.xaml`.
`Assets/CalcMDL2.ttf` è copiato senza modifiche da `Calculator.Shared/Assets`.
Si applicano licenza e notice Uno/Microsoft sopra riportati.

Per il testo su Linux è incluso Selawik (regular, semibold e light), il sostituto
aperto Microsoft di Segoe UI: https://github.com/microsoft/Selawik.
Binari non modificati da `winjs/winstrap`, commit
`342cf99031344f48917e14a5c3a728ec5ede8d8f`, directory `src/fonts/`:
`selawk.ttf`, `selawksb.ttf`, `selawkl.ttf`. Licenza SIL OFL 1.1 in
`Selawik.LICENSE.txt`. I font testuali nativi Android/iOS non sono redistribuiti.
