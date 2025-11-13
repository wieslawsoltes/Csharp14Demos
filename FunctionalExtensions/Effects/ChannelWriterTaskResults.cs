using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using FunctionalExtensions;

namespace FunctionalExtensions.Effects;

/// <summary>
/// Messaging helpers for turning <see cref="ChannelWriter{T}"/> operations into <see cref="TaskResult{T}"/> workflows.
/// </summary>
public static class ChannelWriterTaskResults
{
    extension<T>(ChannelWriter<T> writer)
    {
        /// <summary>
        /// Writes <paramref name="value"/> to the channel and wraps the outcome in a <see cref="TaskResult{T}"/>.
        /// </summary>
        public TaskResult<Unit> WriteTaskResult(T value, CancellationToken cancellationToken = default)
            => TaskResults.From(async () =>
            {
                try
                {
                    await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                    return Result<Unit>.Ok(Unit.Value);
                }
                catch (OperationCanceledException)
                {
                    return Result<Unit>.Fail("Channel write was cancelled.");
                }
                catch (Exception ex)
                {
                    return Result<Unit>.Fail(ex.Message);
                }
            });

        /// <summary>
        /// Attempts to complete the channel, producing a <see cref="TaskResult{T}"/> that indicates whether completion occurred.
        /// </summary>
        public TaskResult<Unit> CompleteTaskResult(Exception? error = null)
        {
            var completed = writer.TryComplete(error);
            return TaskResults.FromResult(completed
                ? Result<Unit>.Ok(Unit.Value)
                : Result<Unit>.Fail("Channel writer was already completed."));
        }

        /// <summary>
        /// Lifts a channel write into a <see cref="ReaderTaskResult{TEnv, TValue}"/> that treats the writer as the environment.
        /// </summary>
        public ReaderTaskResult<ChannelWriter<T>, Unit> ToReaderTaskResult(T value, CancellationToken cancellationToken = default)
            => ReaderTaskResults.From<ChannelWriter<T>, Unit>(w => w.WriteTaskResult(value, cancellationToken));
    }
}
