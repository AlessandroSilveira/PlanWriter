using System;
using System.IO;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Domain.Interfaces.Services;

public interface IWordCountService
{
    ValidationResultDto FromText(string text, int? goal, Guid? projectId);
    ValidationResultDto FromDocx(Stream fileStream, int? goal, Guid? projectId);
    ValidationResultDto FromPlainFile(Stream fileStream, int? goal, Guid? projectId); // txt / md
}
