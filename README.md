# SmartSort

[English](README.en.md)

SmartSort ist ein Kommandozeilenprogramm in C#, das lokale Dateien nach JSON-Regeln in Ordner sortiert. Regeln können Dateiendungen, Dateinamen und eingebetteten PDF-Text auswerten. Das Programm ist aus meinem IHK-Projekt entstanden.

Die Vorschau (Dry-Run) ist standardmäßig aktiv. Sie protokolliert die geplanten Ziele und lässt die Quelldateien an Ort und Stelle. Beim Verschieben erhält eine Datei bei einem Namenskonflikt einen nummerierten Namen. Die vorhandene Datei bleibt erhalten.

## Mit Beispieldateien ausprobieren

Der Quellcode von v1.0.1 zielt auf .NET 8 und benötigt zum Build das .NET 8 SDK. Der Microsoft-Support für .NET 8 endete am 10. November 2026. Die Umstellung auf .NET 10 ist separat geplant. [Release v1.0.1](https://github.com/rogchris/smartsort-cli/releases/tag/v1.0.1) · [Microsoft-Supportkalender](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) · [Roadmap](docs/roadmap.md)

Pakete wiederherstellen, Build ausführen und Tests starten. Alle Befehle laufen vom Repository-Stamm aus.

```sh
dotnet restore SmartSorter.sln --locked-mode
dotnet build SmartSorter.sln -c Release --no-restore --warnaserror
dotnet test SmartSorter.sln -c Release --no-build --no-restore
```

1. Einen Ordner `Inbox` anlegen und die synthetische [Beispielrechnung](examples/rechnung-demo.pdf) und [Beispielnotiz](examples/notiz-demo.txt) hineinkopieren.
2. `dotnet run --project SmartSorter/SmartSort.csproj -c Release --no-build` ausführen und die geplanten Ziele prüfen.
3. Zum Verschieben den Befehl um `-- --DryRun false` ergänzen. Die Rechnung landet in `Sorted/Rechnungen`, die Notiz in `Sorted/_Uncategorized`.

Relative Pfade beziehen sich auf das Arbeitsverzeichnis. [Pfade und Grenzwerte](SmartSorter/appsettings.json) · [Beispielregeln](SmartSorter/App/Configuration/rules.json)

## Tests und Grenzen

Die CI auf Windows und Ubuntu führt den Restore mit Lockfiles, den Release-Build und Tests zu Regeln, PDF-Extraktion, Dateioperationen und Fehlerfällen aus. [CI-Lauf für v1.0.1](https://github.com/rogchris/smartsort-cli/actions/runs/46134080043)

Verwende vertrauenswürdige Ordner und PDFs. Scans ohne eingebettete Textebene benötigen eine separate OCR-Verarbeitung. Protokolle können Dateinamen und Pfade enthalten. [Architektur](docs/architecture.md) · [Sicherheit](SECURITY.md)

## Lizenz

Der Projektcode steht unter MIT. PdfPig ist eine Abhängigkeit unter Apache-2.0. Bei der Weitergabe von Binärdateien gelten auch die Lizenztexte und Hinweise der eingebundenen Pakete. [MIT-Lizenz](LICENSE.txt) · [Drittanbieterhinweise](THIRD-PARTY-NOTICES.md) · [Release Notes](RELEASE-NOTES.md)
