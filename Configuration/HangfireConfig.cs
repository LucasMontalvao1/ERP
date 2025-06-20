namespace API.Configuration;

public class HangfireConfig
{
    public int WorkerCount { get; set; } = Environment.ProcessorCount;
    public List<string> Queues { get; set; } = new() { "default", "critical", "emails", "reports" };
    public string DashboardPath { get; set; } = "/hangfire";
    public bool RequireAuth { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "password";
}