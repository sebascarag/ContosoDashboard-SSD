# Implementation Plan: Document Management

**Branch**: `001-document-management` | **Date**: 2026-11-20 | **Spec**: [spec.md](./spec.md)

## Summary

Implement centralized document upload, browsing, preview, download, metadata maintenance, sharing, project/task integration, notifications, audit reporting, and document counts for ContosoDashboard. Preserve the existing Blazor Server, EF Core, cookie authentication, and service-layer architecture. Store files locally outside web-accessible content behind a replaceable storage abstraction, enforce authorization in services and download endpoints, and keep all core workflows offline.

## Technical Context

**Language/Version**: C# with .NET 10 / ASP.NET Core
**Primary Dependencies**: Blazor Server, Entity Framework Core 10, SQL Server LocalDB, Bootstrap 5.3
**Storage**: SQL Server LocalDB for metadata and audit records; local filesystem under `AppData/uploads` for files
**Testing**: Existing solution has no test project; use focused manual verification and add a test project only if repository tooling supports it
**Target Platform**: Desktop and tablet web browsers using Blazor Server
**Project Type**: Single server-hosted web application
**Performance Goals**: 25 MB upload within 30 seconds; up to 500 document list records within 2 seconds; search within 2 seconds; preview within 3 seconds
**Constraints**: Offline/local operation; no Azure SDK; integer document keys; text categories; 25 MB maximum file; authorization on every protected operation
**Scale/Scope**: Training-scale users and projects; approximately 500 documents per list target

## Constitution Check

| Gate | Status | Evidence |
|---|---|---|
| Offline-first training operation | Pass | Local database and local filesystem implementation; no mandatory cloud dependency |
| Layered architecture | Pass | Models, EF data context, services, Blazor pages, and storage abstraction remain separate |
| Server-side authorization | Pass | DocumentService performs ownership, role, project, team, and explicit-share checks; download and preview endpoints repeat authorization |
| Secure file handling | Pass | Extension and size validation; GUID filenames; storage outside web-accessible content; authorization before file access |
| Replaceable infrastructure | Pass | `IFileStorageService` supports local and future cloud implementations |
| Verifiable changes | Pass | Contract and quickstart validation cover upload, access, sharing, project lifecycle, and audit behavior |

### Document Activity Retention

Add a local, bounded retention process that keeps document activity for 180 days only:

- Store every retained activity with `OccurredAt` in UTC.
- Calculate the cutoff once per cleanup run as `UTC now minus 180 days`.
- Delete all document activity records with `OccurredAt < cutoff`.
- Run cleanup once when the application starts and then once every 24 hours while it remains running.
- Also execute cleanup before generating an administrator activity report, preventing stale records from being visible.
- Process deletions in bounded batches to avoid long database transactions.
- Log the cleanup run's cutoff and deleted-record count for operational verification.
- Do not delete document files or document metadata as part of this process.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── document-http-contract.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── DocumentActivityLog.cs
│   └── existing models
├── Services/
│   ├── DocumentService.cs
│   ├── IFileStorageService.cs
│   ├── LocalFileStorageService.cs
│   └── IFileScanService.cs
├── Pages/
│   ├── Documents.razor
│   ├── SharedDocuments.razor
│   ├── ProjectDetails.razor
│   └── Tasks.razor
├── Controllers/
│   └── DocumentsController.cs
└── Program.cs
```

**Structure Decision**: Extend the existing single-project web application. Add document models and EF mappings to the current data layer, add document and storage services, add Blazor views, and add authorized controller endpoints for browser preview and download.

## Complexity Tracking

No constitution violations require an exception. The storage abstraction and file-scanning abstraction are required boundaries for future migration and secure handling rather than unnecessary infrastructure.
