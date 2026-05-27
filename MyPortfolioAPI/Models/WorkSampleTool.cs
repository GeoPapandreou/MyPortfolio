namespace MyPortfolioAPI.Models;

public sealed class WorkSampleTool
{
    public Guid Id { get; set; }

    public Guid WorkSampleId { get; set; }

    public string Tag { get; set; } = string.Empty;

    public WorkSample? WorkSample { get; set; }
}
