namespace IO.Common.Models;

/// <summary>
/// Location information matching Python model
/// </summary>
public class Location
{
    public string? Company { get; set; }
    public string? Number { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Weight { get; set; }
}

/// <summary>
/// Fleet information matching Python model
/// </summary>
public class Fleet
{
    public string Manager { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string ServiceCenter { get; set; } = string.Empty;
}

/// <summary>
/// Training information matching Python model
/// </summary>
public class Training
{
    public string Coordinator { get; set; } = string.Empty;
    public string CoordinatorSupervisor { get; set; } = string.Empty;
}

/// <summary>
/// Order information matching Python model
/// </summary>
public class Order
{
    public int? Number { get; set; }
    public string? Sbu { get; set; }
    public string? Terminal { get; set; }
}

/// <summary>
/// Driver information matching Python model
/// </summary>
public class Driver
{
    public string? Id { get; set; }
    public string? Company { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sbu { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string JobDesc { get; set; } = string.Empty;
    public string Persona { get; set; } = "driver";
    public Location? Truck { get; set; }
    public Location? Trailer { get; set; }
    public List<string> VendorServices { get; set; } = new();
    public Fleet? Fleet { get; set; }
    public Training? Training { get; set; }
    public string? StateZone { get; set; }
    public Order? Order { get; set; }
    public string? PrimaryCoverage { get; set; }
    public string? DomicileTerminal { get; set; }
    public string? CurrentPTA { get; set; }
    public string? PreferredLanguage { get; set; }
}

/// <summary>
/// Context response matching Python model
/// </summary>
public class ContextResponse
{
    public Driver? Driver { get; set; }
}

/// <summary>
/// Genesys driver response matching Python model
/// </summary>
public class GenesysDriverResponse
{
    public object? Driver { get; set; }
    public object? Call { get; set; }
}

/// <summary>
/// Context request matching Python model
/// </summary>
public class ContextRequest
{
    public string Id { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}
