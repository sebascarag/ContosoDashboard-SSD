# Document HTTP Contract

This contract describes user-facing server operations. UI interactions use the existing Blazor Server event model; file bytes are served through authorized HTTP endpoints.

## Authorized Preview

`GET /documents/{documentId}/preview`

Requirements:
- Authenticated request
- Document access must pass service authorization
- Supported for PDF and image files
- Returns the file inline when authorized
- Rejects unauthorized, missing, deleted, or unsupported documents

## Authorized Download

`GET /documents/{documentId}/download`

Requirements:
- Authenticated request
- Document access must pass service authorization
- Returns the original file as an attachment
- Records a successful download activity

## Upload

The upload UI submits to the document service through Blazor Server.

Input:
- One or more selected files
- Title
- Optional description
- Required category
- Optional project ID
- Optional tags

Responses:
- Success with document metadata
- Validation error naming unsupported files or the 25 MB limit
- Authorization error
- Storage or scan failure with no usable orphaned document

## Metadata Update

Input:
- Document ID
- Title
- Optional description
- Category
- Optional project ID
- Optional tags

Responses:
- Updated document metadata
- Authorization or validation error

## File Replacement

Input:
- Document ID
- Replacement file

Requirements:
- Same file-size, type, and scan validation as upload
- Save the replacement before replacing the persisted path

## Delete

Input:
- Document ID
- Explicit confirmation

Responses:
- Success after metadata and file deletion
- Authorization or confirmation error

## Sharing

Input:
- Document ID
- Optional recipient user ID
- Optional team recipient

Responses:
- Share success
- Unauthorized recipient or invalid target error
- In-app notification creation result

## Browse and Search

Operations:
- My Documents
- Project Documents
- Shared with Me
- Search across title, description, tags, uploader name, and project

Requirements:
- Every result has passed authorization
- Browse supports category, project, date-range, title, upload-date, category, and file-size sorting/filtering
