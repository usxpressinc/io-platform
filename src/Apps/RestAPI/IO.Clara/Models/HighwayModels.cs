namespace IO.Clara.Models;

/// <summary>
/// Simplified Highway carrier data matching Python structure
/// </summary>
public class HighwayCarrierData
{
    public HighwayConnection? Connection { get; set; }
    public HighwayContactInformation? ContactInformation { get; set; }
}

/// <summary>
/// Highway connection information
/// </summary>
public class HighwayConnection
{
    public string? Status { get; set; }
    public bool? IsMonitored { get; set; }
}

/// <summary>
/// Highway contact information
/// </summary>
public class HighwayContactInformation
{
    public HighwayDispatchContact? DispatchContact { get; set; }
    public List<HighwayLineItemContact> LineItemContacts { get; set; } =
    [
    ];
}

/// <summary>
/// Highway dispatch contact
/// </summary>
public class HighwayDispatchContact
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? EmailAddress { get; set; }
}

/// <summary>
/// Highway line item contact
/// </summary>
public class HighwayLineItemContact
{
    public string? IsType { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? EmailAddress { get; set; }
}
