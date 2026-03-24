namespace ACGCET_Faculty.Services;

/// <summary>
/// Build-time injected secrets. The placeholder below is replaced by GitHub Actions
/// before compilation — the real value is never stored in source control.
/// </summary>
internal static class AppSecrets
{
    internal const string ConnectionString = "##DB_CONNECTION_STRING##";
}
