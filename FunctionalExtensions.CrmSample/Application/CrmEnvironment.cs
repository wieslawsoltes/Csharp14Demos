using System;
using System.Net.Http;
using FunctionalExtensions.CrmSample.Infrastructure.Files;
using FunctionalExtensions.CrmSample.Infrastructure.Http;
using FunctionalExtensions.CrmSample.Infrastructure.Notifications;
using FunctionalExtensions.CrmSample.Infrastructure.Persistence;
using FunctionalExtensions.CrmSample.Infrastructure.Undo;
using Microsoft.Data.Sqlite;

namespace FunctionalExtensions.CrmSample.Runtime;

public sealed class CrmEnvironment : IAsyncDisposable
{
    public string DataDirectory { get; }
    public CrmDatabase Database { get; }
    public FileAttachmentStore FileStore { get; }
    public CrmEnrichmentClient EnrichmentClient { get; }
    public NotificationHub Notifications { get; }
    public UndoStack Undo { get; }
    public HttpClient HttpClient { get; }
    public Func<SqliteConnection> ConnectionFactory { get; }

    public CrmEnvironment(
        string dataDirectory,
        CrmDatabase database,
        FileAttachmentStore fileStore,
        CrmEnrichmentClient enrichmentClient,
        NotificationHub notifications,
        UndoStack undo,
        HttpClient httpClient)
    {
        DataDirectory = dataDirectory;
        Database = database;
        FileStore = fileStore;
        EnrichmentClient = enrichmentClient;
        Notifications = notifications;
        Undo = undo;
        HttpClient = httpClient;
        ConnectionFactory = database.CreateConnection;
    }

    public async ValueTask DisposeAsync()
    {
        await Notifications.DisposeAsync();
        HttpClient.Dispose();
    }
}
