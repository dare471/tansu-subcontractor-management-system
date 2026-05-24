namespace Tansu.Application.Matrix;

public sealed record MatrixStepDto(
    Guid Id,
    int OrderNo,
    Guid UserId,
    string UserFullName,
    string UserEmail);

public sealed record MatrixSummaryDto(
    Guid ProjectOid,
    string? ProjectName,
    Guid SubcontractorId,
    string SubcontractorName,
    IReadOnlyList<MatrixStepDto> Steps);

public sealed record SetMatrixRequest(IReadOnlyList<MatrixStepInput> Steps);
public sealed record MatrixStepInput(int OrderNo, Guid UserId);
