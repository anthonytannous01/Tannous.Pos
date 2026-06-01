namespace Tannous.Pos.Application.Sync;

/// <summary>
/// Per-request scope for correlating durable replay short-circuit with sync push classification (not exposed to clients).
/// </summary>
public interface ISyncPushOperationExecutionScope
{
  bool ConsumeReplayShortCircuited();

  void MarkReplayShortCircuited();
}
