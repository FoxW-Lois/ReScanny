namespace ReScanny.Models;

public class CapturedPhoto
{
	public required string Path { get; init; }

	// AssignedId ID est attribué uniquement dans la preview (null avant)
	public int? AssignedId { get; set; }
}
