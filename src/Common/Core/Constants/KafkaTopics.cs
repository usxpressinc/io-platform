namespace IO.Core.Constants;

/// <summary>
/// Kafka topic name constants for event streaming
/// </summary>
public static class KafkaTopics
{
    // Authentication and token events
    public const string TokenGenerated = "io.token.generated";
    public const string TokenValidated = "io.token.validated";
    public const string TokenRevoked = "io.token.revoked";

    // Email service events
    public const string EmailSent = "io.email.sent";
    public const string EmailFailed = "io.email.failed";

    // Context service events
    public const string ContextUpdated = "io.context.updated";

    // Carrier events
    public const string CarrierVetted = "io.carrier.vetted";
    public const string CarrierRejected = "io.carrier.rejected";

    // Pricing events
    public const string PriceCalculated = "io.price.calculated";

    // Vendor events
    public const string VendorQueried = "io.vendor.queried";

    // Job events
    public const string JobSearched = "io.job.searched";

    // Audit events
    public const string AuditLogCreated = "io.audit.created";
}
