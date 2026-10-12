namespace ReScanny.IServices;

public interface IPhotoStorageService
{
	Task<string> SaveAsync(byte[] jpegBytes);

	void Delete(string path);

	void Clear();

	IReadOnlyList<string> GetAll();
}
