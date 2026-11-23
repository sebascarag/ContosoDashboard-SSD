# Tasks: Document Management

**Input**: Design documents from `specs/001-document-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: No automated test project exists and the specification does not request TDD. Use the independent manual acceptance checks below and the runnable scenarios in `quickstart.md`.

**Organization**: Tasks are grouped by the six prioritized user stories in `spec.md`.

## Phase 1: Setup

**Purpose**: Prepare local document storage configuration without adding external dependencies.

- [X] T001 [P] Exclude runtime document files and temporary upload data from version control in `ContosoDashboard/.gitignore`.
- [X] T002 [P] Configure the local file-storage root outside `wwwroot` in `ContosoDashboard/appsettings.json`.

## Phase 2: Foundational

**Purpose**: Enable authenticated file endpoints needed by preview and download.

- [X] T003 Enable controller registration and endpoint mapping alongside Blazor routes in `ContosoDashboard/Program.cs`.

**Checkpoint**: Setup and endpoint routing are ready; document-specific work can begin.

## Phase 3: User Story 1 - Upload and Organize a Document (Priority: P1) - MVP

**Goal**: Accept validated, malware-scanned files with required metadata and store them securely for the uploader.

**Independent Test**: Sign in as an employee, upload a valid PDF with a title and category, and verify that it appears in My Documents with the correct metadata. Verify clear rejection for an oversized file, unsupported type, or scanner/storage failure.

### Implementation

- [X] T004 [P] [US1] Create `Document` in `ContosoDashboard/Models/Document.cs` with fields defined in `data-model.md`: `Document.DocumentId`, `Document.Title`, `Document.Description`, `Document.Category`, `Document.Tags`, `Document.ProjectId`, `Document.UploadedByUserId`, `Document.UploadedAt`, `Document.FileName`, `Document.FilePath`, `Document.FileSizeBytes`, `Document.FileType`, `Document.CreatedAt`, and `Document.UpdatedAt`.
- [X] T005 [P] [US1] Create `DocumentActivityLog` in `ContosoDashboard/Models/DocumentActivityLog.cs` with fields defined in `data-model.md`: `DocumentActivityLog.DocumentActivityId`, `DocumentActivityLog.DocumentId`, `DocumentActivityLog.ActorUserId`, `DocumentActivityLog.Action`, `DocumentActivityLog.OccurredAt`, and `DocumentActivityLog.Success`.
- [X] T006 [US1] Add `Document` and `DocumentActivityLog` DbSets, foreign keys, delete behavior, and query indexes to `ContosoDashboard/Data/ApplicationDbContext.cs`, following the relationships in `data-model.md`; add an idempotent schema initializer so existing `EnsureCreated` databases receive the new tables without data loss.
- [X] T007 [P] [US1] Define the portable file operations contract for upload, delete, download, and URL generation in `ContosoDashboard/Services/IFileStorageService.cs`.
- [X] T008 [P] [US1] Define the fail-closed file scan result contract in `ContosoDashboard/Services/IFileScanService.cs`.
- [X] T009 [US1] Implement local storage in `ContosoDashboard/Services/LocalFileStorageService.cs` using the configured root, GUID-based relative paths, and no user-supplied path segments; implement the `IFileStorageService` contract from `ContosoDashboard/Services/IFileStorageService.cs`.
- [X] T010 [US1] Implement the offline training scanner in `ContosoDashboard/Services/LocalFileScanService.cs` per `research.md`: inspect file signatures, reject mismatches or suspicious results, fail closed on scan errors, and clearly identify that this is not production-grade antivirus.
- [X] T011 [US1] Implement upload orchestration in `ContosoDashboard/Services/DocumentService.cs`: validate allowed types and 25 MB maximum, authorize employees for personal or assigned-project uploads and project managers for their projects, scan before storage, save the file before database metadata, compensate for database failure by deleting the stored file, and record upload activity.
- [X] T012 [US1] Create the upload form and uploader's basic My Documents view in `ContosoDashboard/Pages/Documents.razor`, including required title/category, optional description/project/tags, multi-file selection, progress, success, and explicit validation or storage errors.
- [X] T013 [US1] Register `DocumentService`, `LocalFileStorageService`, and `LocalFileScanService` in `ContosoDashboard/Program.cs`.
- [X] T014 [US1] Add the Documents navigation entry in `ContosoDashboard/Shared/NavMenu.razor`.

**Checkpoint**: A user can upload an allowed file and see it in My Documents; validation, malware scan failure, and storage failure do not produce a usable orphaned document.

## Phase 4: Establish EF Core Migration Baseline

**Purpose**: Enable versioned schema changes after the MVP while preserving databases already created with `EnsureCreated()`.

### Implementation

- [ ] T045 Create an initial EF Core migration and model snapshot in `ContosoDashboard/Migrations/` for the complete current schema, including the existing dashboard entities and document MVP entities; verify the migration can create the schema in an empty database.
- [ ] T046 Replace `EnsureCreated()` and `DocumentSchemaInitializer` in `ContosoDashboard/Program.cs` with EF Core migration startup and remove the initializer. Define a safe adoption procedure for existing `EnsureCreated()` databases: verify their schema matches the baseline, abort adoption on mismatch, and record the baseline as applied without rerunning its table-creation operations. Verify that existing records remain intact and that an empty database is created successfully through migrations.

**Checkpoint**: New databases are created through EF Core migrations, and existing databases can adopt the baseline without data loss.

## Phase 5: User Story 2 - Find and Open Authorized Documents (Priority: P1)

**Goal**: Browse, sort, filter, search, preview, and download all documents the user is authorized to access.

**Independent Test**: With accessible documents across categories and projects, confirm the required sort/filter/search fields, load a PDF or image preview, download an authorized file, and verify that an unauthorized user cannot retrieve metadata or bytes using a direct URL.

### Implementation

- [ ] T015 [US2] Add authorized document list, sort, filter, and search operations to `ContosoDashboard/Services/DocumentService.cs` for My Documents and Project Documents; include Team Lead access to current team members' documents and apply authorization before returning any result.
- [ ] T016 [US2] Extend `ContosoDashboard/Pages/Documents.razor` with sorting by title/date/category/size, filtering by category/project/date range, search across title/description/tags/uploader/project, and empty/loading/error states.
- [ ] T017 [US2] Implement authorized inline PDF/image preview and attachment download routes in `ContosoDashboard/Controllers/DocumentsController.cs` using the document HTTP contract in `contracts/document-http-contract.md`.
- [ ] T018 [US2] Record authorized download activity and deny unauthorized or missing document requests in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Controllers/DocumentsController.cs`.

