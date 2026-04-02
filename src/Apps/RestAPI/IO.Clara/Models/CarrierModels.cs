namespace IO.Clara.Models;

/// <summary>
/// Carrier validity request
/// </summary>
public class CarrierValidityRequest
{
    public string? DotNumber { get; set; }
    public string? McNumber { get; set; }
    public string? BrokerageOrderId { get; set; }
}

/// <summary>
/// Carrier validity response
/// </summary>
public class CarrierValidityResponse
{
    public string IsValid { get; set; } = "false";
    public int StatusCode { get; set; } = 200;
    public List<CarrierValidityError> Errors { get; set; } =
    [
    ];
    public List<string> FailedBy { get; set; } =
    [
    ];
    public List<CarrierContact> Contacts { get; set; } =
    [
    ];
}

/// <summary>
/// Carrier validity error
/// </summary>
public class CarrierValidityError
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
}

/// <summary>
/// Carrier contact
/// </summary>
public class CarrierContact
{
    public string Name { get; set; } = string.Empty;
    public string IsType { get; set; } = string.Empty;
    public List<string> Phones { get; set; } =
    [
    ];
    public List<string> EmailAddresses { get; set; } =
    [
    ];
}
