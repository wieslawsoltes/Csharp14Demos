using System;
using FunctionalExtensions.CrmSample.Infrastructure.Notifications;

namespace FunctionalExtensions.CrmSample.ViewModels;

public sealed class NotificationViewModel : ViewModelBase
{
    public NotificationViewModel(CrmNotification notification)
        => Notification = notification;

    public CrmNotification Notification { get; }

    public string Message => Notification.Message;
    public NotificationKind Kind => Notification.Kind;
    public DateTimeOffset Timestamp => Notification.Timestamp;
}
