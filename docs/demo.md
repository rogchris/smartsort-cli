# SmartSort in unter drei Minuten

Nutze ein entpacktes Release-Archiv für Windows x64 oder Linux x64. Es enthält das Programm, die .NET-Laufzeit, die Standardregeln und synthetische Beispieldateien. Arbeite im entpackten Ordner `SmartSort` und verwende Exemplare der Dateien aus `examples/`.

## 0:00–0:30 · Ausgangslage

Lege `Inbox` an und kopiere `examples/rechnung-demo.pdf` und `examples/notiz-demo.txt` hinein. Lege außerdem `Sorted/Rechnungen` an und kopiere die Beispielrechnung als vorhandene Datei in diesen Ordner. So wird der Namenskonflikt sichtbar.

## 0:30–1:15 · Vorschau

Starte unter Windows `.\SmartSort.exe`, unter Linux `./SmartSort`. Zeige die beiden geplanten Ziele in der Ausgabe. Beide Dateien liegen weiterhin in `Inbox`.

## 1:15–2:00 · Dateien verschieben

Starte unter Windows `.\SmartSort.exe --DryRun false`, unter Linux `./SmartSort --DryRun false`. Zeige die drei Dateien im Ergebnis.

- `Sorted/Rechnungen/rechnung-demo.pdf` war bereits vorhanden.
- `Sorted/Rechnungen/rechnung-demo (1).pdf` ist die neu verschobene Rechnung.
- `Sorted/_Uncategorized/notiz-demo.txt` ist die verschobene Notiz.

## 2:00–2:40 · Technische Entscheidung erklären

Zeige die Dateinamenregel für die Rechnung in `App/Configuration/rules.json`. Erkläre kurz, warum SmartSort vor dem Verschieben eine Vorschau anbietet und vorhandene Ziele bei einem Konflikt erhält. Die Demo-Rechnung wird über ihren Dateinamen erkannt. Die Verarbeitung von eingebettetem PDF-Text hat eigene Tests.
