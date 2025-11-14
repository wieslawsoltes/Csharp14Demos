using System.Collections.Immutable;
using System.Text.Json;
using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Domain;

namespace FunctionalExtensions.CrmSample.Infrastructure.Files;

/// <summary>
/// Handles attachment import/export using TaskIO/Try combinators.
/// </summary>
public sealed class FileAttachmentStore
{
    private readonly string _attachmentsDirectory;
    private readonly string _auditFile;

    public FileAttachmentStore(string dataDirectory)
    {
        _attachmentsDirectory = Path.Combine(dataDirectory, "attachments");
        _auditFile = Path.Combine(dataDirectory, "audit.jsonl");
    }

    public TaskResult<Unit> WarmupAsync()
        => TaskIO.From(async () =>
            {
                Directory.CreateDirectory(_attachmentsDirectory);
                if (!File.Exists(_auditFile))
                {
                    await File.Create(_auditFile).DisposeAsync().ConfigureAwait(false);
                }

                return Unit.Value;
            })
            .ToTaskResult(ex => $"Failed to initialize attachment store: {ex.Message}");

    public TaskResult<CustomerAttachment> ImportAsync(AttachmentDraft draft, CancellationToken cancellationToken = default)
    {
        return TaskIO.From(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(draft.FileName)}";
                var destination = Path.Combine(_attachmentsDirectory, safeName);

                await using (var source = File.OpenRead(draft.SourcePath))
                await using (var target = File.Create(destination))
                {
                    await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
                }

                var info = new FileInfo(destination);
                var attachment = new CustomerAttachment(Guid.NewGuid(), draft.FileName, destination, info.Length, DateTimeOffset.UtcNow);

                await AppendAuditAsync(new
                {
                    kind = "import",
                    attachment.Id,
                    attachment.FileName,
                    attachment.Size,
                    attachment.AddedAt
                }).ConfigureAwait(false);

                return attachment;
            })
            .ToTaskResult(ex => $"Import failed: {ex.Message}");
    }

    public TaskResult<Unit> DeleteAsync(CustomerAttachment attachment)
    {
        return TaskIO.From(async () =>
            {
                var deletion = Try.Run(() =>
                {
                    if (File.Exists(attachment.PhysicalPath))
                    {
                        File.Delete(attachment.PhysicalPath);
                    }
                });

                if (!deletion.IsSuccess)
                {
                    throw deletion.Exception!;
                }

                await AppendAuditAsync(new
                {
                    kind = "delete",
                    attachment.Id,
                    attachment.FileName,
                    timestamp = DateTimeOffset.UtcNow
                }).ConfigureAwait(false);

                return Unit.Value;
            })
            .ToTaskResult(ex => $"Delete failed: {ex.Message}");
    }

    public IO<TaskResult<Unit>> ExportAllAsync(string destinationDirectory, IEnumerable<CustomerAttachment> attachments)
        => IO.From(() =>
            TaskIO.From(async () =>
            {
                Directory.CreateDirectory(destinationDirectory);
                var attachmentsList = attachments.ToImmutableArray();

                foreach (var attachment in attachmentsList)
                {
                    var target = Path.Combine(destinationDirectory, attachment.FileName);
                    await using var source = File.OpenRead(attachment.PhysicalPath);
                    await using var targetStream = File.Create(target);
                    await source.CopyToAsync(targetStream).ConfigureAwait(false);
                }

                await AppendAuditAsync(new
                {
                    kind = "export",
                    count = attachmentsList.Length,
                    destinationDirectory,
                    timestamp = DateTimeOffset.UtcNow
                }).ConfigureAwait(false);

                return Unit.Value;
            }).ToTaskResult(ex => $"Export failed: {ex.Message}"));

    private Task AppendAuditAsync(object payload)
    {
        return Task.Run(async () =>
        {
            var line = JsonSerializer.Serialize(payload);
            await File.AppendAllLinesAsync(_auditFile, new[] { line }).ConfigureAwait(false);
        });
    }
}
