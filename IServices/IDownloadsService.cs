namespace ReScanny.IServices;

public interface IDownloadsService
{
	Task<bool> CopyPdfAsync(string sourcePath);
}
