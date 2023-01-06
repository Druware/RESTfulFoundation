using System;

namespace RESTfulFoundation.UnitTests
{
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
    }
}

