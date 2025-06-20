namespace API.Models.DTOs.Webhook;

public class EmailMessage
{
    public string Id { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? From { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<string> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Priority { get; set; } = 5;
    public string? CorrelationId { get; set; }
}

public class ReportConfig
{
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? OutputFormat { get; set; } = "PDF";
    public string? Template { get; set; }
}