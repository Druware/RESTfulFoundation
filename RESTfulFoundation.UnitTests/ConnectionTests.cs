using System.Text.Json.Serialization;

namespace RESTfulFoundation.UnitTests
{
    class Player : RESTObject
    {
        public static readonly string Path = "/api/Players";
        
        [JsonPropertyName("playerId")]
        public long PlayerId { get; set; }
        [JsonPropertyName("playerName")]
        public string? PlayerName { get; set; }
    }

    public class ConnectionTests
    {
        private readonly TestConfiguration _configuration = new();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestConfiguration()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");
            Assert.Pass();
        }
        
        [Test]
        public void TestUrlBuilder()
        {
            const string rootPath = "https://www.druware.com/";
            const string path = "/api/controller/";
            const long id = 12;

            var result = RESTConnection.BuildUrlString(null, rootPath, path, "", id.ToString());

            Assert.That(result, 
                Is.EqualTo("https://www.druware.com/api/controller/12/"), 
                "Resulting String does not match expected result");
        }

        [Test]
        public void TestListErrorHandling()
        {
            const string path = "/api/notimplemented/";

            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = connection.List<Player>(path, "");
            
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "Result is null and should not be");
                Assert.That(connection.Info!, Is.Not.Empty, "Info does not have any records and should");
            });

            Assert.Pass();
        }

        [Test]
        public void TestList()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = connection.List<Player>(Player.Path, "");

            Assert.That(result, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result!.Count, Is.GreaterThan(0), "List does not have any records");
        }
        
        [Test]
        public async Task TestListAsync()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = await connection.ListAsync<Player>(Player.Path);

            Assert.That(result, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result!.Count, Is.GreaterThan(0), "List does not have any records");
        }

        [Test]
        public void TestPagedList()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = connection.List<Player>(Player.Path, 0);

            Assert.That(result, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result.TotalRecords ?? 0, Is.GreaterThan(0), "List does not have any records");
        }

        [Test]
        public async Task TestPagedListAsync()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = await connection.ListAsync<Player>(Player.Path, 0);

            Assert.That(result, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result.TotalRecords ?? 0, Is.GreaterThan(0), "List does not have any records");        }

        // GET/POST/PUT/DELETE

        [Test]
        public void TestGet()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = connection.Get<Player>(Player.Path, 1.ToString(), "");
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.EqualTo(1), "Result Id does not match");
                Assert.That(result?.PlayerName, Is.EqualTo("Mickey Mouse"), "Result Name does not match");
            });
        }
        
        [Test]
        public async Task TestGetAsync()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            var result = await connection.GetAsync<Player>(Player.Path, 1.ToString());
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.EqualTo(1), "Result Id does not match");
                Assert.That(result?.PlayerName, Is.EqualTo("Mickey Mouse"), "Result Name does not match");
            });
        }

        [Test]
        public void TestPost()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");
            
            Player p = new() { PlayerName = "A Test of Post" };

            var result = connection.Post<Player, Player>(Player.Path, p);
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");

                Assert.That(connection.Delete(Player.Path, result!.PlayerId.ToString()),
                    Is.True, "Delete failed");
            });
        }

        [Test]
        public async Task TestPostAsync()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");
            
            Player p = new() { PlayerName = "A Test of Post" };

            var result = await connection.PostAsync<Player, Player>(Player.Path, p);
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");

                Assert.That(connection.Delete(Player.Path, result!.PlayerId.ToString()),
                    Is.True, "Delete failed");
            });        
        }
        

        [Test]
        public void TestPut()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            Player p = new() { PlayerId = 2, PlayerName = "An Async Test of Put" };

            var result = connection.Put<Player?, Player>(Player.Path, 2.ToString(), p);
            result ??= connection.Get<Player?>(Player.Path, 2.ToString());
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("An Async Test of Put"), "Result Name does not match");
            });
            result!.PlayerName = "Donald Duck";
            result = connection.Put<Player?, Player>(Player.Path, 2.ToString(), result);
            result ??= connection.Get<Player?>(Player.Path, 2.ToString());

            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("Donald Duck"), "Result Name does not match");
            });
        }

        [Test]
        public async Task TestPutAsync()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            Player p = new() { PlayerId = 2, PlayerName = "An Async Test of Put" };

            var result = await connection.PutAsync<Player?, Player>(Player.Path, 2.ToString(), p);
            result ??= await connection.GetAsync<Player?>(Player.Path, 2.ToString());
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("An Async Test of Put"), "Result Name does not match");
            });
            result!.PlayerName = "Donald Duck";
            result = await connection.PutAsync<Player?, Player>(Player.Path, 2.ToString(), result);
            result ??= await connection.GetAsync<Player?>(Player.Path, 2.ToString());

            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("Donald Duck"), "Result Name does not match");
            });        
        }

        [Test]
        public void TestPostDelete()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            Player p = new() { PlayerName = "A Test of Post" };

            var result = connection.Post<Player?, Player>(Player.Path, p);
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");

                Assert.That(connection.Delete(Player.Path, result!.PlayerId.ToString()),
                    Is.True, "Delete failed");
            });
        }

        [Test]
        public async Task TestPostDeleteAsync()
        {
            Assert.That(_configuration.Host, Is.Not.Null, "Host Cannot be null");

            var connection = new RESTConnection(_configuration.Host);
            Assert.That(connection, Is.Not.Null, "Connection should never be null");

            Player p = new() { PlayerName = "A Test of Post" };

            var result = await connection.PostAsync<Player?, Player>(Player.Path, p);
            Assert.Multiple(() =>
            {
                Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
                Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");
            });
            var deleted = await connection.DeleteAsync(Player.Path,
                result!.PlayerId.ToString());
            Assert.That(deleted, Is.True, "Delete failed");
        }
        
        
    }
    
}

