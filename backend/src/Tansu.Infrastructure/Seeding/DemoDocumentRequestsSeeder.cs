using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;

namespace Tansu.Infrastructure.Seeding;

public static class DemoDocumentRequestsSeeder
{
    private sealed record MatrixTemplate(string RequestType, string[] Roles);

    private static readonly MatrixTemplate[] DefaultMatrices =
    [
        new(DocumentRequestType.Leave, [ApproverRole.HR, ApproverRole.Management]),
        new(DocumentRequestType.Ticket, [ApproverRole.HR, ApproverRole.Accounting]),
        new(DocumentRequestType.Document, [ApproverRole.Accounting, ApproverRole.Finance, ApproverRole.Management]),
        new(DocumentRequestType.Expense, [ApproverRole.Accounting, ApproverRole.Finance, ApproverRole.Management]),
    ];

    public static readonly (string Email, string Role)[] RoleAssignments =
    [
        (DemoSeedData.TansuAdminEmail, ApproverRole.Management),
        (DemoSeedData.TansuApprovers[0].Email, ApproverRole.Management),
        (DemoSeedData.TansuApprovers[1].Email, ApproverRole.HR),
        (DemoSeedData.TansuApprovers[2].Email, ApproverRole.Finance),
        (DemoSeedData.AccountingEmail, ApproverRole.Accounting),
    ];

    public static async Task EnsureAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("DemoDocumentRequestsSeeder");

        foreach (var (email, role) in RoleAssignments)
        {
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user is null && email == DemoSeedData.AccountingEmail)
            {
                user = new User
                {
                    FullName = DemoSeedData.AccountingFullName,
                    Position = DemoSeedData.AccountingPosition,
                    Email = email,
                    UserType = UserType.Tansu,
                    ApproverRole = role,
                    IsActive = true
                };
                ctx.Users.Add(user);
            }
            else if (user is not null && user.ApproverRole != role)
            {
                user.ApproverRole = role;
                if (email == DemoSeedData.AccountingEmail)
                {
                    user.FullName = DemoSeedData.AccountingFullName;
                    user.Position = DemoSeedData.AccountingPosition;
                }
            }
        }

        await ctx.SaveChangesAsync();

        var subs = await ctx.Subcontractors
            .Where(s => s.Bin == DemoSeedData.SubMontazhBin || s.Bin == DemoSeedData.SubEnergoBin)
            .ToListAsync();

        foreach (var sub in subs)
        {
            var projectOids = await ctx.ProjectSubcontractors
                .Where(x => x.SubcontractorId == sub.Id)
                .Select(x => x.ProjectOid)
                .ToListAsync();

            foreach (var projectOid in projectOids)
            {
                foreach (var template in DefaultMatrices)
                {
                    var hasMatrix = await ctx.DocumentApprovalMatrix.AnyAsync(m =>
                        m.ProjectOid == projectOid &&
                        m.SubcontractorId == sub.Id &&
                        m.RequestType == template.RequestType);

                    if (hasMatrix) continue;

                    for (var i = 0; i < template.Roles.Length; i++)
                    {
                        ctx.DocumentApprovalMatrix.Add(new DocumentRequestMatrixEntry
                        {
                            ProjectOid = projectOid,
                            SubcontractorId = sub.Id,
                            RequestType = template.RequestType,
                            OrderNo = i + 1,
                            ApproverRole = template.Roles[i]
                        });
                    }

                    logger.LogInformation(
                        "Матрица заявок: проект {Project}, тип {Type}",
                        projectOid, template.RequestType);
                }
            }
        }

        await ctx.SaveChangesAsync();
    }
}
