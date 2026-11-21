# Quickstart Validation: Document Management

## Prerequisites

- Run in a local development environment with .NET 10 SDK
- Use SQL Server LocalDB
- Login with the existing mock authentication users
- Ensure local write permission to `AppData/uploads`

## Start the Application

```powershell
cd D:\Repo\EstudioRepo\ContosoDashboard-SSD\ContosoDashboard
dotnet run
```

Open the application and sign in through the mock login page.

## Validate Upload

1. Sign in as Ni Kang.
2. Open Documents.
3. Select a PDF smaller than 25 MB.
4. Enter a title and category.
5. Submit the upload.

Expected:
- Progress is visible.
- Success is shown.
- The document appears in My Documents.
- Uploader, size, type, category, title, and upload time are shown.

## Validate Validation and Security

1. Attempt an unsupported file type.
2. Attempt a file over 25 MB.
3. Attempt direct access to a document owned by another user.

Expected:
- Unsupported and oversized uploads are rejected clearly.
- Unauthorized direct URLs reveal no metadata or file bytes.

## Validate Browse and Search

1. Upload documents in multiple categories and projects.
2. Sort by title, upload date, category, and file size.
3. Filter by category, project, and date range.
4. Search title, description, tag, uploader name, and project name.

Expected:
- Sorting and filtering produce the expected permitted results.
- Unauthorized documents do not appear.
- Search results complete within 2 seconds for the training dataset.

## Validate Share and Project Behavior

1. Sign in as a document owner.
2. Share the document with a user or team.
3. Sign in as the recipient.

Expected:
- A notification is received.
- The document appears in Shared with Me.
- Revoking the share removes access immediately.

1. Open an existing project.
2. Confirm project members can view and download documents.
3. Mark the project inactive.

Expected:
- Existing documents remain visible and downloadable.
- Upload, edit, replace, share, and delete actions are unavailable until the project is reactivated.

## Validate Project Deletion

1. Delete a project containing associated documents.

Expected:
- Associated document metadata and files are permanently removed.

## Validate Reports and Activity

1. Perform upload, download, edit, share, replace, and delete actions.
2. Sign in as Administrator and open document reporting.

Expected:
- Every successful or denied relevant action needed by the audit design is traceable to actor and time.
- Records dated within the last 180 days remain available.
- Records older than 180 days are absent after startup, scheduled, or report-before-query cleanup.
