using System.Reflection;

namespace RESTfulFoundation.UnitTests;

/// <summary>
/// Regression cover for the shared HttpClient's default request headers.
///
/// RESTConnection's HttpClient is static - one instance for the whole process -
/// and DefaultRequestHeaders.Add appends rather than replaces. Setting the
/// User-Agent in the instance constructor therefore added one more value for
/// every connection constructed: the second connection sent
/// "RESTfulFoundation.Client RESTfulFoundation.Client", the third added a third
/// copy, and so on for the life of the process. A consumer that builds a
/// connection per client per run, over months, ends up sending an unboundedly
/// long request header.
///
/// These tests need no server: they construct connections and inspect the
/// headers that would be sent, so they are the only tests in this project that
/// can run unattended in CI.
/// </summary>
public class UserAgentTests
{
    private const string ExpectedUserAgent = "RESTfulFoundation.Client";

    /// <summary>
    /// The HttpClient the tests inspect is the private static one every
    /// RESTConnection shares. Reaching it by reflection keeps the fix free of
    /// any test-only widening of the public surface.
    /// </summary>
    private static HttpClient SharedHttpClient()
    {
        var field = typeof(RESTConnection).GetField("HttpClient",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null,
            "RESTConnection no longer has a private static HttpClient field " +
            "named 'HttpClient'. If it was renamed or made non-static, this " +
            "test needs updating - do not simply delete it.");

        return (HttpClient)field!.GetValue(null)!;
    }

    [Test]
    public void ConstructingManyConnectionsLeavesExactlyOneUserAgent()
    {
        // Ten is arbitrary but well past the one-per-run the old code needed to
        // start doubling the header up.
        for (var i = 0; i < 10; i++)
            _ = new RESTConnection("http://localhost/");

        var headers = SharedHttpClient().DefaultRequestHeaders;

        Assert.Multiple(() =>
        {
            Assert.That(headers.UserAgent, Has.Count.EqualTo(1),
                "The shared HttpClient accumulated one User-Agent value per " +
                "connection. The header must be set once for the process, not " +
                "in the instance constructor.");

            // What actually goes on the wire: the values joined, which is what
            // grew to "RESTfulFoundation.Client RESTfulFoundation.Client ...".
            Assert.That(string.Join(" ", headers.GetValues("User-Agent")),
                Is.EqualTo(ExpectedUserAgent),
                "The User-Agent sent on the wire is not the single expected " +
                "value. Its text is deliberate - a server may match on it - so " +
                "it must stay exactly '" + ExpectedUserAgent + "'.");
        });
    }

    [Test]
    public void UserAgentIsSetBeforeAnyConnectionIsConstructed()
    {
        // The header belongs to the static client, so it must be in place as
        // soon as the type is initialised. This also pins the fix to the static
        // initialiser rather than to "the first constructor wins".
        var headers = SharedHttpClient().DefaultRequestHeaders;

        Assert.That(headers.GetValues("User-Agent"),
            Is.EqualTo(new[] { ExpectedUserAgent }));
    }

    [Test]
    public void AcceptHeadersAreNotAccumulatedEither()
    {
        // Accept was already being cleared before it was set, two lines from
        // the User-Agent that was not. Now that both live in the static
        // initialiser, neither can grow.
        var before = SharedHttpClient().DefaultRequestHeaders.Accept.Count;

        for (var i = 0; i < 10; i++)
            _ = new RESTConnection("http://localhost/");

        Assert.That(SharedHttpClient().DefaultRequestHeaders.Accept,
            Has.Count.EqualTo(before),
            "Constructing connections changed the shared Accept header.");
    }
}
