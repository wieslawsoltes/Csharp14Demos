using FunctionalExtensions.CrmSample.Domain;

namespace FunctionalExtensions.CrmSample.ViewModels;

public sealed class CustomerViewModel : ViewModelBase
{
    public CustomerViewModel(Customer model)
        => Model = model;

    public Customer Model { get; }
    public string Name => Model.Name;
    public string Email => Model.Email;
    public string Phone => Model.Contact.Phone;
    public bool IsArchived => Model.IsArchived;
    public double Score => (double)Model.Score.Normalized;
    public double Momentum => Model.Score.Momentum;
    public string Location => Model.Contact.Address.HasValue ? Model.Contact.Address.Value!.ToString() : "—";
    public int AttachmentCount => Model.Attachments.Length;

    public static CustomerViewModel FromModel(Customer customer)
        => new(customer);
}
