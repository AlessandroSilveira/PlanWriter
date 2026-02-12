using PlanWriter.Domain.Entities;

namespace PlanWriter.Tests.API.Integration;

public sealed class InMemoryProfileStore
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, User> _users = [];
    private readonly Dictionary<Guid, Project> _projects = [];

    public void Reset()
    {
        lock (_sync)
        {
            _users.Clear();
            _projects.Clear();
        }
    }

    public void SeedUser(User user) => UpsertUser(user);

    public void SeedProject(Project project) => UpsertProject(project);

    public bool EmailExists(string email)
    {
        lock (_sync)
        {
            return _users.Values.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public User? GetUserByEmail(string email)
    {
        lock (_sync)
        {
            var user = _users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            return user is null ? null : Clone(user);
        }
    }

    public User? GetUserById(Guid userId)
    {
        lock (_sync)
        {
            return _users.TryGetValue(userId, out var user) ? Clone(user) : null;
        }
    }

    public User? GetUserBySlug(string slug)
    {
        lock (_sync)
        {
            var user = _users.Values.FirstOrDefault(u => string.Equals(u.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return user is null ? null : Clone(user);
        }
    }

    public bool SlugExists(string slug, Guid exceptUserId)
    {
        lock (_sync)
        {
            return _users.Values.Any(u =>
                u.Id != exceptUserId &&
                string.Equals(u.Slug, slug, StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyList<User> GetUsersByIds(IEnumerable<Guid> ids)
    {
        var idsSet = ids.ToHashSet();
        lock (_sync)
        {
            return _users.Values
                .Where(u => idsSet.Contains(u.Id))
                .Select(Clone)
                .ToList();
        }
    }

    public void UpsertUser(User user)
    {
        lock (_sync)
        {
            _users[user.Id] = Clone(user);
        }
    }

    public Project? GetProjectById(Guid projectId)
    {
        lock (_sync)
        {
            return _projects.TryGetValue(projectId, out var project) ? Clone(project) : null;
        }
    }

    public IReadOnlyList<Project> GetAllProjects()
    {
        lock (_sync)
        {
            return _projects.Values.Select(Clone).ToList();
        }
    }

    public IReadOnlyList<Project> GetProjectsByUser(Guid userId)
    {
        lock (_sync)
        {
            return _projects.Values
                .Where(p => p.UserId == userId)
                .Select(Clone)
                .ToList();
        }
    }

    public IReadOnlyList<Project> GetPublicProjectsByUser(Guid userId)
    {
        lock (_sync)
        {
            return _projects.Values
                .Where(p => p.UserId == userId && p.IsPublic)
                .Select(Clone)
                .ToList();
        }
    }

    public bool DeleteProject(Guid projectId, Guid userId)
    {
        lock (_sync)
        {
            return _projects.TryGetValue(projectId, out var project) &&
                   project.UserId == userId &&
                   _projects.Remove(projectId);
        }
    }

    public bool SetProjectVisibility(Guid projectId, Guid userId, bool isPublic)
    {
        lock (_sync)
        {
            if (!_projects.TryGetValue(projectId, out var project) || project.UserId != userId)
            {
                return false;
            }

            project.IsPublic = isPublic;
            return true;
        }
    }

    public void UpsertProject(Project project)
    {
        lock (_sync)
        {
            _projects[project.Id] = Clone(project);
        }
    }

    private static User Clone(User user)
    {
        return new User
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            IsProfilePublic = user.IsProfilePublic,
            Slug = user.Slug,
            DisplayName = user.DisplayName
        };
    }

    private static Project Clone(Project project)
    {
        return new Project
        {
            Id = project.Id,
            UserId = project.UserId,
            Title = project.Title,
            Genre = project.Genre,
            Description = project.Description,
            GoalAmount = project.GoalAmount,
            GoalUnit = project.GoalUnit,
            WordCountGoal = project.WordCountGoal,
            CreatedAt = project.CreatedAt,
            StartDate = project.StartDate,
            Deadline = project.Deadline,
            CurrentWordCount = project.CurrentWordCount,
            IsPublic = project.IsPublic,
            ValidatedWords = project.ValidatedWords,
            ValidatedAtUtc = project.ValidatedAtUtc,
            ValidationPassed = project.ValidationPassed,
            CoverBytes = project.CoverBytes,
            CoverMime = project.CoverMime,
            CoverSize = project.CoverSize,
            CoverUpdatedAt = project.CoverUpdatedAt
        };
    }
}
