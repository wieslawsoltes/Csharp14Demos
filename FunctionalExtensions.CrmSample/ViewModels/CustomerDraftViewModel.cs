using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Domain;
using ReactiveUI;

namespace FunctionalExtensions.CrmSample.ViewModels;

public sealed class CustomerDraftViewModel : ViewModelBase
{
    private CustomerDraft _draft = CustomerDraft.Empty;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private bool _isArchived;

    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            _draft = _draft with { Name = value };
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            this.RaiseAndSetIfChanged(ref _email, value);
            _draft = _draft with { Email = value };
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            this.RaiseAndSetIfChanged(ref _phone, value);
            _draft = _draft with { Phone = value };
        }
    }

    public bool IsArchived
    {
        get => _isArchived;
        set
        {
            this.RaiseAndSetIfChanged(ref _isArchived, value);
            _draft = _draft with { IsArchived = value };
        }
    }

    public void Load(CustomerDraft draft)
    {
        _draft = draft;
        _name = draft.Name;
        _email = draft.Email;
        _phone = draft.Phone;
        _isArchived = draft.IsArchived;

        this.RaisePropertyChanged(nameof(Name));
        this.RaisePropertyChanged(nameof(Email));
        this.RaisePropertyChanged(nameof(Phone));
        this.RaisePropertyChanged(nameof(IsArchived));
    }

    public void Reset()
        => Load(CustomerDraft.Empty);

    public CustomerDraft ToDraft()
        => _draft;
}
