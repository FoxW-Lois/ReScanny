using ReScanny.IServices;

namespace ReScanny.Services;

public class PhotoStorageService : IPhotoStorageService
{
	private readonly string _folder;

	public PhotoStorageService()
	{
		// AppDataDirectory plutôt que CacheDirectory : Android peut vider le cache à tout moment.
		_folder = Path.Combine(FileSystem.AppDataDirectory, "temp_photos");

		Directory.CreateDirectory(_folder);
	}

	public async Task<string> SaveAsync(byte[] jpegBytes)
	{
		var path = Path.Combine(_folder, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg");
		await File.WriteAllBytesAsync(path, jpegBytes);

		return path;
	}

	public void Delete(string path)
	{
		if (File.Exists(path))
			File.Delete(path);
	}

	public void Clear()
	{
		foreach (var file in Directory.GetFiles(_folder))
			File.Delete(file);
	}

	public IReadOnlyList<string> GetAll() =>
		[.. Directory.GetFiles(_folder, "*.jpg").OrderBy(f => f, StringComparer.Ordinal)];
}
