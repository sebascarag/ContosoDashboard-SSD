# Phase 0 Research: Document Management

## Decision: Use EF Core models with integer primary keys and text categories

**Decision**: Add `Document`, `DocumentShare`, `DocumentActivityLog`, and task/document relationships using integer keys consistent with existing entities. Store category values as text.

**Rationale**: The stakeholder requirements and existing data model require integer keys and text categories. EF Core can enforce uniqueness and relationships while preserving the current architecture.

**Alternatives considered**:
- GUID primary keys: rejected because the project standardizes on integer keys.
- Integer category enum: rejected because categories must remain text values.

## Decision: Use local filesystem storage outside web-root content

**Decision**: Store files under `AppData/uploads/{userId}/{projectId or personal}/{guid}.{extension}` as relative file paths.

**Rationale**: This isolates files from static web serving and prevents user filenames from influencing storage paths.

**Alternatives considered**:
- Store files under `wwwroot`: rejected because direct static access bypasses authorization.
- Store file bytes in the database: rejected because it burdens the database and complicates migration.

## Decision: Abstract file storage and scanning behind interfaces

**Decision**: Implement `IFileStorageService` for upload, delete, download, and URL generation. Implement `IFileScanService` for a malware scan result.

**Rationale**: The storage boundary enables a future Azure Blob implementation without changing document workflows. A scan boundary allows an offline training implementation and a replaceable production-capable implementation.

**Alternatives considered**:
- Direct `System.IO` calls throughout: rejected because it couples business logic to one provider.
- Mandatory external scanner: rejected because core training workflows must remain offline.

**Training scanner constraint**: The training implementation may provide a local, bounded scanner that validates file signatures and rejects suspicious content, but it must not be represented as a production-grade anti-malware solution. A production deployment must supply a configured scanner.

## Decision: Generate and save storage paths before metadata persistence

**Decision**: Generate a GUID-based relative path and save the file before inserting document metadata. If metadata persistence fails, delete the newly stored file.

**Rationale**: This prevents non-unique empty paths and avoids orphaned database records or stored files.

**Alternatives considered**:
- Insert metadata first: rejected because a storage failure could leave a metadata-only record.
- Use uploader-provided filenames: rejected because of traversal and collision risk.

## Decision: Enforce authorization in both service methods and file endpoints

**Decision**: Apply ownership, role, project membership, team membership, and explicit share checks in `DocumentService`. Repeat checks in preview/download endpoints.

**Rationale**: Blazor route attributes and UI visibility do not protect file bytes or direct requests. Defense in depth is required.

**Alternatives considered**:
- UI-only authorization: rejected as insecure and incompatible with direct-link protection.
- Endpoint-only authorization: rejected because document search, browse, and mutations must also filter or deny unauthorized access.

## Decision: Use existing notification records for document notifications

**Decision**: Create `Notification` records for share and project-upload events using the existing notification service and in-app notification page.

**Rationale**: This reuses the application's established notification mechanism and avoids introducing an external messaging dependency.

**Alternatives considered**:
- Separate document notification subsystem: rejected as unnecessary architecture.

## Decision: Project deletion and inactivity follow clarified lifecycle rules

**Decision**: Permanently delete associated document files and metadata when a project is deleted. Preserve documents and authorization when a project becomes inactive, but permit only view and download until reactivated.

**Rationale**: These rules match the accepted clarifications and prevent accidental storage loss while blocking mutations in inactive projects.

**Alternatives considered**:
- Preserve deleted-project documents: rejected because project deletion is explicitly permanent.
- Allow mutations for inactive projects: rejected because inactive projects must become read-only.

## Decision: Retain document activity records for 180 days

**Decision**: Store activity records and purge records older than 180 days.

**Rationale**: The clarified retention period provides a bounded audit window and limits local training storage growth.

**Alternatives considered**:
- Indefinite retention: rejected because it is unbounded.