**Checkpoint**: Browse and search return only authorized documents; allowed files preview or download through protected endpoints.

## Phase 6: User Story 3 - Maintain Document Metadata and Files (Priority: P2)

**Goal**: Let document owners maintain metadata and replace files, and let owners or authorized project managers permanently delete documents.

**Independent Test**: Edit metadata and replace a document as its owner; confirm the new metadata and file are used. Delete a document as its owner and as an authorized project manager, then verify the file and record are unavailable. Verify unauthorized mutations are denied.

### Implementation

- [ ] T019 [US3] Add metadata update and validation operations to `ContosoDashboard/Services/DocumentService.cs` for owners and Team Leads managing current team members' documents, including project reassignment authorization.
- [ ] T020 [US3] Add file replacement to `ContosoDashboard/Services/DocumentService.cs` for owners and Team Leads managing current team members' documents; scan and save the replacement first, update the existing record, and delete the old file only after metadata persistence succeeds.
- [ ] T021 [US3] Add confirmed permanent deletion to `ContosoDashboard/Services/DocumentService.cs` for owners, Team Leads managing current team members' documents, and authorized project managers; record the deletion activity and remove the stored file without exposing it if cleanup fails.
- [ ] T022 [US3] Add edit, replace, and confirmation-gated delete controls with clear failure messages to `ContosoDashboard/Pages/Documents.razor`.

**Checkpoint**: Owners can maintain their documents, project managers can delete permitted project documents, and unauthorized users cannot mutate them.

## Phase 7: User Story 4 - Share Documents with Users and Teams (Priority: P2)

**Goal**: Grant and revoke explicit document access for selected users or teams and notify recipients.

**Independent Test**: Share a document with a user and a team, confirm access and in-app notification for recipients, verify it appears in Shared with Me, revoke access, and confirm direct access is immediately denied.

### Implementation

