using System;
using System.Text.Json.Serialization;

namespace RESTfulFoundation.UnitTests
{
    class Player : RESTObject
    {
        [JsonPropertyName("playerId")]
        public long PlayerId { get; set; }
        [JsonPropertyName("playerName")]
        public string? PlayerName { get; set; }
    }

    public class ConnectionTests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestUrlBuilder()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/api/controller/";
            long Id = 12;

            RESTConnection connection = new(RootPath);

            string result = connection.BuildUrlString(Path, "", Id.ToString());

            Assert.That(result.Equals("https://www.trustee13.com/api/controller/12/"),
                Is.True, "Resulting String does not match expected result");
        }

        [Test]
        public void TestListErrorHandling()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/api/notimplemented/";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                connection.List<RESTObject>(Path);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(null), "Result is not null and should be");
                Assert.That(connection.Info!, Is.Not.Empty, "Info does not have any records");
                Assert.That(connection.Info![0], Is.EqualTo("An Exception was raised: Response status code does not indicate success: 404 (Not Found)."), "Unexpected Error Message was returned");
            });

            return;
        }

        [Test]
        public void TestList()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                connection.List<RESTObject>(Path);

            Assert.That(result?.List, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result?.List!.Count, Is.GreaterThan(0), "List does not have any records");

            return;
        }

        [Test]
        public async Task TestListAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                await connection.ListAsync<RESTObject>(Path);

            Assert.That(result?.List, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result?.List!.Count, Is.GreaterThan(0), "List does not have any records");

            return;
        }


        [Test]
        public void TestPagedList()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                connection.List<RESTObject>(Path, 1, 1);

            Assert.That(result?.List, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result?.List!.Count, Is.GreaterThan(0), "List does not have any records");

            return;
        }

        [Test]
        public async Task TestPagedListAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                await connection.ListAsync<RESTObject>(Path, 1, 1);

            Assert.That(result?.List, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result?.List!.Count, Is.GreaterThan(0), "List does not have any records");

            return;
        }

        // GET/POST/PUT/DELETE

        [Test]
        public void TestGet()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player? result = connection.Get<Player?>(Path, (1).ToString());

            Assert.That(result?.PlayerId, Is.EqualTo(1), "Result Id does not match");
            Assert.That(result?.PlayerName, Is.EqualTo("Mickey Mouse"), "Result Name does not match");

            return;
        }

        [Test]
        public async Task TestGetAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player? result = await connection.GetAsync<Player?>(Path, (1).ToString());

            Assert.That(result?.PlayerId, Is.EqualTo(1), "Result Id does not match");
            Assert.That(result?.PlayerName, Is.EqualTo("Mickey Mouse"), "Result Name does not match");

            return;
        }

        [Test]
        public void TestPost()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player p = new() { PlayerName = "A Test of Post" };

            Player? result = connection.Post<Player?>(Path, p);

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");

            Assert.That(connection.Delete(Path, result!.PlayerId.ToString()),
                Is.True, "Delete failed");

            return;
        }

        [Test]
        public async Task TestPostAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player p = new() { PlayerName = "An Async Test of Post" };

            Player? result = await connection.PostAsync<Player?>(Path, p);

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("An Async Test of Post"), "Result Name does not match");

            Assert.That(connection.Delete(Path, result!.PlayerId.ToString()),
                Is.True, "Delete failed");
            return;
        }

        [Test]
        public void TestPut()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player p = new() { PlayerId = 2, PlayerName = "An Async Test of Put" };

            Player? result = connection.Put<Player?>(Path, 2.ToString(), p);
            result ??= connection.Get<Player?>(Path, 2.ToString());

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("An Async Test of Put"), "Result Name does not match");

            result.PlayerName = "Donald Duck";
            result = connection.Put<Player?>(Path, 2.ToString(), result);
            result ??= connection.Get<Player?>(Path, 2.ToString());

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("Donald Duck"), "Result Name does not match");

            return;
        }

        [Test]
        public async Task TestPutAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player p = new() { PlayerId = 2, PlayerName = "An Async Test of Put" };

            Player? result = await connection.PutAsync<Player?>(Path, 2.ToString(), p);
            result ??= await connection.GetAsync<Player?>(Path, 2.ToString());

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("An Async Test of Put"), "Result Name does not match");

            result.PlayerName = "Donald Duck";
            result = await connection.PutAsync<Player?>(Path, 2.ToString(), result);
            result ??= await connection.GetAsync<Player?>(Path, 2.ToString());

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("Donald Duck"), "Result Name does not match");

            return;
        }

        [Test]
        public void TestPostDelete()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player p = new() { PlayerName = "A Test of Post" };

            Player? result = connection.Post<Player?>(Path, p);

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");


            Assert.That(connection.Delete(Path, result!.PlayerId.ToString()),
                Is.True, "Delete failed");


            return;
        }

        [Test]
        public async Task TestPostDeleteAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/unittest/api/Players";

            RESTConnection connection = new(RootPath);

            Player p = new() { PlayerName = "A Test of Post" };

            Player? result = await connection.PostAsync<Player?>(Path, p);

            Assert.That(result?.PlayerId, Is.Not.EqualTo(null), "Result Id is null");
            Assert.That(result?.PlayerName, Is.EqualTo("A Test of Post"), "Result Name does not match");


            Assert.That(await connection.DeleteAsync (Path, result!.PlayerId.ToString()),
                Is.True, "Delete failed");


            return;
        }
    }
}

