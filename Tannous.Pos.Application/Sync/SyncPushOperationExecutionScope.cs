namespace Tannous.Pos.Application.Sync;

public sealed class SyncPushOperationExecutionScope : ISyncPushOperationExecutionScope
{
    private bool _replayShortCircuited;

    public void MarkReplayShortCircuited() => _replayShortCircuited = true;

    public bool ConsumeReplayShortCircuited()
    {
        if (!_replayShortCircuited)
            return false;

        _replayShortCircuited = false;
        return true;
    }
}