- [ ] T023 [P] [US4] Create `DocumentShare` in `ContosoDashboard/Models/DocumentShare.cs` with fields defined in `data-model.md`: `DocumentShare.DocumentShareId`, `DocumentShare.DocumentId`, `DocumentShare.UserId`, `DocumentShare.TeamId`, `DocumentShare.SharedByUserId`, and `DocumentShare.SharedAt`; enforce exactly one user or team recipient.
- [ ] T024 [US4] Add the `DocumentShare` DbSet, foreign keys, and uniqueness rules to `ContosoDashboard/Data/ApplicationDbContext.cs` using the relationships in `data-model.md`; add and apply an EF Core migration.
- [ ] T025 [US4] Implement owner-authorized share creation, revocation, and live recipient/team authorization checks in `ContosoDashboard/Services/DocumentService.cs`; use current department membership for team recipients because the application has no separate Team entity.
- [ ] T026 [US4] Create the Shared with Me view in `ContosoDashboard/Pages/SharedDocuments.razor` and add it to `ContosoDashboard/Shared/NavMenu.razor`.
- [ ] T027 [US4] Create recipient notifications for new shares through `ContosoDashboard/Services/NotificationService.cs`, extending `ContosoDashboard/Models/Notification.cs` only as needed to represent document-share notifications; add and apply an EF Core migration if the model changes.
- [ ] T028 [US4] Add share and revoke controls for document owners in `ContosoDashboard/Pages/Documents.razor`; show clear recipient and authorization errors.

**Checkpoint**: Only owners can share; recipients gain access and receive notice; revoked recipients lose access immediately.

## Phase 8: User Story 5 - Connect Documents to Projects and Tasks (Priority: P2)

**Goal**: Surface project documents and allow permitted task users to upload or attach documents with the correct project association.

**Independent Test**: Open a project and task, view project documents, upload and attach a document from task context, and confirm permitted project members can open it while unauthorized users cannot.

### Implementation

- [ ] T029 [P] [US5] Create `DocumentTask` in `ContosoDashboard/Models/DocumentTask.cs` with fields defined in `data-model.md`: `DocumentTask.DocumentTaskId`, `DocumentTask.DocumentId`, and `DocumentTask.TaskId`.
- [ ] T030 [US5] Add the `DocumentTask` DbSet, foreign keys, uniqueness constraint, and task/project association validation to `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`, following `data-model.md`; add and apply an EF Core migration.
- [ ] T031 [US5] Add project-scoped document listing and project-manager upload entry points to `ContosoDashboard/Pages/ProjectDetails.razor`, applying the inactive-project read-only rules in `spec.md`.
- [ ] T032 [US5] Add task document upload and existing-document attachment controls to `ContosoDashboard/Pages/Tasks.razor`; derive project association from the selected task and enforce both task and document access in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T033 [US5] Implement project deletion orchestration in `ContosoDashboard/Services/ProjectService.cs` and `ContosoDashboard/Services/DocumentService.cs` so associated document files and records are permanently removed; reject document mutations for inactive projects.

**Checkpoint**: Project and task views expose only permitted documents; task attachments inherit the task project's association; project deletion removes associated files and inactive projects are read-only.

## Phase 9: User Story 6 - Receive Document Notifications and Review Activity (Priority: P3)

**Goal**: Notify project members of document additions, show document dashboard summaries, and provide administrator activity reporting with 180-day retention.

**Independent Test**: Add a document to a project and confirm member notifications; verify the five most recent uploads and document count on the dashboard; as Administrator, review activity reports and confirm records older than 180 days are removed.

### Implementation

