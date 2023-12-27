using Microsoft.Extensions.Configuration;

namespace RESTfulFoundation.UnitTests;

public class TestConfiguration
{
    private const string ConfigFile = "RESTfulFoundationTests.json";
    public string Host { get; private set; } = "http://localhost/";

    public TestConfiguration()
    {
        // check for the configuration file, and skip if it is not found
        if (File.Exists(ConfigFile))
        {
            var builder =
                new ConfigurationBuilder().AddJsonFile(
                    ConfigFile, false, true);
            var root = builder.Build();
            Host = root["host"] ?? "http://localhost/";
        }
        else
        {
            Console.WriteLine("Tests Using Default Configuration");
        }
    }

}
