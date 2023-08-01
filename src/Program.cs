using Mobius.Qonto.API;
using System.CommandLine;
using System.Configuration;
using System.Globalization;

Console.WriteLine("Mobius.Qonto.AttachmentDownload is running");

#region CommandLine
var secretKeyOption = new Option<string>(
        "--SecretKey",
        "You can find and manage your secret key from the Qonto web application Settings > API Key > Integrations.");
var loginOption = new Option<string>(
        "--Login",
        "You can find and manage your login identifier from the Qonto web application Settings > API Key > Integrations.");
var ibanOption = new Option<string>(
        "--IBAN",
        "The IBAN of the account.");
var directoryOption = new Option<string>(
        "--Directory",
        "The directory of destination of the statement.");

var rootCommand = new RootCommand("Mobius.Qonto.AttachmentDownload")
{
    secretKeyOption,
    loginOption,
    ibanOption,
    directoryOption
};
rootCommand.TreatUnmatchedTokensAsErrors = true;
rootCommand.SetHandler(async (context) => 
{
    var token = context.GetCancellationToken();
    await Main(context?.ParseResult?.GetValueForOption(loginOption),
        context?.ParseResult.GetValueForOption(secretKeyOption),
        context?.ParseResult.GetValueForOption(ibanOption),
        context?.ParseResult.GetValueForOption(directoryOption), 
        token);
    Console.WriteLine("Mobius.Qonto.AttachmentDownload has ending");
});

return rootCommand.Invoke(args);
#endregion
static async Task Main(string? login, string? secreteKey, string? iban, string? directory, CancellationToken cancellationToken)
{
    if (String.IsNullOrEmpty(login))
        throw new System.ArgumentException($"{nameof(login)} is null or empty.", nameof(login));
    if (String.IsNullOrEmpty(secreteKey))
        throw new System.ArgumentException($"{nameof(secreteKey)} is null or empty.", nameof(secreteKey));
    if (String.IsNullOrEmpty(iban))
        throw new System.ArgumentException($"{nameof(iban)} is null or empty.", nameof(iban));
    if (String.IsNullOrEmpty(directory))
        throw new System.ArgumentException($"{nameof(directory)} is null or empty.", nameof(directory));

    var lastExecution = GetLastExecution();

    using var client = new QontoClient();
    client.InitializeAuthorization(login, secreteKey);
    var attachments = await client.GetNewAttachmentsSinceAsync(lastExecution, iban, cancellationToken);
    await QontoClient.DownloadAttachmentsAsync(attachments, directory, cancellationToken);

    SetLastExecution();
}

static DateTime GetMinDateForQonto() => DateTime.MinValue.AddYears(2016);
static DateTime GetLastExecution()
{
    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    var setting = GetSetting(config);

    if (!DateTime.TryParse(setting.Value, out var result))
        result = GetMinDateForQonto();

    return result;
}
static void SetLastExecution()
{
    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    var setting = GetSetting(config);

    setting.Value = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture);
    config.Save(ConfigurationSaveMode.Modified);
}
static KeyValueConfigurationElement GetSetting(Configuration config)
{
    const string K_KEY = "LastExecution";

    var setting = config.AppSettings.Settings[K_KEY];
    if (setting == null)
    {
        config.AppSettings.Settings
            .Add(K_KEY, GetMinDateForQonto().ToString("F", CultureInfo.InvariantCulture));
        // NOTE: Qonto do not allow date before 2016. 
        setting = config.AppSettings.Settings[K_KEY];
    }

    return setting;
}