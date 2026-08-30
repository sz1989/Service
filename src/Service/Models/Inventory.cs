using System.Text.Json;

namespace Service.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // This property will map to the JSONB column
    public ProductAttributes Attributes { get; set; } = null!;
}

// The structural blueprint of your JSON column
public class ProductAttributes
{
    public string Brand { get; set; } = null!;
    public string Category { get; set; } = null!;
    public TechnicalSpecs Specs { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public bool InStock { get; set; }
}

public class TechnicalSpecs
{
    public int? RamGb { get; set; }
    public int? StorageGb { get; set; }
    public string? Cpu { get; set; }
    public int? CapacityLiters { get; set; }
}
