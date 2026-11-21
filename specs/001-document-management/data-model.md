# Data Model: Document Management

## Document

The central metadata record for a stored file.

| Field | Type | Rules |
|---|---|---|
| `DocumentId` | integer | Primary key |
| `Title` | string | Required; maximum 255 characters |
| `Description` | string | Optional; maximum 2000 characters |
| `Category` | string | Required; one of the six predefined categories |
| `ProjectId` | integer | Optional foreign key to Project |
| `UploadedByUserId` | integer | Required foreign key to User |
| `UploadedAt` | datetime | Automatically set at successful upload |
| `FileName` | string | Original name retained for display/download; never used as a storage path |
| `FilePath` | string | Relative portable path or provider-specific blob name |
| `FileSizeBytes` | long | Maximum 25 MB per file |
| `FileType` | string | Required; maximum 255 characters |
| `CreatedAt` | datetime | Record creation time |
| `UpdatedAt` | datetime | Updated after metadata or file replacement |

Relationships:
- One uploader
- Zero or one project
- Zero or many task attachments
- Zero or many document shares
- Zero or many activity records

## DocumentShare

Represents explicit access granted to a user or team.

| Field | Type | Rules |
|---|---|---|
| `DocumentShareId` | integer | Primary key |
| `DocumentId` | integer | Required foreign key |
| `UserId` | integer | Optional recipient user |
| `TeamId` | integer/string | Optional recipient team; department or existing team entity |
| `SharedByUserId` | integer | Required foreign key |
| `SharedAt` | datetime | Automatically set |

Validation:
- Exactly one of `UserId` or team recipient must be selected.
- Sharing is denied unless the requester is the document owner or has another owner-authorized management role.
- Revocation removes recipient access immediately.

## DocumentTask

Associates a document with a task and inherits the task's project.

| Field | Type | Rules |
|---|---|---|
| `DocumentTaskId` | integer | Primary key |
| `DocumentId` | integer | Required |
| `TaskId` | integer | Required |

Validation:
- The associated task must belong to the same project as the document if the document has a project.
- The task attachment operation requires document access and task access.

## DocumentActivityLog

An immutable audit record for document actions.

| Field | Type | Rules |
|---|---|---|
| `DocumentActivityId` | integer | Primary key |
| `DocumentId` | integer | Optional; retained if the document was deleted |
| `ActorUserId` | integer | Required |
| `Action` | string | Upload, download, delete, share, revoke-share, edit-metadata, replace-file |
| `OccurredAt` | datetime | UTC timestamp |
| `Success` | boolean | Whether authorization and processing succeeded |

Retention:
- Purge records after 180 days.

## Existing Entity Relationships

- `User` owns documents and may receive explicit shares.
- `Project` contains documents and project members.
- `ProjectMember` determines default project-document access.
- `TaskItem` may reference documents and inherits the task project's project association.
- `Notification` receives document share and project-upload alerts.

## Lifecycle Rules

- Upload: validation and authorization -> generate path -> scan -> save file -> persist metadata -> notify project members.
- Metadata edit: authorization -> validate -> update.
- File replacement: authorization -> validate and scan -> save new file -> replace path -> delete old file only after persistence succeeds.
- Delete: authorization and confirmation -> persist deletion -> remove file.
- Share: owner authorization -> create share -> notify recipients.
- Project deletion: permanent document and file deletion.
- Project inactivity: preserve documents; only view and download remain available.
