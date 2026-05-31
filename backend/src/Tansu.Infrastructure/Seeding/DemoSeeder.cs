using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoSeeder
{
    public static readonly Guid DemoProjectOid = DemoSeedData.ProjectKeremetOid;
    public const string TansuAdminEmail = DemoSeedData.TansuAdminEmail;
    public const string SubcontractorEmail = DemoSeedData.SubMontazhEmail;
    public const string SubcontractorTempPassword = DemoSeedData.SubcontractorTempPassword;

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder");

        if (await ctx.Subcontractors.AnyAsync())
        {
            logger.LogInformation("Начальные данные уже есть.");
            return;
        }

        logger.LogInformation("Загрузка начальных данных...");

        ctx.ProjectRefs.Add(new ProjectRef
        {
            ProjectOid = DemoSeedData.ProjectKeremetOid,
            Name = DemoSeedData.ProjectKeremetName
        });
        ctx.ProjectRefs.Add(new ProjectRef
        {
            ProjectOid = DemoSeedData.ProjectAbayTowerOid,
            Name = DemoSeedData.ProjectAbayTowerName
        });

        var subMontazh = new Subcontractor
        {
            Name = DemoSeedData.SubMontazhName,
            Bin = DemoSeedData.SubMontazhBin
        };
        var subEnergo = new Subcontractor
        {
            Name = DemoSeedData.SubEnergoName,
            Bin = DemoSeedData.SubEnergoBin
        };
        ctx.Subcontractors.Add(subMontazh);
        ctx.Subcontractors.Add(subEnergo);

        var admin = new User
        {
            FullName = DemoSeedData.TansuAdminFullName,
            Position = DemoSeedData.TansuAdminPosition,
            Email = DemoSeedData.TansuAdminEmail,
            UserType = UserType.Tansu,
            PasswordHash = null,
            IsActive = true,
            IsSuperUser = true
        };
        var subMontazhUser = new User
        {
            FullName = DemoSeedData.SubMontazhUserFullName,
            Position = DemoSeedData.SubMontazhUserPosition,
            Email = DemoSeedData.SubMontazhEmail,
            UserType = UserType.Subcontractor,
            SubcontractorId = subMontazh.Id,
            PasswordHash = hasher.Hash(SubcontractorTempPassword),
            MustChangePassword = true,
            IsActive = true
        };
        var subEnergoUser = new User
        {
            FullName = DemoSeedData.SubEnergoUserFullName,
            Position = DemoSeedData.SubEnergoUserPosition,
            Email = DemoSeedData.SubEnergoEmail,
            UserType = UserType.Subcontractor,
            SubcontractorId = subEnergo.Id,
            PasswordHash = hasher.Hash(SubcontractorTempPassword),
            MustChangePassword = true,
            IsActive = true
        };
        ctx.Users.Add(admin);
        ctx.Users.Add(subMontazhUser);
        ctx.Users.Add(subEnergoUser);

        ctx.ProjectSubcontractors.Add(new ProjectSubcontractor
        {
            ProjectOid = DemoSeedData.ProjectKeremetOid,
            SubcontractorId = subMontazh.Id
        });
        ctx.ProjectSubcontractors.Add(new ProjectSubcontractor
        {
            ProjectOid = DemoSeedData.ProjectAbayTowerOid,
            SubcontractorId = subEnergo.Id
        });

        var approverUsers = new List<User>();
        foreach (var seed in DemoSeedData.TansuApprovers)
        {
            approverUsers.Add(new User
            {
                FullName = seed.FullName,
                Position = seed.Position,
                Email = seed.Email,
                UserType = UserType.Tansu,
                IsActive = true
            });
        }
        ctx.Users.AddRange(approverUsers);

        await ctx.SaveChangesAsync();

        var chain = new[] { admin.Id, approverUsers[0].Id, approverUsers[1].Id, approverUsers[2].Id };

        await DemoApproversSeeder.ApplyMatrixAsync(
            ctx, DemoSeedData.ProjectKeremetOid, subMontazh.Id, chain, logger);
        await DemoApproversSeeder.ApplyMatrixAsync(
            ctx, DemoSeedData.ProjectAbayTowerOid, subEnergo.Id, chain, logger);

        await ctx.SaveChangesAsync();
        logger.LogInformation("Начальные данные загружены.");
    }

    public static async Task EnsureSubcontractorCredentialsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder");

        foreach (var email in new[] { DemoSeedData.SubMontazhEmail, DemoSeedData.SubEnergoEmail, DemoSeedData.LegacyMontazhHrEmail })
        {
            var user = await ctx.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.ToLower());
            if (user is null || user.UserType != UserType.Subcontractor)
                continue;

            if (user.PasswordHash is not null &&
                hasher.Verify(SubcontractorTempPassword, user.PasswordHash))
                continue;

            user.PasswordHash = hasher.Hash(SubcontractorTempPassword);
            user.MustChangePassword = true;
            user.IsActive = true;
            logger.LogInformation("Сброс пароля субподрядчика: {Email}.", user.Email);
        }

        await ctx.SaveChangesAsync();
    }

    public static async Task EnsureKazakhCompanyProfilesAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder");

        var changed = false;

        async Task EnsureProject(Guid oid, string name)
        {
            var project = await ctx.ProjectRefs.FirstOrDefaultAsync(p => p.ProjectOid == oid);
            if (project is null)
            {
                ctx.ProjectRefs.Add(new ProjectRef { ProjectOid = oid, Name = name });
                changed = true;
                return;
            }

            if (project.Name != name)
            {
                project.Name = name;
                changed = true;
            }
        }

        await EnsureProject(DemoSeedData.ProjectKeremetOid, DemoSeedData.ProjectKeremetName);
        await EnsureProject(DemoSeedData.ProjectAbayTowerOid, DemoSeedData.ProjectAbayTowerName);

        async Task<bool> TryAssignBin(Subcontractor sub, string targetBin)
        {
            if (sub.Bin == targetBin) return false;
            var taken = await ctx.Subcontractors.AnyAsync(s => s.Bin == targetBin && s.Id != sub.Id);
            if (taken) return false;
            sub.Bin = targetBin;
            return true;
        }

        var subMontazhUser = await ctx.Users
            .Include(u => u.Subcontractor)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == DemoSeedData.SubMontazhEmail.ToLower()
                || u.Email.ToLower() == "sub@example.local");

        Subcontractor? montazh = subMontazhUser?.Subcontractor
            ?? await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubMontazhBin);

        if (montazh is null)
        {
            montazh = new Subcontractor { Bin = DemoSeedData.SubMontazhBin, Name = DemoSeedData.SubMontazhName };
            ctx.Subcontractors.Add(montazh);
            changed = true;
        }
        else
        {
            if (montazh.Name != DemoSeedData.SubMontazhName) { montazh.Name = DemoSeedData.SubMontazhName; changed = true; }
            if (await TryAssignBin(montazh, DemoSeedData.SubMontazhBin)) changed = true;
        }

        if (subMontazhUser is not null)
        {
            var hrUser = await ctx.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == DemoSeedData.SubMontazhEmail.ToLower());

            if (hrUser is not null && hrUser.Id != subMontazhUser.Id)
            {
                if (hrUser.SubcontractorId != montazh.Id) { hrUser.SubcontractorId = montazh.Id; changed = true; }
                if (hrUser.FullName != DemoSeedData.SubMontazhUserFullName) { hrUser.FullName = DemoSeedData.SubMontazhUserFullName; changed = true; }
                if (hrUser.Position != DemoSeedData.SubMontazhUserPosition) { hrUser.Position = DemoSeedData.SubMontazhUserPosition; changed = true; }

                if (subMontazhUser.Email.Equals(DemoSeedData.LegacyMontazhHrEmail, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Users.Remove(subMontazhUser);
                    changed = true;
                }
            }
            else
            {
                if (subMontazhUser.Email != DemoSeedData.SubMontazhEmail)
                {
                    subMontazhUser.Email = DemoSeedData.SubMontazhEmail;
                    changed = true;
                }
                if (subMontazhUser.SubcontractorId != montazh.Id) { subMontazhUser.SubcontractorId = montazh.Id; changed = true; }
                if (subMontazhUser.FullName != DemoSeedData.SubMontazhUserFullName) { subMontazhUser.FullName = DemoSeedData.SubMontazhUserFullName; changed = true; }
                if (subMontazhUser.Position != DemoSeedData.SubMontazhUserPosition) { subMontazhUser.Position = DemoSeedData.SubMontazhUserPosition; changed = true; }
            }
        }

        if (changed)
        {
            await ctx.SaveChangesAsync();
            changed = false;
        }

        var energo = await ctx.Subcontractors.FirstOrDefaultAsync(s => s.Bin == DemoSeedData.SubEnergoBin);
        if (energo is null)
        {
            energo = new Subcontractor { Bin = DemoSeedData.SubEnergoBin, Name = DemoSeedData.SubEnergoName };
            ctx.Subcontractors.Add(energo);
            changed = true;
        }
        else if (energo.Name != DemoSeedData.SubEnergoName)
        {
            energo.Name = DemoSeedData.SubEnergoName;
            changed = true;
        }

        async Task EnsureUser(string email, string fullName, string position, string userType, Guid? subcontractorId = null)
        {
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user is null && userType == UserType.Subcontractor && subcontractorId is not null)
            {
                if (await ctx.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                    return;

                ctx.Users.Add(new User
                {
                    Email = email,
                    FullName = fullName,
                    Position = position,
                    UserType = userType,
                    SubcontractorId = subcontractorId,
                    PasswordHash = hasher.Hash(SubcontractorTempPassword),
                    MustChangePassword = true,
                    IsActive = true
                });
                changed = true;
                return;
            }

            if (user is null) return;
            if (user.FullName != fullName) { user.FullName = fullName; changed = true; }
            if (user.Position != position) { user.Position = position; changed = true; }
            if (email.Equals(DemoSeedData.TansuAdminEmail, StringComparison.OrdinalIgnoreCase) && !user.IsSuperUser)
            {
                user.IsSuperUser = true;
                changed = true;
            }
        }

        await EnsureUser(DemoSeedData.TansuAdminEmail, DemoSeedData.TansuAdminFullName, DemoSeedData.TansuAdminPosition, UserType.Tansu);
        await EnsureUser(DemoSeedData.SubMontazhEmail, DemoSeedData.SubMontazhUserFullName, DemoSeedData.SubMontazhUserPosition, UserType.Subcontractor, montazh.Id);
        await EnsureUser(DemoSeedData.SubEnergoEmail, DemoSeedData.SubEnergoUserFullName, DemoSeedData.SubEnergoUserPosition, UserType.Subcontractor, energo.Id);
        foreach (var seed in DemoSeedData.TansuApprovers)
            await EnsureUser(seed.Email, seed.FullName, seed.Position, UserType.Tansu);
        await EnsureUser(DemoSeedData.AccountingEmail, DemoSeedData.AccountingFullName, DemoSeedData.AccountingPosition, UserType.Tansu);

        if (!await ctx.ProjectSubcontractors.AnyAsync(x =>
                x.SubcontractorId == montazh.Id && x.ProjectOid == DemoSeedData.ProjectKeremetOid))
        {
            ctx.ProjectSubcontractors.Add(new ProjectSubcontractor
            {
                ProjectOid = DemoSeedData.ProjectKeremetOid,
                SubcontractorId = montazh.Id
            });
            changed = true;
        }

        if (!await ctx.ProjectSubcontractors.AnyAsync(x =>
                x.SubcontractorId == energo.Id && x.ProjectOid == DemoSeedData.ProjectAbayTowerOid))
        {
            ctx.ProjectSubcontractors.Add(new ProjectSubcontractor
            {
                ProjectOid = DemoSeedData.ProjectAbayTowerOid,
                SubcontractorId = energo.Id
            });
            changed = true;
        }

        if (changed)
        {
            await ctx.SaveChangesAsync();
            logger.LogInformation("Профили организаций обновлены.");
        }
    }
}
