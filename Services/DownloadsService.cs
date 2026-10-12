using ReScanny.IServices;

namespace ReScanny.Services;

public class DownloadsService : IDownloadsService
{
	public Task<bool> CopyPdfAsync(string sourcePath)
	{
#if ANDROID
		return Task.Run(() =>
		{
			try
			{
				var fileName = Path.GetFileName(sourcePath);

				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
				{
					// Android 10+ : MediaStore, aucune permission nécessaire.
					var resolver = Android.App.Application.Context.ContentResolver!;

					var values = new Android.Content.ContentValues();
					values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
					values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "application/pdf");
					values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

					var uri = resolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri!, values);
					if (uri is null)
						return false;

					using var output = resolver.OpenOutputStream(uri);
					if (output is null)
						return false;

					using var input = File.OpenRead(sourcePath);
					input.CopyTo(output);
					return true;
				}
				else
				{
					// Android 9 et moins : écriture directe (permission WRITE_EXTERNAL_STORAGE).
					var dir = Android.OS.Environment
						.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)!
						.AbsolutePath;

					File.Copy(sourcePath, Path.Combine(dir, fileName), overwrite: true);
					return true;
				}
			}
			catch
			{
				return false;
			}
		});
#else
        return Task.FromResult(false);
#endif
	}
}
