namespace IO.Cass.Models;

/// <summary>
/// Carrier validity request matching Python model
/// </summary>
public class CarrierValidityRequest
{
    public string? DotNumber { get; set; }
    public string? McNumber { get; set; }
    public string? BrokerageOrderId { get; set; }
}

/// <summary>
/// Carrier contact information matching Python model
/// </summary>
public class CarrierContact
{
    public string? Name { get; set; }
    public List<string> EmailAddresses { get; set; } = new();
    public List<string> Phones { get; set; } = new();
    public string? IsType { get; set; }
}

/// <summary>
/// Carrier validity error matching Python model
/// </summary>
public class CarrierValidityError
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Classification { get; set; }
}

/// <summary>
/// Carrier validity response matching Python model exactly
/// </summary>
public class CarrierValidityResponse
{
    public string IsValid { get; set; } = "false";
    public List<CarrierValidityError> Errors { get; set; } = new();
    public List<string> FailedBy { get; set; } = new();
    public object StatusCode { get; set; } = 200;
    public List<CarrierContact> Contacts { get; set; } = new();
}
