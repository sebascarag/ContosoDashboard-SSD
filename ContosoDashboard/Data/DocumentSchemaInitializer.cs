using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

internal static class DocumentSchemaInitializer
{
    private const string CreateSchemaSql = """
        IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[Documents] (
                [DocumentId] int NOT NULL IDENTITY,
                [Title] nvarchar(255) NOT NULL,
                [Description] nvarchar(2000) NULL,
                [Category] nvarchar(100) NOT NULL,
                [Tags] nvarchar(max) NULL,
                [ProjectId] int NULL,
                [UploadedByUserId] int NOT NULL,
                [UploadedAt] datetime2 NOT NULL,
                [FileName] nvarchar(255) NOT NULL,
                [FilePath] nvarchar(1024) NOT NULL,
                [FileSizeBytes] bigint NOT NULL,
                [FileType] nvarchar(255) NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]),
                CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId])
                    REFERENCES [dbo].[Projects] ([ProjectId]) ON DELETE NO ACTION,
                CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId])
                    REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
            );
        END;

        IF OBJECT_ID(N'dbo.DocumentActivityLogs', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[DocumentActivityLogs] (
                [DocumentActivityId] int NOT NULL IDENTITY,
                [DocumentId] int NULL,
                [ActorUserId] int NOT NULL,
                [Action] nvarchar(50) NOT NULL,
                [OccurredAt] datetime2 NOT NULL,
                [Success] bit NOT NULL,
                CONSTRAINT [PK_DocumentActivityLogs] PRIMARY KEY ([DocumentActivityId]),
                CONSTRAINT [FK_DocumentActivityLogs_Documents_DocumentId] FOREIGN KEY ([DocumentId])
                    REFERENCES [dbo].[Documents] ([DocumentId]) ON DELETE SET NULL,
                CONSTRAINT [FK_DocumentActivityLogs_Users_ActorUserId] FOREIGN KEY ([ActorUserId])
                    REFERENCES [dbo].[Users] ([UserId]) ON DELETE NO ACTION
            );
        END;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE [name] = N'IX_Documents_UploadedByUserId_UploadedAt'
                AND [object_id] = OBJECT_ID(N'dbo.Documents'))
            CREATE INDEX [IX_Documents_UploadedByUserId_UploadedAt]
                ON [dbo].[Documents] ([UploadedByUserId], [UploadedAt]);

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE [name] = N'IX_Documents_ProjectId'
                AND [object_id] = OBJECT_ID(N'dbo.Documents'))
            CREATE INDEX [IX_Documents_ProjectId] ON [dbo].[Documents] ([ProjectId]);

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE [name] = N'IX_Documents_Category'
                AND [object_id] = OBJECT_ID(N'dbo.Documents'))
            CREATE INDEX [IX_Documents_Category] ON [dbo].[Documents] ([Category]);

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE [name] = N'IX_DocumentActivityLogs_OccurredAt'
                AND [object_id] = OBJECT_ID(N'dbo.DocumentActivityLogs'))
            CREATE INDEX [IX_DocumentActivityLogs_OccurredAt]
                ON [dbo].[DocumentActivityLogs] ([OccurredAt]);
        """;

    public static void EnsureCreated(ApplicationDbContext context)
    {
        context.Database.ExecuteSqlRaw(CreateSchemaSql);
    }
}
