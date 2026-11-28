# SmartSort

[Deutsch](README.md)

SmartSort is a C# CLI that sorts local files into folders using JSON rules. Rules can match a file's extension, name or embedded PDF text. It began as part of my IHK project.

Dry-run is enabled by default. It logs the planned destinations and leaves the source files in place. When moves are enabled, a collision gets a numbered filename so the existing file stays in place.

## Download v2.0.0

[Windows x64 ZIP](https://github.com/rogchris/smartsort-cli/releases/download/v2.0.0/SmartSort-2.0.0-win-x64.zip) · [Linux x64 TAR.GZ](https://github.com/rogchris/smartsort-cli/releases/download/v2.0.0/SmartSort-2.0.0-linux-x64.tar.gz) · [SHA-256](https://github.com/rogchris/smartsort-cli/releases/download/v2.0.0/SHA256SUMS.txt)

The archives bundle the .NET 10 runtime and synthetic sample files. Extract the archive for your platform and run SmartSort from the extracted folder. The Windows launcher is unsigned. Windows may warn or block it depending on local policy. The Linux archive was tested on Ubuntu 24.04 x64. [Three-minute demo (German)](docs/demo.md)

## Try it with sample files

This source tree requires the .NET 10 SDK. The published v1.0.1 release still targets .NET 8, which Microsoft stopped supporting on 10 November 2026. [Release v1.0.1](https://github.com/rogchris/smartsort-cli/releases/tag/v1.0.1) · [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) · [Project roadmap](docs/roadmap.md)

From the repository root, restore, build and run the tests.

```sh
dotnet restore SmartSorter.sln --locked-mode
dotnet build SmartSorter.sln -c Release --no-restore --warnaserror
dotnet test SmartSorter.sln -c Release --no-build --no-restore
```

1. Create an `Inbox` folder and copy the [sample invoice](examples/rechnung-demo.pdf) and [sample note](examples/notiz-demo.txt) into it.
2. Run `dotnet run --project SmartSorter/SmartSort.csproj -c Release --no-build` and review the planned destinations.
3. To move the sample files, repeat the command with `-- --DryRun false`. The invoice goes to `Sorted/Rechnungen` and the note goes to `Sorted/_Uncategorized`.

Paths are relative to the working directory. [Paths and limits](SmartSorter/appsettings.json) · [Example rules](SmartSorter/App/Configuration/rules.json)

## Tests and operating limits

The CI workflow is configured for Windows and Ubuntu with locked restore, a Release build and tests for rule matching, PDF extraction, file moves and failure cases. [CI for the published v1.0.1](https://github.com/rogchris/smartsort-cli/actions/runs/46134080043)

Use folders and documents you trust. Scans without an embedded text layer require separate OCR. Logs can contain filenames and paths. [Architecture](docs/architecture.md) · [Security](SECURITY.md)

## License

The project code is MIT-licensed. PdfPig is a separate dependency under Apache-2.0. If you distribute binaries, include the relevant dependency licenses and notices. [MIT license](LICENSE.txt) · [Third-party notices](THIRD-PARTY-NOTICES.md) · [Release notes](RELEASE-NOTES.md)
