using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Runtime;
using FunctionalExtensions.CrmSample.Domain;
using FunctionalExtensions.CrmSample.Infrastructure.Http;
using FunctionalExtensions.CrmSample.Infrastructure.Notifications;
using FunctionalExtensions.Patterns;
using FunctionalExtensions.TypeClasses;
using ReactiveUI;
using RxUnit = System.Reactive.Unit;
using NotificationKindAlias = FunctionalExtensions.CrmSample.Infrastructure.Notifications.NotificationKind;

namespace FunctionalExtensions.CrmSample.ViewModels;

public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    private static readonly Reader<CrmEnvironment, string> DataDirectoryReader = Reader.From<CrmEnvironment, string>(env => env.DataDirectory);

    private readonly CrmEnvironment _environment;
    private readonly ObservableCollection<CustomerViewModel> _customers = new();
    private readonly ObservableCollection<NotificationViewModel> _notifications = new();
    private readonly ObservableCollection<string> _remoteCompanies = new();
    private readonly List<Customer> _customersCache = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ChannelReader<CrmNotification> _notificationReader;

    private DashboardFilter _filter = DashboardFilter.Default;
    private CustomerViewModel? _selectedCustomer;
    private string _filterText = string.Empty;
    private bool _includeArchived;

    public MainViewModel(CrmEnvironment environment)
    {
        _environment = environment;
        _notificationReader = environment.Notifications.Reader;

        Editor = new CustomerDraftViewModel();

        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        ImportCommand = ReactiveCommand.CreateFromTask(ImportSampleAsync);
        ExportCommand = ReactiveCommand.CreateFromTask(ExportAttachmentsAsync);
        UndoCommand = ReactiveCommand.CreateFromTask(UndoAsync);
        NewDraftCommand = ReactiveCommand.Create(NewDraft);
        LoadDraftCommand = ReactiveCommand.CreateFromTask(LoadDraftAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask(DeleteSelectedAsync);

        _ = PumpNotificationsAsync(_cts.Token);
        _ = RefreshAsync();
    }

    public CustomerDraftViewModel Editor { get; }
    public ObservableCollection<CustomerViewModel> Customers => _customers;
    public ObservableCollection<NotificationViewModel> Notifications => _notifications;
    public ObservableCollection<string> RemoteCompanies => _remoteCompanies;

    public ReactiveCommand<RxUnit, RxUnit> RefreshCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SaveCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> ImportCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> ExportCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> UndoCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> NewDraftCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> LoadDraftCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> DeleteCommand { get; }

    public CustomerViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set => this.RaiseAndSetIfChanged(ref _selectedCustomer, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            this.RaiseAndSetIfChanged(ref _filterText, value);
            ApplyFilter();
        }
    }

    public bool IncludeArchived
    {
        get => _includeArchived;
        set
        {
            this.RaiseAndSetIfChanged(ref _includeArchived, value);
            ApplyFilter();
        }
    }

    public async Task RefreshAsync()
    {
        var cancellationToken = _cts.Token;
        var customersTask = CrmWorkflows.LoadCustomers(cancellationToken).Invoke(_environment).Invoke();
        var remoteTask = CrmWorkflows.DownloadRemoteCompanies(cancellationToken).Invoke(_environment).Invoke();

        var combined = customersTask.LiftA2(remoteTask, static (customers, companies) => (customers, companies));
        var (customersResult, companiesResult) = await combined.ConfigureAwait(false);

        if (customersResult.IsSuccess && customersResult.Value is not null)
        {
            _customersCache.Clear();
            _customersCache.AddRange(customersResult.Value);
            ApplyFilter();

            var firstActive = customersResult.Value
                .Filter(customer => !customer.IsArchived)
                .FirstOption();

            if (firstActive.Some(out var active))
            {
                SelectedCustomer = CustomerViewModel.FromModel(active);
            }
        }
        else
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(customersResult.Error ?? "Failed to load customers")).Invoke().ConfigureAwait(false);
        }

        if (companiesResult.IsSuccess && companiesResult.Value is not null)
        {
            UpdateCollection(_remoteCompanies, companiesResult.Value.FMap(company => $"{company.Name} ({company.CompanyName})"));
        }
    }

    public async Task SaveAsync()
    {
        var writerTask = new WriterTaskResult<Customer, CrmDomainEvent>(
            CrmWorkflows.SaveCustomer(Editor.ToDraft(), _cts.Token)
                .Invoke(_environment)
                .Map(writer => (writer.Value, writer.Logs)));

        var saveResult = await writerTask.Invoke().Invoke().ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(saveResult.Error ?? "Failed to save")).Invoke().ConfigureAwait(false);
            return;
        }

        var (customer, logs) = saveResult.Value;
        UpsertCustomer(customer);
        foreach (var log in logs)
        {
            _notifications.Insert(0, new NotificationViewModel(new CrmNotification(log.Description, NotificationKindAlias.Info, log.OccurredAt)));
        }

        Editor.Load(CustomerDraft.FromCustomer(customer));
    }

    private async Task ImportSampleAsync()
    {
        var samplePath = Path.Combine(DataDirectoryReader.Invoke(_environment), "sample-import.csv");
        if (!File.Exists(samplePath))
        {
            var sample = """
                Acme Corp;hello@acme.test;+1-555-0101;Seattle;8
                Contoso;contact@contoso.test;+1-555-0202;New York;5
                Fabrikam;hi@fabrikam.test;+1-555-0303;Oslo;7
                """;
            Directory.CreateDirectory(Path.GetDirectoryName(samplePath)!);
            await File.WriteAllTextAsync(samplePath, sample.Replace("\r", string.Empty)).ConfigureAwait(false);
        }

        var parsed = await CsvImporter.ParseFileAsync(samplePath, _cts.Token).Invoke().ConfigureAwait(false);
        if (!parsed.IsSuccess || parsed.Value is null)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(parsed.Error ?? "Import failed")).Invoke().ConfigureAwait(false);
            return;
        }

        foreach (var draft in parsed.Value)
        {
            var writerTask = new WriterTaskResult<Customer, CrmDomainEvent>(
                CrmWorkflows.SaveCustomer(draft, _cts.Token)
                    .Invoke(_environment)
                    .Map(writer => (writer.Value, writer.Logs)));

            var saveResult = await writerTask.Invoke().Invoke().ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                await _environment.Notifications.PublishAsync(CrmNotification.Error(saveResult.Error ?? "Import save failed")).Invoke().ConfigureAwait(false);
            }
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task ExportAttachmentsAsync()
    {
        var option = Option.FromNullable(SelectedCustomer);
        if (!option.HasValue)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error("Select a customer first.")).Invoke().ConfigureAwait(false);
            return;
        }

        var target = Path.Combine(DataDirectoryReader.Invoke(_environment), "exports", option.Value!.Model.Id.ToString());
        var exportResult = await _environment.FileStore.ExportAllAsync(target, option.Value!.Model.Attachments).Invoke().Invoke().ConfigureAwait(false);

        if (!exportResult.IsSuccess)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(exportResult.Error ?? "Export failed")).Invoke().ConfigureAwait(false);
            return;
        }

        await _environment.Notifications.PublishAsync(CrmNotification.Success($"Exported to {target}")).Invoke().ConfigureAwait(false);
    }

    private void NewDraft()
    {
        SelectedCustomer = null;
        Editor.Reset();
    }

    private async Task LoadDraftAsync()
    {
        var selected = SelectedCustomer;
        if (selected is null)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error("Select a customer to load.")).Invoke().ConfigureAwait(false);
            return;
        }

        var result = await CrmWorkflows.LoadDraft(selected.Model.Id, _cts.Token)
            .Invoke(_environment)
            .Invoke()
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(result.Error ?? "Failed to load draft")).Invoke().ConfigureAwait(false);
            return;
        }

        if (!result.Value.HasValue)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error("No draft available for the selected customer.")).Invoke().ConfigureAwait(false);
            return;
        }

        Editor.Load(result.Value.Value!);
    }

    private async Task DeleteSelectedAsync()
    {
        var selected = SelectedCustomer;
        if (selected is null)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error("Select a customer to delete.")).Invoke().ConfigureAwait(false);
            return;
        }

        var deleteResult = await CrmWorkflows.DeleteCustomer(selected.Model.Id, _cts.Token)
            .Invoke(_environment)
            .Invoke()
            .ConfigureAwait(false);

        if (!deleteResult.IsSuccess)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(deleteResult.Error ?? "Failed to delete customer")).Invoke().ConfigureAwait(false);
            return;
        }

        RemoveCustomer(selected.Model.Id);
        Editor.Reset();

        var writer = deleteResult.Value;
        foreach (var log in writer.Logs)
        {
            _notifications.Insert(0, new NotificationViewModel(new CrmNotification(log.Description, NotificationKindAlias.Info, log.OccurredAt)));
        }
    }

    private async Task UndoAsync()
    {
        var result = await CrmWorkflows.UndoLastOperation().Invoke(_environment).Invoke().ConfigureAwait(false);
        if (!result.IsSuccess || !result.Value.IsSuccess)
        {
            await _environment.Notifications.PublishAsync(CrmNotification.Error(result.Value.Error ?? "Nothing to undo")).Invoke().ConfigureAwait(false);
            return;
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task PumpNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var notification in _notificationReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                _notifications.Insert(0, new NotificationViewModel(notification));
                if (_notifications.Count > 25)
                {
                    _notifications.RemoveAt(_notifications.Count - 1);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpsertCustomer(Customer customer)
    {
        var existingIndex = _customersCache.FindIndex(c => c.Id == customer.Id);
        if (existingIndex >= 0)
        {
            _customersCache[existingIndex] = customer;
        }
        else
        {
            _customersCache.Add(customer);
        }

        ApplyFilter();

        SelectedCustomer = Customers.FirstOrDefault(vm => vm.Model.Id == customer.Id);
    }

    private void RemoveCustomer(CustomerId id)
    {
        var existingIndex = _customersCache.FindIndex(c => c.Id == id);
        if (existingIndex >= 0)
        {
            _customersCache.RemoveAt(existingIndex);
        }

        ApplyFilter();

        if (SelectedCustomer?.Model.Id == id)
        {
            SelectedCustomer = Customers.FirstOrDefault();
        }
    }

    private void ApplyFilter()
    {
        var state = DashboardState.FilterCustomers(_customersCache, FilterText, IncludeArchived);
        var (filtered, updated) = state.Invoke(_filter);
        _filter = updated;

        UpdateCollection(_customers, filtered.FMap(CustomerViewModel.FromModel));
    }

    private void UpdateCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}
