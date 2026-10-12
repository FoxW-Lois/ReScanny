namespace ReScanny.IServices;

public interface ICounterService
{
	int LastId { get; }

	int NextId { get; }

	void Advance(int count);

	void Reset();
}
