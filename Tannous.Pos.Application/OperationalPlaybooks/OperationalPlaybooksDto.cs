namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Bounded operational playbooks, response steps, and escalation guidance.</summary>
public sealed class OperationalPlaybooksDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int PlaybookCount { get; init; }
    public int ResponseStepCount { get; init; }
    public int EscalationGuidanceCount { get; init; }
    public IReadOnlyList<OperationalPlaybookDto> Playbooks { get; init; } = Array.Empty<OperationalPlaybookDto>();
    public IReadOnlyList<OperationalResponseStepDto> ResponseSteps { get; init; } = Array.Empty<OperationalResponseStepDto>();
    public IReadOnlyList<OperationalEscalationGuidanceDto> EscalationGuidance { get; init; } = Array.Empty<OperationalEscalationGuidanceDto>();
    public OperationalResponseAlignmentDto ResponseAlignment { get; init; } = new();
    public string PlaybookNote { get; init; } =
        "Advisory deterministic operational response guidance composed from existing diagnostics. Sequencing only — not workflow execution, automation, or remediation.";
}
