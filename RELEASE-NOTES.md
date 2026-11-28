# SmartSort release notes

## 2.0.0

- Build the CLI and tests with .NET 10 LTS. The file sorting rules and command-line options are unchanged.
- Provide self-contained ZIP (Windows x64) and TAR.GZ (Linux x64) downloads, SHA-256 checksums and a short demo using synthetic files.
- The Windows launcher is unsigned and may be blocked by local policy. The Linux archive was verified on Ubuntu 24.04 x64.

## 1.0.1

- A disallowed extension is reported as a per-file failure, so the batch can continue.
- Cancellation at the end of directory enumeration is detected before the completion message.
- Regression tests cover both cases and check that a rejected file stays in place in dry-run and move mode.

## 1.0.0

- First public source release of the C#/.NET 8 CLI. JSON rules match filenames, extensions and embedded PDF text.
- PdfPig replaced iText for text extraction. Scans without a text layer require separate OCR.
- Dry-run is the default. File moves use numbered destinations to avoid overwriting existing files.
- Windows and Linux CI run locked restore, build and tests.
