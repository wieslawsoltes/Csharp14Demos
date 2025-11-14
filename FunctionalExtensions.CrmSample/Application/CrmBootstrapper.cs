using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.Computation;
using FunctionalExtensions.CrmSample.Infrastructure.Files;
using FunctionalExtensions.CrmSample.Infrastructure.Http;
using FunctionalExtensions.CrmSample.Infrastructure.Notifications;
using FunctionalExtensions.CrmSample.Infrastructure.Persistence;
using FunctionalExtensions.CrmSample.Infrastructure.Undo;

namespace FunctionalExtensions.CrmSample.Runtime;

public static class CrmBootstrapper
{
    public static CrmEnvironment BuildEnvironment()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDirectory = Path.Combine(appData, "FunctionalExtensions.CrmSample");
        Directory.CreateDirectory(dataDirectory);

        var database = new CrmDatabase(dataDirectory);
        var fileStore = new FileAttachmentStore(dataDirectory);
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
        };

        var enrichment = new CrmEnrichmentClient(httpClient);
        var notifications = new NotificationHub();
        var undo = new UndoStack();

        var initialization = TaskResults.Do.Run(
            (Func<TaskResultDoScope, Task<Unit>>)(async scope =>
            {
                await scope.Await(database.InitializeAsync());
                await scope.Await(fileStore.WarmupAsync());
                await scope.Await(notifications.PublishAsync(CrmNotification.Success("CRM workspace ready.")));
                return Unit.Value;
            }));

        var initResult = initialization.Invoke().GetAwaiter().GetResult();
        if (!initResult.IsSuccess)
        {
            throw new InvalidOperationException(initResult.Error ?? "Failed to bootstrap sample CRM.");
        }

        return new CrmEnvironment(
            dataDirectory,
            database,
            fileStore,
            enrichment,
            notifications,
            undo,
            httpClient);
    }
}
