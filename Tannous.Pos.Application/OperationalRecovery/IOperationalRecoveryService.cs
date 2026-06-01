namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Operator recovery posture and stabilization outlook (advisory, process-local).</summary>
public interface IOperationalRecoveryService
{
    Task<OperationalRecoveryPostureDto> GetRecoveryPostureAsync(CancellationToken cancellationToken = default);

    Task<OperationalRecoveryOutlookDto> GetRecoveryOutlookAsync(CancellationToken cancellationToken = default);
}
