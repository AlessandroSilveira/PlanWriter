using MediatR;
using PlanWriter.Domain.Dtos;

namespace PlanWriter.Application.Profile.Dtos.Queries;

public record GetPublicProfileQuery(string Slug) : IRequest<PublicProfileDto>;

    
