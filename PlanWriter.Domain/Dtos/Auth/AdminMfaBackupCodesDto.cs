using System.Collections.Generic;

namespace PlanWriter.Domain.Dtos.Auth;

public sealed class AdminMfaBackupCodesDto
{
    public IReadOnlyList<string> BackupCodes { get; init; } = [];
}
