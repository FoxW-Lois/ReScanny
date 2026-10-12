using ReScanny.IServices;

namespace ReScanny.Services;

public class CounterService : ICounterService
{
	private const string LastIdKey = "last_object_id";

	public int LastId => Preferences.Default.Get(LastIdKey, 0);

	public int NextId => LastId + 1;

	public void Advance(int count) // count : Nombre de photos prises depuis le dernier ID
	{
		if (count <= 0) return;
		Preferences.Default.Set(LastIdKey, LastId + count);
	}

	public void Reset() => Preferences.Default.Set(LastIdKey, 0);
}
