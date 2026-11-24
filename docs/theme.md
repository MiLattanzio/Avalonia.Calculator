# Tema di sistema (0.1.5)

L'app segue automaticamente il tema del dispositivo, senza un selettore
indipendente. Sul desktop `ThemeVariant.Default` usa la preferenza di piattaforma
rilevata da Avalonia. Su Ubuntu Touch viene letta la preferenza del toolkit Lomiri,
che può differire da quella GNOME/GTK: sul Fairphone `SuruDark` era attivo mentre
`org.gnome.desktop.interface color-scheme` riportava `default`.

`UbuntuTouchTheme` legge `[General] theme=...` da
`$HOME/.config/lomiri-ui-toolkit/theme.ini`. Supporta anche i percorsi storici
`ubuntu-ui-toolkit/theme.ini` e `UITK/theme.ini`, in questo ordine. `SuruDark`
seleziona il tema scuro, `Ambiance` quello chiaro. Se la preferenza manca o il nome
non è riconosciuto, resta il tema di piattaforma. Un errore temporaneo di lettura
mantiene il tema precedente.

Il file viene letto prima di mostrare la finestra e ogni due secondi mentre
la finestra è visibile; quando viene mostrata o riceve un evento di attivazione
viene controllato subito. Il controllo si ferma quando la finestra viene nascosta
o chiusa; la sospensione dell'app in background resta gestita da Lomiri.
Su XMir il focus può restare su `PointerRoot` anche durante l'uso touch:
gli eventi di attivazione non sono quindi un requisito per avviare il controllo.
Questo supporta anche la sostituzione atomica
del file senza richiedere l'accesso alla directory per un watcher. Il profilo
Click standard già consente la lettura dei tre file: nessun nuovo permesso e
nessuna scrittura delle preferenze da parte dell'app.

`Views/ThemeResources.axaml` raccoglie le palette chiara e scura. La griglia
mobile Uno, le dimensioni e i font restano uguali; cambiano sfondo, superfici,
testi, icone, accenti e stati disabilitati. Le viste costruite in C# usano classi
con risorse dinamiche, comprese espressioni, cronologia e bit del programmatore.
I selettori, i menu e i popup Fluent seguono la stessa variante. In scuro l'accento
azzurro mantiene leggibili link e bit attivi sul fondo scuro.

Il cambio di tema aggiorna i controlli esistenti: non ricrea il motore né i
campi e non cancella calcolo, memorie, cronologia o tasso manuale. Per catture
di verifica si può usare `--theme=light` oppure `--theme=dark`; questo override
vale solo per quel processo e non viene memorizzato.

Le prove automatiche verificano configurazione attuale e legacy, priorità dei
percorsi, aggiornamento a caldo anche senza attivazione XMir, finestra nascosta,
riattivazione, errori di lettura, disiscrizione
alla chiusura, tutte le 17 modalità, stati dei bit, colori e popup aperti.
Le prove sul Fairphone sono registrate in `docs/device-testing.md`.
