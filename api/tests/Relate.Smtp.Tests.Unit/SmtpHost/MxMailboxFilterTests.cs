using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.SmtpHost;
using Relate.Smtp.SmtpHost.Handlers;
using SmtpServer;
using SmtpServer.Mail;

namespace Relate.Smtp.Tests.Unit.SmtpHost;

[Trait("Category", "Unit")]
[Trait("Protocol", "SMTP")]
public class MxMailboxFilterTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<ILogger<MxMailboxFilter>> _loggerMock;
    private const int MxPort = 25;
    private const int SubmissionPort = 587;

    public MxMailboxFilterTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<MxMailboxFilter>>();

        // Setup service provider chain
        var scopedServiceProvider = new Mock<IServiceProvider>();
        scopedServiceProvider.Setup(s => s.GetService(typeof(IUserRepository)))
            .Returns(_userRepositoryMock.Object);

        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProvider.Object);
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
    }

    private MxMailboxFilter CreateFilter(bool mxEnabled = true, string[]? hostedDomains = null, bool validateRecipients = true)
    {
        var options = new SmtpServerOptions
        {
            Port = SubmissionPort,
            SecurePort = 465,
            Mx = new MxEndpointOptions
            {
                Enabled = mxEnabled,
                Port = MxPort,
                HostedDomains = hostedDomains ?? ["example.com", "mail.example.com"],
                ValidateRecipients = validateRecipients
            }
        };

        return new MxMailboxFilter(_serviceProviderMock.Object, _loggerMock.Object, options);
    }

    private static Mock<ISessionContext> CreateMockSessionContext(int port, bool authenticated = false)
    {
        var contextMock = new Mock<ISessionContext>();
        var properties = new Dictionary<string, object>();

        if (authenticated)
        {
            properties["AuthenticatedUserId"] = Guid.NewGuid();
            properties["AuthenticatedEmail"] = "user@example.com";
        }

        contextMock.Setup(c => c.Properties).Returns(properties);

        // Mock EndpointDefinition to return the specified port
        var endpointDefMock = new Mock<IEndpointDefinition>();
        endpointDefMock.Setup(e => e.Endpoint).Returns(new IPEndPoint(IPAddress.Loopback, port));
        contextMock.Setup(c => c.EndpointDefinition).Returns(endpointDefMock.Object);

        return contextMock;
    }

    private static IMailbox CreateMailbox(string user, string host)
    {
        return new Mailbox(user, host);
    }

    // ========== CanAcceptFromAsync Tests ==========

    [Fact]
    public async Task CanAcceptFromAsync_MxDisabled_AcceptsAll()
    {
        // Arrange
        var filter = CreateFilter(mxEnabled: false);
        var context = CreateMockSessionContext(SubmissionPort);
        var from = CreateMailbox("sender", "external.com");

        // Act
        var result = await filter.CanAcceptFromAsync(context.Object, from, 0, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAcceptFromAsync_AuthenticatedOnSubmissionPort_Accepts()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateMockSessionContext(SubmissionPort, authenticated: true);
        var from = CreateMailbox("sender", "external.com");

        // Act
        var result = await filter.CanAcceptFromAsync(context.Object, from, 0, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAcceptFromAsync_UnauthenticatedOnMxPort_Accepts()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");

        // Act
        var result = await filter.CanAcceptFromAsync(context.Object, from, 0, CancellationToken.None);

        // Assert — MX port accepts any sender (validation happens on RCPT TO)
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAcceptFromAsync_UnauthenticatedOnSubmissionPort_Rejects()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateMockSessionContext(SubmissionPort, authenticated: false);
        var from = CreateMailbox("sender", "external.com");

        // Act
        var result = await filter.CanAcceptFromAsync(context.Object, from, 0, CancellationToken.None);

        // Assert — Submission port requires authentication
        result.ShouldBeFalse();
    }

    // ========== CanDeliverToAsync Tests — Open Relay Prevention ==========

    [Fact]
    public async Task CanDeliverToAsync_MxPort_HostedDomain_Accepts()
    {
        // Arrange
        var filter = CreateFilter(validateRecipients: false);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("recipient", "example.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxPort_NonHostedDomain_Rejects()
    {
        // Arrange — This is the critical open relay prevention test
        var filter = CreateFilter();
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("spammer", "evil.com");
        var to = CreateMailbox("victim", "other-server.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — MUST reject: this would be an open relay
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxPort_HostedDomainCaseInsensitive_Accepts()
    {
        // Arrange
        var filter = CreateFilter(validateRecipients: false);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("recipient", "EXAMPLE.COM");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — Domain matching should be case-insensitive
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxPort_SecondHostedDomain_Accepts()
    {
        // Arrange
        var filter = CreateFilter(validateRecipients: false);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("recipient", "mail.example.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    // ========== CanDeliverToAsync Tests — Recipient Validation ==========

    [Fact]
    public async Task CanDeliverToAsync_MxPort_ValidateRecipients_KnownUser_Accepts()
    {
        // Arrange
        var filter = CreateFilter(validateRecipients: true);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("known", "example.com");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("known@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "known@example.com" });

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxPort_ValidateRecipients_UnknownUser_Rejects()
    {
        // Arrange
        var filter = CreateFilter(validateRecipients: true);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("unknown", "example.com");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — Unknown user at hosted domain should be rejected
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxPort_ValidateRecipientsDisabled_AcceptsUnknown()
    {
        // Arrange
        var filter = CreateFilter(validateRecipients: false);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("unknown", "example.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — With validation disabled, any address at hosted domain is accepted
        result.ShouldBeTrue();
        // Verify no DB call was made
        _userRepositoryMock.Verify(
            r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========== CanDeliverToAsync Tests — Authenticated Connections ==========

    [Fact]
    public async Task CanDeliverToAsync_Authenticated_AnyRecipient_Accepts()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateMockSessionContext(SubmissionPort, authenticated: true);
        var from = CreateMailbox("sender", "example.com");
        var to = CreateMailbox("recipient", "external-server.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — Authenticated users can send to any domain
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxDisabled_AcceptsAll()
    {
        // Arrange
        var filter = CreateFilter(mxEnabled: false);
        var context = CreateMockSessionContext(SubmissionPort);
        var from = CreateMailbox("sender", "example.com");
        var to = CreateMailbox("recipient", "external.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanDeliverToAsync_UnauthenticatedOnSubmissionPort_Rejects()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateMockSessionContext(SubmissionPort, authenticated: false);
        var from = CreateMailbox("sender", "example.com");
        var to = CreateMailbox("recipient", "example.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — Submission port requires authentication
        result.ShouldBeFalse();
    }

    // ========== Factory Tests ==========

    [Fact]
    public void CreateInstance_ReturnsSelf()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateMockSessionContext(MxPort);

        // Act
        var instance = filter.CreateInstance(context.Object);

        // Assert
        instance.ShouldBeSameAs(filter);
    }

    // ========== Edge Cases ==========

    [Fact]
    public async Task CanDeliverToAsync_MxPort_SubdomainNotInHostedDomains_Rejects()
    {
        // Arrange — "sub.example.com" is NOT in hosted domains (only "example.com" and "mail.example.com")
        var filter = CreateFilter(validateRecipients: false);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("recipient", "sub.example.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — Only exact domain matches are accepted
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeliverToAsync_MxPort_EmptyHostedDomains_RejectsAll()
    {
        // Arrange
        var filter = CreateFilter(hostedDomains: []);
        var context = CreateMockSessionContext(MxPort, authenticated: false);
        var from = CreateMailbox("sender", "gmail.com");
        var to = CreateMailbox("recipient", "example.com");

        // Act
        var result = await filter.CanDeliverToAsync(context.Object, to, from, CancellationToken.None);

        // Assert — No hosted domains means nothing is accepted
        result.ShouldBeFalse();
    }
}
