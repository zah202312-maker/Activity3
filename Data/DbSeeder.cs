using ActivityApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();
        EnsureCurrentSchema(db);

        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { Name = "Admin", ArRole = "مدير النظام", Note = "صلاحيات كاملة" },
                new Role { Name = "User", ArRole = "مستخدم", Note = "صلاحيات الإدخال والعرض" });
            db.SaveChanges();
        }

        // محافظات العراق: تُضاف الناقصة فقط عند كل تشغيل (لا تكرار)
        var existingGovs = db.Governorates.Select(g => g.Name).ToList().Select(n => n.Trim()).ToHashSet();
        var missingGovs = ActivityApp.Helpers.IraqGovernorates.All.Where(n => !existingGovs.Contains(n)).ToList();
        if (missingGovs.Count > 0)
        {
            db.Governorates.AddRange(missingGovs.Select(n => new Governorate { Name = n }));
            db.SaveChanges();
        }

        if (!db.Users.Any())
        {
            var admin = new AppUser { UserName = "admin", Active = true, UserType = UserType.Admin };
            admin.PasswordHash = new PasswordHasher<AppUser>().HashPassword(admin, "Admin@123");
            var adminRole = db.Roles.First(r => r.Name == "Admin");
            admin.UserRoles.Add(new UserRole { Role = adminRole });
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }

    private static void EnsureCurrentSchema(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH(N'dbo.Activities', N'TroopName') IS NULL
                ALTER TABLE dbo.Activities ADD TroopName nvarchar(250) NULL;
            IF COL_LENGTH(N'dbo.Activities', N'GovernorateId') IS NULL
                ALTER TABLE dbo.Activities ADD GovernorateId int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'OfficeId') IS NULL
                ALTER TABLE dbo.Activities ADD OfficeId int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'NumberOfPeopleEvaluation') IS NULL
                ALTER TABLE dbo.Activities ADD NumberOfPeopleEvaluation int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ActivityPlaceEvaluation') IS NULL
                ALTER TABLE dbo.Activities ADD ActivityPlaceEvaluation int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ScoutUniformEvaluation') IS NULL
                ALTER TABLE dbo.Activities ADD ScoutUniformEvaluation int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'FlagsEvaluation') IS NULL
                ALTER TABLE dbo.Activities ADD FlagsEvaluation int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ActivityReportEvaluation') IS NULL
                ALTER TABLE dbo.Activities ADD ActivityReportEvaluation int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ActivityOrganizationEvaluation') IS NULL
                ALTER TABLE dbo.Activities ADD ActivityOrganizationEvaluation int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'NumberOfPeopleScore') IS NULL
                ALTER TABLE dbo.Activities ADD NumberOfPeopleScore int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ActivityPlaceScore') IS NULL
                ALTER TABLE dbo.Activities ADD ActivityPlaceScore int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ScoutUniformScore') IS NULL
                ALTER TABLE dbo.Activities ADD ScoutUniformScore int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'FlagsScore') IS NULL
                ALTER TABLE dbo.Activities ADD FlagsScore int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ActivityReportScore') IS NULL
                ALTER TABLE dbo.Activities ADD ActivityReportScore int NULL;
            IF COL_LENGTH(N'dbo.Activities', N'ActivityOrganizationScore') IS NULL
                ALTER TABLE dbo.Activities ADD ActivityOrganizationScore int NULL;

            IF COL_LENGTH(N'dbo.People', N'GovernorateId') IS NULL
                ALTER TABLE dbo.People ADD GovernorateId int NULL;
            IF COL_LENGTH(N'dbo.People', N'OfficeId') IS NULL
                ALTER TABLE dbo.People ADD OfficeId int NULL;
            IF COL_LENGTH(N'dbo.People', N'RolePlayed') IS NULL
                ALTER TABLE dbo.People ADD RolePlayed nvarchar(250) NULL;
            IF COL_LENGTH(N'dbo.People', N'Education') IS NULL
                ALTER TABLE dbo.People ADD Education nvarchar(200) NULL;
            IF COL_LENGTH(N'dbo.People', N'BirthDate') IS NULL
                ALTER TABLE dbo.People ADD BirthDate datetime2 NULL;
            IF COL_LENGTH(N'dbo.People', N'Residence') IS NULL
                ALTER TABLE dbo.People ADD Residence nvarchar(300) NULL;
            IF COL_LENGTH(N'dbo.People', N'Phone') IS NULL
                ALTER TABLE dbo.People ADD Phone nvarchar(30) NULL;
            IF COL_LENGTH(N'dbo.People', N'IsEmployee') IS NULL
                ALTER TABLE dbo.People ADD IsEmployee bit NOT NULL
                    CONSTRAINT DF_People_IsEmployee DEFAULT 0;
            IF COL_LENGTH(N'dbo.People', N'ScoutCourses') IS NULL
                ALTER TABLE dbo.People ADD ScoutCourses int NOT NULL
                    CONSTRAINT DF_People_ScoutCourses DEFAULT 0;
            IF COL_LENGTH(N'dbo.People', N'BadgesCount') IS NULL
                ALTER TABLE dbo.People ADD BadgesCount int NOT NULL
                    CONSTRAINT DF_People_BadgesCount DEFAULT 0;
            IF COL_LENGTH(N'dbo.People', N'Capacity') IS NULL
                ALTER TABLE dbo.People ADD Capacity int NOT NULL
                    CONSTRAINT DF_People_Capacity DEFAULT 0;
            IF COL_LENGTH(N'dbo.People', N'HasPhoto') IS NULL
                ALTER TABLE dbo.People ADD HasPhoto bit NOT NULL
                    CONSTRAINT DF_People_HasPhoto DEFAULT 0;

            IF OBJECT_ID(N'dbo.PersonPhotos', N'U') IS NULL
                CREATE TABLE dbo.PersonPhotos (
                    PersonId int NOT NULL,
                    ContentType nvarchar(max) NOT NULL,
                    Data varbinary(max) NOT NULL,
                    CONSTRAINT PK_PersonPhotos PRIMARY KEY (PersonId),
                    CONSTRAINT FK_PersonPhotos_People_PersonId
                        FOREIGN KEY (PersonId) REFERENCES dbo.People (Id) ON DELETE CASCADE
                );
            """);
    }
}
