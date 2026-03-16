namespace LogiDocs.Api.Security;

public static class ApiRoles
{
    public const string Shipper = "Shipper";
    public const string Carrier = "Carrier";
    public const string CustomsBroker = "CustomsBroker";
    public const string CustomsAuthority = "CustomsAuthority";
    public const string Administrator = "Administrator";

    public const string AllOperational =
        Shipper + "," + Carrier + "," + CustomsBroker + "," + CustomsAuthority + "," + Administrator;

    public const string CreateTransport =
        Shipper + "," + Administrator;

    public const string UploadDocuments =
    Shipper + "," + Carrier + "," + Administrator;

    public const string ReviewDocuments =
        CustomsBroker + "," + CustomsAuthority + "," + Administrator;

    public const string ValidateDocuments =
        CustomsAuthority + "," + Administrator;

    public const string RegisterOnChain =
        CustomsAuthority + "," + Administrator;
}