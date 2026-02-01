using Bogus;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Tests.Common.Factories;

/// <summary>
/// Factory for generating test Label entities.
/// </summary>
public class LabelFactory
{
    private static readonly string[] DefaultColors =
    [
        "#3b82f6", // Blue
        "#ef4444", // Red
        "#22c55e", // Green
        "#f59e0b", // Amber
        "#8b5cf6", // Purple
        "#06b6d4", // Cyan
        "#ec4899", // Pink
        "#6b7280"  // Gray
    ];

    private readonly Faker<Label> _faker;
    private int _counter;

    public LabelFactory()
    {
        _faker = new Faker<Label>()
            .RuleFor(l => l.Id, _ => Guid.NewGuid())
            .RuleFor(l => l.Name, f => f.Commerce.Department())
            .RuleFor(l => l.Color, f => f.PickRandom(DefaultColors))
            .RuleFor(l => l.SortOrder, f => f.Random.Int(0, 100))
            .RuleFor(l => l.CreatedAt, f => f.Date.PastOffset(1).ToUniversalTime());
    }

    /// <summary>
    /// Creates a new Label with random data.
    /// </summary>
    public Label Create(Guid userId)
    {
        var label = _faker.Generate();
        label.UserId = userId;
        return label;
    }

    /// <summary>
    /// Creates a label with a specific name.
    /// </summary>
    public Label WithName(Guid userId, string name, string? color = null)
    {
        var label = Create(userId);
        label.Name = name;
        if (color != null)
        {
            label.Color = color;
        }
        return label;
    }

    /// <summary>
    /// Creates a label with predictable properties.
    /// </summary>
    public Label CreateSequential(Guid userId)
    {
        _counter++;
        var label = Create(userId);
        label.Name = $"Label {_counter}";
        label.SortOrder = _counter;
        return label;
    }

    /// <summary>
    /// Creates common label set (Inbox, Work, Personal, etc.)
    /// </summary>
    public IReadOnlyList<Label> CreateCommonLabels(Guid userId)
    {
        return
        [
            WithName(userId, "Work", "#3b82f6"),
            WithName(userId, "Personal", "#22c55e"),
            WithName(userId, "Important", "#ef4444"),
            WithName(userId, "Follow-up", "#f59e0b"),
            WithName(userId, "Archive", "#6b7280")
        ];
    }

    /// <summary>
    /// Creates multiple labels.
    /// </summary>
    public IReadOnlyList<Label> CreateMany(Guid userId, int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Create(userId))
            .ToList();
    }
}

/// <summary>
/// Extension methods for LabelFactory.
/// </summary>
public static class LabelFactoryExtensions
{
    /// <summary>
    /// Adds a label to the database context and saves.
    /// </summary>
    public static async Task<Label> AddToDbAsync(
        this Label label,
        Infrastructure.Data.AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        context.Labels.Add(label);
        await context.SaveChangesAsync(cancellationToken);
        return label;
    }
}
