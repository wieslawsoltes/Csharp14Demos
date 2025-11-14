using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FunctionalExtensions;
using FunctionalExtensions.Effects;

namespace FunctionalExtensions.CrmSample.Infrastructure.Http;

public sealed class CrmEnrichmentClient
{
    private readonly HttpClient _httpClient;

    public CrmEnrichmentClient(HttpClient httpClient)
        => _httpClient = httpClient;

    public TaskResult<IReadOnlyList<RemoteCompany>> DownloadCompaniesAsync(CancellationToken cancellationToken = default)
        => _httpClient
            .GetJsonTaskResult<List<RemoteCompanyDto>>("users", cancellationToken)
            .Map(dtos =>
            {
                var items = dtos
                    .Select(dto => new RemoteCompany(
                        dto.Id,
                        dto.Name,
                        dto.Email,
                        dto.Phone,
                        dto.Company?.Name ?? "Unknown"))
                    .ToList();

                return (IReadOnlyList<RemoteCompany>)items;
            });

    private sealed record RemoteCompanyDto(int Id, string Name, string Email, string Phone, CompanyDto? Company);
    private sealed record CompanyDto(string Name);
}

public sealed record RemoteCompany(int Id, string Name, string Email, string Phone, string CompanyName);
