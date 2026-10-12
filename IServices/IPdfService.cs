namespace ReScanny.IServices;

public interface IPdfService
{
	Task<string> CreatePdfAsync(IReadOnlyList<(string Path, int Id)> items);
}
