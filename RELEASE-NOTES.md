# SmartSort Release Notes

## 1.0.1

- Eine unzulässige Dateiendung wird als Fehler der einzelnen Datei gemeldet. Der Batch kann weiterlaufen.
- Ein Abbruch am Ende der Verzeichnissuche wird vor der Abschlussmeldung erkannt.
- Regressionstests prüfen beide Fehlerfälle, darunter die unveränderte Quelldatei im Dry-Run und im regulären Lauf.

## 1.0.0

- Lokale C#/.NET-8-Anwendung zur regelbasierten Zuordnung von Dateien.
- JSON-Regeln nach Dateiname, Erweiterung und eingebettetem PDF-Text. PdfPig ersetzt iText. Scans ohne Textebene bleiben ein Fall für OCR.
- DryRun als Standard, nummerierte Zielnamen bei Kollisionen und synthetische Beispieldateien.
- Reproduzierbarer NuGet-Restore und automatisierte Prüfung auf Windows und Linux.

Die erste öffentliche Version ist als Quellcode-Release vorbereitet.
