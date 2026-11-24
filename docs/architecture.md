# Architektur und technische Entscheidungen

**Stand:** SmartSort als lokale .NET-8-Konsolenanwendung (Batch-Verarbeitung).

## Komponenten

| Bereich | Aufgabe |
|---|---|
| `SmartSorter/Program.cs` | Zusammensetzen der Komponenten, Konfiguration, Abbruchbehandlung und Exit-Codes |
| `App/Configuration` | Settings und Regeln; frühe Prüfung fehlerhafter Einstellungen |
| `App/Processing` | Verarbeitung eines Dokuments, DryRun und Zusammenfassung des Batch-Laufs |
| `Core` | Klassifizierung und Auswahl einer Zielkategorie ohne physische Dateioperationen |
| `Core/Documents` | Dokumentinformationen und verzögertes Öffnen des Datenstroms |
| `Core/Extraction` | Schnittstelle und Kombination von Text-Extraktoren |
| `Infrastructure` | Lokaler Dateisystemzugriff, Pfadsicherheit, Konfigurationsladung und PDF-Adapter |
| `Logging` | Serilog-Konfiguration |
| `SmartSorter.Tests` | Regeln, Konfiguration, Extraktion sowie Fehler- und Integrationstests |

## Verarbeitung

1. Konfiguration und Regeldatei einlesen und validieren.
2. Eingangsordner rekursiv scannen und Dateigrößen/-typen gemäß Konfiguration filtern.
3. Datei in ein `DocumentInput` mit verzögertem Stream-Zugriff überführen.
4. Die `RuleEngine` prüft zuerst günstige Kriterien (Dateiendung, Dateiname); PDF-Text wird nur bei Bedarf gelesen.
5. `FileClassifier` bestimmt die Kategorie einschließlich der Standardkategorien.
6. `DocumentProcessor` validiert Zielpfade und simuliert die Aktion oder führt sie aus.
7. `ProcessingResult` und die Batch-Zusammenfassung machen Erfolg und Fehler explizit.

## Designentscheidungen

- **Modularer Monolith statt Microservices:** Für ein lokales Kommandozeilenprogramm übersichtlicher und einfacher testbar.
- **Interfaces dort, wo sie helfen:** `IDocumentTextExtractor` kapselt PDF-Verarbeitung; `System.IO.Abstractions` ermöglicht simulierte Dateisysteme in Tests.
- **Deterministische Regeln:** Priorität zuerst, bei Gleichstand feste Sortierung nach Regelname.
- **DryRun als Standard:** Aktionen vor einem echten Verschieben nachvollziehen; standardmäßig keine Schreiboperationen auf Zieldateien.
- **Konflikte ohne Überschreiben:** Bei gleichem Dateinamen wird ein nummerierter Zielname gesucht; konkurrierende Schreibzugriffe werden berücksichtigt.
- **Schmale Ergebnismodelle:** Klassifizierung, extrahierter Inhalt und tatsächliche Dateioperation werden getrennt.
- **Betriebslogs statt Inhaltsablage:** Textinhalte von PDF-Dokumenten werden nicht in die strukturierten Logs übernommen; Dateinamen/Pfade können dennoch sensible Informationen enthalten.

## Grenzen

Die Anwendung überwacht Ordner nicht dauerhaft. Es existieren keine Datenbank, Web-API, Weboberfläche oder OCR. Der PDF-Parser ist keine Sandbox für nicht vertrauenswürdige Eingabedateien. Prüfungen auf Symlinks/Pfade eliminieren keine Race Conditions mit konkurrierenden Dateisystemänderungen. Der Betrieb sollte deshalb auf vertrauenswürdigen Ordnern erfolgen.

Siehe [SECURITY.md](../SECURITY.md) für die ausführlichen Einschränkungen.

## Testen

```sh
dotnet restore SmartSorter.sln --locked-mode
dotnet build SmartSorter.sln -c Release --no-restore --warnaserror
dotnet test SmartSorter.sln -c Release --no-build --no-restore
```

Die GitHub-Actions-Konfiguration führt diese Schritte auf Windows und Linux aus. Ein erfolgreiches CI-Ergebnis ist ein technischer Nachweis für den geprüften Stand, nicht für Fehlerfreiheit.

## Datenfluss

```mermaid
flowchart TD
    In["Lokaler Eingangsordner"] --> Scan["Dateien filtern und einlesen"]
    Rules["JSON-Regeln und Prioritäten"] --> Classify["Datei klassifizieren"]
    Scan --> Classify
    Classify --> Pdf["PDF-Text bei Bedarf auswerten"]
    Pdf --> Target["Ziel und Konflikte prüfen"]
    Classify --> Target
    Target --> Dry{"DryRun?"}
    Dry -->|Ja| Log["Entscheidung protokollieren"]
    Dry -->|Nein| Move["Unter freiem Zielnamen verschieben"]
    Move --> Log
```
