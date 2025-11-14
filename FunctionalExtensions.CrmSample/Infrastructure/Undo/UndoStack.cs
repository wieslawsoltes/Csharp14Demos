using System;
using System.Collections.Generic;
using FunctionalExtensions;

namespace FunctionalExtensions.CrmSample.Infrastructure.Undo;

/// <summary>
/// Very small undo manager that uses the Continuation helpers to model replay semantics.
/// </summary>
public sealed class UndoStack
{
    private readonly Stack<IO<Unit>> _undoEffects = new();

    public void Push(IO<Unit> effect)
        => _undoEffects.Push(effect);

    public Cont<Unit, Unit> ReplayContinuations()
        => Continuation.CallCC<Unit, Unit>(escape =>
            Continuation.From<Unit, Unit>(continuation =>
            {
                while (_undoEffects.Count > 0)
                {
                    var effect = _undoEffects.Pop();
                    var result = Try.Run(effect.Invoke);
                    if (!result.IsSuccess)
                    {
                        return escape(Unit.Value).Run(continuation);
                    }
                }

                return continuation(Unit.Value);
            }));

    public Result<Unit> RunUndo()
    {
        try
        {
            ReplayContinuations().Invoke(static _ => Unit.Value);
            return Result<Unit>.Ok(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail(ex.Message);
        }
    }
}
