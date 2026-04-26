using System.Threading.Tasks;

public interface IExportService
{
    Task<string> ExportToMarkdownAsync(string sessionId);
    Task<string> ExportToPlainTextAsync(string sessionId);
    Task SaveToFileAsync(string content, string filename);
    /// <summary>
    /// Exports the session as an Obsidian-compatible markdown note into the configured vault path.
    /// Returns the full file path written.
    /// </summary>
    Task<string> ExportToObsidianVaultAsync(string sessionId);
}
