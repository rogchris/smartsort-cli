# SmartSort

SmartSort ordnet lokale Dateien anhand von JSON-Regeln Zielordnern zu. Das C#/.NET-8-Projekt entstand aus einer Aufgabe meiner Umschulung und wurde um Eingabevalidierung, Kollisionsbehandlung und Tests erweitert.

## Ablauf

Eine lokale Eingabemappe wird rekursiv gelesen. Die Regeln prüfen Dateiendung, Dateiname und bei Bedarf eingebetteten PDF-Text. PdfPig liest den vorhandenen Text. Scans ohne Textebene benötigen OCR. Gewinnt eine Regel, ergibt sich daraus der Zielunterordner. Der DryRun protokolliert die Zuordnung, ohne Dateien zu verschieben. Im echten Lauf erhält eine Datei bei einer Namenskollision einen nummerierten Zielnamen.

## Mit Beispieldateien ausprobieren

Voraussetzung ist das .NET 8 SDK. Vom Repository-Stamm aus:

```sh
dotnet restore SmartSorter.sln --locked-mode
dotnet build SmartSorter.sln -c Release --no-restore
dotnet test SmartSorter.sln -c Release --no-build --no-restore
```

Im aktuellen Arbeitsverzeichnis einen Ordner `Inbox` mit selbst erzeugten Testdateien anlegen, beispielsweise Kopien der beiden synthetischen Dateien aus `examples/`. Ein Beispiel für eine JSON-Regel steht in den vorhandenen Konfigurationsdateien. Für die erste Ausführung bleibt DryRun aktiv.

```sh
dotnet run --project SmartSorter/SmartSort.csproj -c Release --no-build
```

`SmartSorter/appsettings.json` enthält Pfade und Grenzwerte. Die Regeln liegen unter `SmartSorter/App/Configuration/rules.json`. Pfade werden relativ zum Arbeitsverzeichnis ausgewertet. Im Demo-Lauf bleibt `Inbox` unverändert. Für den tatsächlichen Lauf `-- --DryRun false` ergänzen. Danach liegen die Beispielrechnung in `Sorted/Rechnungen` und die Notiz in `Sorted/_Uncategorized`.

## Tests

Die Testprojekte decken Regeln, Fehlerfälle und Dateioperationen ab. Die CI ist für Windows und Linux eingerichtet. Der öffentliche Snapshot erhält einen neu ausgeführten Prüflauf.

## Architektur und Lizenz

[Architektur und Datenfluss](docs/architecture.md) · [MIT-Lizenz für den Projektcode](LICENSE.txt) · [Sicherheit](SECURITY.md)

PdfPig ist eine separate Abhängigkeit unter Apache-2.0. Diese Fassung trägt die Projektversion 1.0.0. Für Binärdistributionen gelten zusätzlich die Lizenzen und Hinweise der eingebundenen Pakete.

[Release Notes](RELEASE-NOTES.md) · [Drittanbieterhinweise](THIRD-PARTY-NOTICES.md)
