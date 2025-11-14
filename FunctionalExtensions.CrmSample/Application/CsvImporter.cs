using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Domain;
using FunctionalExtensions.Patterns;

namespace FunctionalExtensions.CrmSample.Runtime;

public static class CsvImporter
{
    public static TaskResult<IReadOnlyList<CustomerDraft>> ParseFileAsync(string path, CancellationToken cancellationToken = default)
        => TaskIO.From(async () =>
            {
                var drafts = new List<CustomerDraft>();
                var directory = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory;

                foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var cells = line.Split(';');
                    if (cells.Length < 4)
                    {
                        continue;
                    }

                    var scoreSpan = cells.Length > 4 ? cells[4].AsSpan() : ReadOnlySpan<char>.Empty;
                    var weightOption = scoreSpan.ToIntOption();

                    var attachmentDrafts = weightOption.Some(out var weight) && weight > 5
                        ? ImmutableArray.Create(CreateAttachmentDraft(directory, cells[0], weight))
                        : ImmutableArray<AttachmentDraft>.Empty;

                    var draft = new CustomerDraft(
                        Option<CustomerId>.None,
                        cells[0].Trim(),
                        cells[1].Trim(),
                        Option<string>.None,
                        cells[2].Trim(),
                        cells.Length > 5 ? cells[5] : string.Empty,
                        null,
                        cells[3].Trim(),
                        "Imported",
                        "00000",
                        Option<string>.None,
                        false,
                        attachmentDrafts);

                    drafts.Add(draft);
                }

                return (IReadOnlyList<CustomerDraft>)drafts;
            })
            .ToTaskResult(ex => $"Import failed: {ex.Message}");

    private static AttachmentDraft CreateAttachmentDraft(string directory, string name, int weight)
    {
        var safeFileName = $"{Sanitize(name)}-{weight}.txt";
        var physicalPath = Path.Combine(directory, safeFileName);
        File.WriteAllText(physicalPath, $"Auto-generated note for {name} with weight {weight}.");
        return new AttachmentDraft(safeFileName, physicalPath);
    }

    private static string Sanitize(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '-');
        }

        return value;
    }
}
