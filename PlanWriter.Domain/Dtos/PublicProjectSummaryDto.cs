using System;

namespace PlanWriter.Domain.Dtos;

public record PublicProjectSummaryDto(
    Guid ProjectId,
    string Title,
    int CurrentWords,
    int? WordGoal,
    int? EventPercent,           // se inscrito no evento ativo
    int? EventTotalWritten,      // palavras no evento
    int? EventTargetWords,       // meta do evento
    string? ActiveEventName      // nome do evento ativo, se houver
);