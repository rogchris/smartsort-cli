# SmartSort

[English](README.en.md)

SmartSort ist ein Kommandozeilenprogramm in C#, das lokale Dateien nach JSON-Regeln in Ordner sortiert. Regeln können Dateiendungen, Dateinamen und eingebetteten PDF-Text auswerten. Das Programm ist aus meinem IHK-Projekt entstanden.

Die Vorschau (Dry-Run) ist standardmäßig aktiv. Sie protokolliert die geplanten Ziele und lässt die Quelldateien an Ort und Stelle. Beim Verschieben erhält eine Datei bei einem Namenskonflikt einen nummerierten Namen. Die vorhandene Datei bleibt erhalten.

## Download v2.0.0

[Windows x64 ZIP](https://github.com/rogchris/smartsort-cli/releases/download/v2.0.0/SmartSort-2.0.0-win-x64.zip) · [Linux x64 TAR.GZ](https://github.com/rogchris/smartsort-cli/releases/download/v2.0.0/SmartSort-2.0.0-linux-x64.tar.gz) · [SHA-256](https://github.com/rogchris/smartsort-cli/releases/download/v2.0.0/SHA256SUMS.txt)

Die Archive enthalten die .NET-10-Laufzeit und synthetische Beispieldateien. Entpacke das passende Archiv und starte SmartSort im entpackten Ordner. Der Windows-Launcher ist unsigniert. Windows kann je nach Sicherheitsrichtlinie eine Warnung anzeigen oder den Start unterbinden. Das Linux-Archiv wurde unter Ubuntu 24.04 x64 geprüft. [Demo in unter drei Minuten](docs/demo.md)

## Mit Beispieldateien ausprobieren

Dieser Quellstand benötigt das .NET 10 SDK. Das veröffentlichte v1.0.1 verwendet noch .NET 8. Der Microsoft-Support dafür endete am 10. November 2026. [Release v1.0.1](https://github.com/rogchris/smartsort-cli/releases/tag/v1.0.1) · [Microsoft-Supportkalender](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) · [Roadmap](docs/roadmap.md)

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

Der CI-Workflow ist für Windows und Ubuntu eingerichtet. Er führt den Restore mit Lockfiles, den Release-Build und Tests zu Regeln, PDF-Extraktion, Dateioperationen und Fehlerfällen aus. [CI des veröffentlichten v1.0.1](https://github.com/rogchris/smartsort-cli/actions/runs/46134080043)

Verwende vertrauenswürdige Ordner und PDFs. Scans ohne eingebettete Textebene benötigen eine separate OCR-Verarbeitung. Protokolle können Dateinamen und Pfade enthalten. [Architektur](docs/architecture.md) · [Sicherheit](SECURITY.md)

## Lizenz

Der Projektcode steht unter MIT. PdfPig ist eine Abhängigkeit unter Apache-2.0. Bei der Weitergabe von Binärdateien gelten auch die Lizenztexte und Hinweise der eingebundenen Pakete. [MIT-Lizenz](LICENSE.txt) · [Drittanbieterhinweise](THIRD-PARTY-NOTICES.md) · [Release Notes](RELEASE-NOTES.md)
