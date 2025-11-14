using System;
using System.Threading;
using System.Threading.Channels;
using FunctionalExtensions;
using FunctionalExtensions.Effects;

namespace FunctionalExtensions.CrmSample.Infrastructure.Notifications;

public sealed record CrmNotification(string Message, NotificationKind Kind, DateTimeOffset Timestamp)
{
    public static CrmNotification Info(string message)
        => new(message, NotificationKind.Info, DateTimeOffset.UtcNow);

    public static CrmNotification Success(string message)
        => new(message, NotificationKind.Success, DateTimeOffset.UtcNow);

    public static CrmNotification Error(string message)
        => new(message, NotificationKind.Error, DateTimeOffset.UtcNow);
}

public enum NotificationKind
{
    Info,
    Success,
    Error
}

public sealed class NotificationHub : IAsyncDisposable
{
    private readonly Channel<CrmNotification> _channel = Channel.CreateUnbounded<CrmNotification>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public ChannelReader<CrmNotification> Reader => _channel.Reader;

    public TaskResult<Unit> PublishAsync(CrmNotification notification, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteTaskResult(notification, cancellationToken);

    public TaskResult<Unit> CompleteAsync()
        => _channel.Writer.CompleteTaskResult();

    public ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