- [ ] T034 [US6] Create project-upload notifications for current project members through `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`.
- [ ] T035 [US6] Add the current user's five most recently uploaded documents and document count to the dashboard query in `ContosoDashboard/Services/DashboardService.cs`.
- [ ] T036 [US6] Display the Recent Documents widget and document count on `ContosoDashboard/Pages/Index.razor`.
- [ ] T037 [US6] Add administrator-only activity reporting for uploaded file types, active uploaders, and access patterns in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/Documents.razor`.
- [ ] T038 [US6] Implement bounded deletion of `DocumentActivityLog` records where `DocumentActivityLog.OccurredAt < UTC now minus 180 days` in `ContosoDashboard/Services/DocumentActivityRetentionService.cs`; log the cutoff and deleted count without deleting document metadata or files.
- [ ] T039 [US6] Register the retention worker in `ContosoDashboard/Program.cs` to clean at startup, every 24 hours, and immediately before administrator reports, as specified in `plan.md`.
- [ ] T040 [US6] Ensure uploads, downloads, edits, replacements, shares, revocations, and deletions record actor, UTC time, action, and outcome in `ContosoDashboard/Services/DocumentService.cs`.

**Checkpoint**: Notifications, dashboard summary, administrative reporting, and bounded 180-day activity retention are available offline.

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Validate the complete workflow and close security and documentation gaps.

- [ ] T041 [P] Update `README.md` with document-management training limitations, offline file location, supported file types, size cap, and local scanner caveat.
- [ ] T042 Verify document authorization, project membership changes, share revocation, inactive-project restrictions, and direct-link denial against `specs/001-document-management/quickstart.md`.
- [ ] T043 Verify upload, listing, search, preview, and report performance targets in `ContosoDashboard/` using up to 500 documents and record results in `specs/001-document-management/quickstart.md`.
- [ ] T044 Run the full `specs/001-document-management/quickstart.md` flow and resolve any implementation or documentation mismatches.

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; local configuration can be prepared immediately.
- **Foundational (Phase 2)**: Depends on Setup; enables authorized HTTP controller endpoints.
- **User Story 1 (Phase 3)**: Depends on Foundational; creates the document and activity core.
- **Migration Baseline (Phase 4)**: Depends on the completed MVP schema from User Story 1; must finish before subsequent EF model changes.
- **User Stories 2-5 (Phases 5-8)**: Depend on User Story 1 document persistence and authorization; schema changes depend on the migration baseline.
- **User Story 6 (Phase 9)**: Depends on User Story 1 activity records and User Stories 2-5 operations for complete reports and project notifications.
- **Polish (Phase 10)**: Depends on all selected stories and the migration baseline.

### User Story Dependencies

- **US1 (P1)**: No story dependencies after setup and foundational routing.
- **Migration Baseline**: Depends on the completed US1 MVP schema; T046 depends on T045.
- **US2 (P1)**: Depends on US1 for persisted documents and access checks.
- **US3 (P2)**: Depends on US1 for persisted documents and storage operations.
- **US4 (P2)**: Depends on US1 for document ownership and storage; schema changes depend on the migration baseline.
- **US5 (P2)**: Depends on US1 for documents and authorization; schema changes depend on the migration baseline.
- **US6 (P3)**: Depends on US1 activity records, US4 sharing, and US5 project/task associations for complete notifications and reporting.

### Parallel Opportunities

- T001 and T002 can run in parallel.
- T004 and T005 can run in parallel; T007 and T008 can run in parallel.
- T045 and T046 are sequential and must complete before schema-changing tasks T024, T027, and T030.
- After US1, US2, US3, US4, and US5 can be assigned to separate contributors, observing sequential dependencies within shared files.
- In US4, model creation (T023) can run in parallel with independent UI groundwork only if the UI uses a different file; tasks are otherwise sequential where they touch `Documents.razor`, `NavMenu.razor`, or `ApplicationDbContext.cs`.
- In US6, dashboard data work (T035) can run in parallel with retention work (T038); registration waits for T038.

## Parallel Example: User Story 1

```text
T004 Create Document model in ContosoDashboard/Models/Document.cs
T005 Create DocumentActivityLog model in ContosoDashboard/Models/DocumentActivityLog.cs
T007 Define IFileStorageService in ContosoDashboard/Services/IFileStorageService.cs
T008 Define IFileScanService in ContosoDashboard/Services/IFileScanService.cs
```

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational phases.
2. Complete US1 upload and basic My Documents view.
3. Validate US1 independently with the upload scenarios in `quickstart.md`.
4. Complete Phase 4 to enable safe, versioned schema changes before adding more EF entities.
5. Add US2 browsing and retrieval for useful document discovery.

### Incremental Delivery

1. US1 establishes secure local uploads.
2. US2 provides authorized document discovery and retrieval.
3. US3 adds document maintenance.
4. US4 enables controlled sharing.
5. US5 connects documents to project and task workflows.
6. US6 adds notifications, reporting, and retention.
7. Polish validates security and stated performance goals.

## Notes

- Every task uses the required checkbox, sequential ID, optional parallel marker, story label for story tasks, and concrete file path.
- Automated test tasks are omitted because no test project exists and TDD was not requested; every story has an independent manual acceptance criterion.
- `[P]` tasks operate on separate files and have no dependency on another incomplete task.
- Field constraints are canonical in `data-model.md`; model implementation tasks cite each constrained `Entity.field` directly.
