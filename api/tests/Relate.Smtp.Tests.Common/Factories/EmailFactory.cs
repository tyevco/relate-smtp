using Bogus;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Tests.Common.Factories;

/// <summary>
/// Factory for generating test Email entities with realistic fake data.
/// </summary>
public class EmailFactory
{
    private readonly Faker<Email> _faker;
    private int _counter;

    public EmailFactory()
    {
        _faker = new Faker<Email>()
            .RuleFor(e => e.Id, _ => Guid.NewGuid())
            .RuleFor(e => e.MessageId, f => $"<{Guid.NewGuid()}@{f.Internet.DomainName()}>")
            .RuleFor(e => e.FromAddress, f => f.Internet.Email().ToLowerInvariant())
            .RuleFor(e => e.FromDisplayName, f => f.Name.FullName())
            .RuleFor(e => e.Subject, f => f.Lorem.Sentence(3, 5))
            .RuleFor(e => e.TextBody, f => f.Lorem.Paragraphs(2))
            .RuleFor(e => e.HtmlBody, (f, e) => $"<html><body><p>{e.TextBody}</p></body></html>")
            .RuleFor(e => e.ReceivedAt, f => f.Date.RecentOffset(30).ToUniversalTime())
            .RuleFor(e => e.SizeBytes, f => f.Random.Long(1000, 100000));
    }

    /// <summary>
    /// Creates a new Email with random data.
    /// </summary>
    public Email Create()
    {
        return _faker.Generate();
    }

    /// <summary>
    /// Creates an email with a specific subject.
    /// </summary>
    public Email WithSubject(string subject)
    {
        var email = Create();
        email.Subject = subject;
        return email;
    }

    /// <summary>
    /// Creates an email from a specific address.
    /// </summary>
    public Email FromAddress(string address, string? displayName = null)
    {
        var email = Create();
        email.FromAddress = address.ToLowerInvariant();
        if (displayName != null)
        {
            email.FromDisplayName = displayName;
        }
        return email;
    }

    /// <summary>
    /// Creates an email with a predictable pattern for testing.
    /// </summary>
    public Email CreateSequential()
    {
        _counter++;
        var email = Create();
        email.Subject = $"Test Email {_counter}";
        email.FromAddress = $"sender{_counter}@test.local";
        email.MessageId = $"<test-{_counter}@test.local>";
        return email;
    }

    /// <summary>
    /// Creates multiple emails.
    /// </summary>
    public IReadOnlyList<Email> CreateMany(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Create())
            .ToList();
    }

    /// <summary>
    /// Adds a recipient to an email.
    /// </summary>
    public Email WithRecipient(
        Email email,
        string address,
        RecipientType type = RecipientType.To,
        Guid? userId = null,
        bool isRead = false)
    {
        email.Recipients.Add(new EmailRecipient
        {
            Id = Guid.NewGuid(),
            EmailId = email.Id,
            Address = address.ToLowerInvariant(),
            Type = type,
            UserId = userId,
            IsRead = isRead
        });
        return email;
    }

    /// <summary>
    /// Creates an email with a To recipient.
    /// </summary>
    public Email WithToRecipient(string address, Guid? userId = null)
    {
        var email = Create();
        return WithRecipient(email, address, RecipientType.To, userId);
    }

    /// <summary>
    /// Creates an email linked to a specific user as recipient.
    /// </summary>
    public Email ForUser(User user, bool isRead = false)
    {
        var email = Create();
        return WithRecipient(email, user.Email, RecipientType.To, user.Id, isRead);
    }

    /// <summary>
    /// Creates an email sent by a specific user (via authenticated SMTP).
    /// </summary>
    public Email SentByUser(User user)
    {
        var email = Create();
        email.FromAddress = user.Email;
        email.FromDisplayName = user.DisplayName;
        email.SentByUserId = user.Id;
        return email;
    }

    /// <summary>
    /// Adds an attachment to an email.
    /// </summary>
    public Email WithAttachment(
        Email email,
        string fileName = "document.pdf",
        string contentType = "application/pdf",
        byte[]? content = null)
    {
        if (content == null)
        {
            content = new byte[1024]; // 1KB dummy content
#pragma warning disable CA5394 // Do not use insecure randomness - test data only
            new Random().NextBytes(content);
#pragma warning restore CA5394
        }

        email.Attachments.Add(new EmailAttachment
        {
            Id = Guid.NewGuid(),
            EmailId = email.Id,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = content.Length,
            Content = content
        });
        return email;
    }

    /// <summary>
    /// Creates an email with threading information (reply to another email).
    /// </summary>
    public Email AsReplyTo(Email parentEmail)
    {
        var email = Create();
        email.Subject = $"Re: {parentEmail.Subject}";
        email.InReplyTo = parentEmail.MessageId;
        email.References = parentEmail.MessageId;
        email.ThreadId = parentEmail.ThreadId ?? parentEmail.Id;
        return email;
    }

    /// <summary>
    /// Creates a thread of emails (parent + replies).
    /// </summary>
    public IReadOnlyList<Email> CreateThread(int replyCount, User recipient)
    {
        var emails = new List<Email>();

        // Create parent email
        var parent = ForUser(recipient);
        parent.ThreadId = parent.Id;
        emails.Add(parent);

        // Create replies
        var current = parent;
        for (var i = 0; i < replyCount; i++)
        {
            var reply = AsReplyTo(current);
            WithRecipient(reply, recipient.Email, RecipientType.To, recipient.Id);
            emails.Add(reply);
            current = reply;
        }

        return emails;
    }
}

/// <summary>
/// Extension methods for EmailFactory.
/// </summary>
public static class EmailFactoryExtensions
{
    /// <summary>
    /// Adds an email to the database context and saves.
    /// </summary>
    public static async Task<Email> AddToDbAsync(
        this Email email,
        Infrastructure.Data.AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        context.Emails.Add(email);
        await context.SaveChangesAsync(cancellationToken);
        return email;
    }

    /// <summary>
    /// Adds multiple emails to the database context and saves.
    /// </summary>
    public static async Task<IReadOnlyList<Email>> AddToDbAsync(
        this IEnumerable<Email> emails,
        Infrastructure.Data.AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        var list = emails.ToList();
        context.Emails.AddRange(list);
        await context.SaveChangesAsync(cancellationToken);
        return list;
    }
}
