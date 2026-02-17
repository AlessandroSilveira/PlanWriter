using System;
using MediatR;
using PlanWriter.Domain.Dtos.Reports;

namespace PlanWriter.Application.Reports.Dtos.Queries;

public sealed record GetWritingReportQuery(
    Guid UserId,
    WritingReportPeriod Period,
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? ProjectId) : IRequest<WritingReportDto>;
