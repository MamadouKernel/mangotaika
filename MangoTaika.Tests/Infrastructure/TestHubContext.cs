using Microsoft.AspNetCore.SignalR;

namespace MangoTaika.Tests.Infrastructure;

public sealed class TestHubContext<THub> : IHubContext<THub> where THub : Hub
{
    public IHubClients Clients { get; } = new TestHubClients();
    public IGroupManager Groups { get; } = new TestGroupManager();

    public TestHubClients TypedClients => (TestHubClients)Clients;
}

public sealed class TestHubClients : IHubClients
{
    private readonly TestClientProxy _proxy = new();

    public IClientProxy All => _proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
    public IClientProxy Client(string connectionId) => _proxy;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
    public IClientProxy Group(string groupName) => _proxy;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
    public IClientProxy User(string userId) => _proxy;
    public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;

    public IReadOnlyList<(string Method, object?[] Args)> SentMessages => _proxy.SentMessages;
}

public sealed class TestClientProxy : IClientProxy
{
    private readonly List<(string Method, object?[] Args)> _sentMessages = [];

    public IReadOnlyList<(string Method, object?[] Args)> SentMessages => _sentMessages;

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        _sentMessages.Add((method, args));
        return Task.CompletedTask;
    }
}

public sealed class TestGroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
