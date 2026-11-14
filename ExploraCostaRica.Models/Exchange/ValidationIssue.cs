namespace ExploraCostaRica.Models.Exchange;


public sealed record ValidationIssue(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}
