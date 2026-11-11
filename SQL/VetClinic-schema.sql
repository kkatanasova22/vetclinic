/*
  Учебна схема за Microsoft SQL Server.
  Приложението за локално стартиране ползва SQLite; тази схема показва
  същите основни връзки за заданието по бази данни.
*/
IF DB_ID(N'VetClinicDB') IS NULL
    CREATE DATABASE VetClinicDB;
GO

USE VetClinicDB;
GO

-- Таблиците за вход са стандартните таблици на ASP.NET Identity.
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetRoles'))
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL,
        TwoFactorEnabled BIT NOT NULL,
        LockoutEnd DATETIMEOFFSET NULL,
        LockoutEnabled BIT NOT NULL,
        AccessFailedCount INT NOT NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId)
            REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetRoleClaims'))
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserClaims'))
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins (
        LoginProvider NVARCHAR(450) NOT NULL,
        ProviderKey NVARCHAR(450) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserLogins'))
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId)
            REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetUserRoles'))
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens (
        UserId NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(450) NOT NULL,
        Name NVARCHAR(450) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;
GO

-- Собственици: имейлът е уникален, за да няма дублирани клиентски записи.
IF OBJECT_ID(N'dbo.Owners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Owners (
        OwnerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Owners PRIMARY KEY,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Phone NVARCHAR(20) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        CONSTRAINT UQ_Owners_Email UNIQUE (Email),
        CONSTRAINT CK_Owners_Name CHECK (LEN(LTRIM(RTRIM(FirstName))) >= 2 AND LEN(LTRIM(RTRIM(LastName))) >= 2)
    );
END;
GO

-- Ветеринарните лекари се избират при записване на час.
IF OBJECT_ID(N'dbo.Veterinarians', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Veterinarians (
        VeterinarianId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Veterinarians PRIMARY KEY,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Specialty NVARCHAR(100) NULL,
        CONSTRAINT CK_Veterinarians_Name CHECK (LEN(LTRIM(RTRIM(FirstName))) >= 2 AND LEN(LTRIM(RTRIM(LastName))) >= 2)
    );
END;
GO

-- Всеки любимец принадлежи на един собственик.
IF OBJECT_ID(N'dbo.Pets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pets (
        PetId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pets PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        Species NVARCHAR(30) NOT NULL,
        Breed NVARCHAR(50) NULL,
        Sex NVARCHAR(20) NULL,
        BirthDate DATETIME2 NULL,
        OwnerId INT NOT NULL,
        CONSTRAINT FK_Pets_Owners FOREIGN KEY (OwnerId) REFERENCES dbo.Owners(OwnerId),
        CONSTRAINT CK_Pets_Name CHECK (LEN(LTRIM(RTRIM(Name))) >= 2)
    );
    CREATE INDEX IX_Pets_OwnerId ON dbo.Pets(OwnerId);
END;
GO

-- Часът свързва пациент, ветеринар и статус на прегледа.
IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments (
        AppointmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY,
        PetId INT NOT NULL,
        VeterinarianId INT NOT NULL,
        AppointmentDate DATETIME2 NOT NULL,
        Diagnosis NVARCHAR(255) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT N'Confirmed',
        CONSTRAINT FK_Appointments_Pets FOREIGN KEY (PetId) REFERENCES dbo.Pets(PetId),
        CONSTRAINT FK_Appointments_Veterinarians FOREIGN KEY (VeterinarianId) REFERENCES dbo.Veterinarians(VeterinarianId),
        CONSTRAINT CK_Appointments_Status CHECK (Status IN (N'Confirmed', N'In progress', N'Completed', N'Cancelled'))
    );
    CREATE INDEX IX_Appointments_Date ON dbo.Appointments(AppointmentDate);
END;
GO

-- Процедурите пазят цена и връзка към преглед.
IF OBJECT_ID(N'dbo.Treatments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Treatments (
        TreatmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Treatments PRIMARY KEY,
        AppointmentId INT NOT NULL,
        TreatmentName NVARCHAR(100) NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_Treatments_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments(AppointmentId),
        CONSTRAINT CK_Treatments_Price CHECK (Price >= 0)
    );
END;
GO

-- Файловете са към преглед; съдържанието им се пази отделно от wwwroot.
IF OBJECT_ID(N'dbo.ClinicFiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClinicFiles (
        ClinicFileId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClinicFiles PRIMARY KEY,
        OriginalFileName NVARCHAR(180) NOT NULL,
        StoredFileName NVARCHAR(80) NOT NULL,
        ThumbnailFileName NVARCHAR(80) NULL,
        ContentType NVARCHAR(100) NOT NULL,
        SizeBytes BIGINT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ClinicFiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        AppointmentId INT NOT NULL,
        UploadedByUserId NVARCHAR(450) NOT NULL,
        CONSTRAINT UQ_ClinicFiles_StoredFileName UNIQUE (StoredFileName),
        CONSTRAINT FK_ClinicFiles_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments(AppointmentId),
        CONSTRAINT FK_ClinicFiles_AspNetUsers FOREIGN KEY (UploadedByUserId) REFERENCES dbo.AspNetUsers(Id),
        CONSTRAINT CK_ClinicFiles_Size CHECK (SizeBytes > 0 AND SizeBytes <= 10485760)
    );
END;
GO

-- Един профил за вход се свързва с един собственик. Identity създава AspNetUsers.
IF OBJECT_ID(N'dbo.ClientProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientProfiles (
        UserId NVARCHAR(450) NOT NULL CONSTRAINT PK_ClientProfiles PRIMARY KEY,
        OwnerId INT NOT NULL,
        CONSTRAINT UQ_ClientProfiles_OwnerId UNIQUE (OwnerId),
        CONSTRAINT FK_ClientProfiles_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
        CONSTRAINT FK_ClientProfiles_Owners FOREIGN KEY (OwnerId) REFERENCES dbo.Owners(OwnerId)
    );
END;
GO

-- При повторно пускане добавяме липсващите връзки към Identity.
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.ClientProfiles')
      AND referenced_object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    ALTER TABLE dbo.ClientProfiles ADD CONSTRAINT FK_ClientProfiles_AspNetUsers
        FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id);

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.ClinicFiles')
      AND referenced_object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    ALTER TABLE dbo.ClinicFiles ADD CONSTRAINT FK_ClinicFiles_AspNetUsers
        FOREIGN KEY (UploadedByUserId) REFERENCES dbo.AspNetUsers(Id);
GO

-- Начални собственици за демонстрация на връзките.
IF NOT EXISTS (SELECT 1 FROM dbo.Owners WHERE Email = N'ivan@example.local')
    INSERT INTO dbo.Owners (FirstName, LastName, Phone, Email)
    VALUES (N'Иван', N'Петров', N'0888123456', N'ivan@example.local');

IF NOT EXISTS (SELECT 1 FROM dbo.Owners WHERE Email = N'maria@example.local')
    INSERT INTO dbo.Owners (FirstName, LastName, Phone, Email)
    VALUES (N'Мария', N'Георгиева', N'0888234567', N'maria@example.local');
GO

-- Любимците се добавят към съответните собственици, без да се повтарят.
IF NOT EXISTS (SELECT 1 FROM dbo.Pets WHERE Name = N'Рекс')
    INSERT INTO dbo.Pets (Name, Species, Breed, Sex, BirthDate, OwnerId)
    SELECT N'Рекс', N'Куче', N'Немска овчарка', N'Мъжки', '20210510', OwnerId
    FROM dbo.Owners WHERE Email = N'ivan@example.local';

IF NOT EXISTS (SELECT 1 FROM dbo.Pets WHERE Name = N'Мая')
    INSERT INTO dbo.Pets (Name, Species, Breed, Sex, BirthDate, OwnerId)
    SELECT N'Мая', N'Котка', N'Сиамска', N'Женски', '20220315', OwnerId
    FROM dbo.Owners WHERE Email = N'maria@example.local';
GO

-- Двама начални ветеринарни лекари за демонстрация на графика.
IF NOT EXISTS (SELECT 1 FROM dbo.Veterinarians WHERE FirstName = N'Николай' AND LastName = N'Иванов')
    INSERT INTO dbo.Veterinarians (FirstName, LastName, Specialty) VALUES (N'Николай', N'Иванов', N'Хирургия');
IF NOT EXISTS (SELECT 1 FROM dbo.Veterinarians WHERE FirstName = N'Елена' AND LastName = N'Тодорова')
    INSERT INTO dbo.Veterinarians (FirstName, LastName, Specialty) VALUES (N'Елена', N'Тодорова', N'Терапия');
GO

-- Примерни часове и процедури показват връзките между таблиците.
IF NOT EXISTS (SELECT 1 FROM dbo.Appointments WHERE Diagnosis = N'Начален примерен преглед')
    INSERT INTO dbo.Appointments (PetId, VeterinarianId, AppointmentDate, Diagnosis, Status)
    SELECT p.PetId, v.VeterinarianId, DATEADD(DAY, 1, SYSDATETIME()), N'Начален примерен преглед', N'Confirmed'
    FROM dbo.Pets p CROSS JOIN dbo.Veterinarians v
    WHERE p.Name = N'Рекс' AND v.FirstName = N'Николай' AND v.LastName = N'Иванов';

IF NOT EXISTS (SELECT 1 FROM dbo.Treatments WHERE TreatmentName = N'Ваксинация')
    INSERT INTO dbo.Treatments (AppointmentId, TreatmentName, Price)
    SELECT AppointmentId, N'Ваксинация', 40.00 FROM dbo.Appointments
    WHERE Diagnosis = N'Начален примерен преглед';
GO
