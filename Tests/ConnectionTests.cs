using System;

namespace RESTfulFoundation.Tests
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

            Assert.That(result.Equals("http://www.trustee13.com/api/controller/12/"),
                Is.True, "Resulting String does not match expected result");
        }

        [Test]
        public void TestList()
        {
            string RootPath = "http://localhost/";
            string Path = "/api/trustee/array";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                connection.List<RESTObject>(Path);

            Assert.That(result?.List, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result?.List!.Count, Is.GreaterThan(0), "List does not have any records");

            return;
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
        public async Task TestListAsync()
        {
            string RootPath = "https://www.trustee13.com/";
            string Path = "/api/trustee/";

            RESTConnection connection = new(RootPath);

            RESTObjectList<RESTObject>? result =
                await connection.ListAsync<RESTObject>(Path);

            Assert.That(result?.List, Is.Not.EqualTo(null), "Result does not contain a list");
            Assert.That(result?.List!.Count, Is.GreaterThan(0), "List does not have any records");

            return;
        }
    }
}

