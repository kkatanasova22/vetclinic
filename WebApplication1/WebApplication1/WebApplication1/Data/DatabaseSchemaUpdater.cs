using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data;

public static class DatabaseSchemaUpdater
{
    // Добавя новите таблици и към вече създадена SQLite база.
    public static async Task ApplyAsync(VetClinicDbContext db)
    {
        if (db.Database.IsSqlServer())
        {
            await ApplySqlServerAsync(db);
            return;
        }

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ClientProfiles" (
                "UserId" TEXT NOT NULL CONSTRAINT "PK_ClientProfiles" PRIMARY KEY,
                "OwnerId" INTEGER NOT NULL,
                CONSTRAINT "FK_ClientProfiles_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ClientProfiles_Owners_OwnerId"
                    FOREIGN KEY ("OwnerId") REFERENCES "Owners" ("OwnerId") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ClientProfiles_OwnerId"
                ON "ClientProfiles" ("OwnerId");

            CREATE TABLE IF NOT EXISTS "ClinicFiles" (
                "ClinicFileId" INTEGER NOT NULL CONSTRAINT "PK_ClinicFiles" PRIMARY KEY AUTOINCREMENT,
                "OriginalFileName" TEXT NOT NULL,
                "StoredFileName" TEXT NOT NULL,
                "ThumbnailFileName" TEXT NULL,
                "ContentType" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "AppointmentId" INTEGER NOT NULL,
                "UploadedByUserId" TEXT NOT NULL,
                CONSTRAINT "CK_ClinicFiles_Size"
                    CHECK ("SizeBytes" > 0 AND "SizeBytes" <= 10485760),
                CONSTRAINT "FK_ClinicFiles_Appointments_AppointmentId"
                    FOREIGN KEY ("AppointmentId") REFERENCES "Appointments" ("AppointmentId") ON DELETE RESTRICT,
                CONSTRAINT "FK_ClinicFiles_AspNetUsers_UploadedByUserId"
                    FOREIGN KEY ("UploadedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ClinicFiles_StoredFileName"
                ON "ClinicFiles" ("StoredFileName");
            """);
    }

    // Добавя същите нови таблици към вече създадена база на SQL Server.
    private static async Task ApplySqlServerAsync(VetClinicDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ClientProfiles', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ClientProfiles (
                    UserId nvarchar(450) NOT NULL CONSTRAINT PK_ClientProfiles PRIMARY KEY,
                    OwnerId int NOT NULL,
                    CONSTRAINT FK_ClientProfiles_AspNetUsers_UserId FOREIGN KEY (UserId)
                        REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_ClientProfiles_Owners_OwnerId FOREIGN KEY (OwnerId)
                        REFERENCES dbo.Owners(OwnerId) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IX_ClientProfiles_OwnerId ON dbo.ClientProfiles(OwnerId);
            END;

            IF OBJECT_ID(N'dbo.ClinicFiles', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ClinicFiles (
                    ClinicFileId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClinicFiles PRIMARY KEY,
                    OriginalFileName nvarchar(180) NOT NULL,
                    StoredFileName nvarchar(80) NOT NULL,
                    ThumbnailFileName nvarchar(80) NULL,
                    ContentType nvarchar(100) NOT NULL,
                    SizeBytes bigint NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    AppointmentId int NOT NULL,
                    UploadedByUserId nvarchar(450) NOT NULL,
                    CONSTRAINT CK_ClinicFiles_Size CHECK (SizeBytes > 0 AND SizeBytes <= 10485760),
                    CONSTRAINT FK_ClinicFiles_Appointments_AppointmentId FOREIGN KEY (AppointmentId)
                        REFERENCES dbo.Appointments(AppointmentId) ON DELETE NO ACTION,
                    CONSTRAINT FK_ClinicFiles_AspNetUsers_UploadedByUserId FOREIGN KEY (UploadedByUserId)
                        REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX IX_ClinicFiles_StoredFileName ON dbo.ClinicFiles(StoredFileName);
            END;
            """);
    }
}
