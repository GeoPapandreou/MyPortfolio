namespace MyPortfolioAPI.Models;

public sealed class WorkSample
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LiveUrl { get; set; } = string.Empty;

    public Portfolio? Portfolio { get; set; }

    public List<WorkSampleTool> Tools { get; set; } = new();
}
