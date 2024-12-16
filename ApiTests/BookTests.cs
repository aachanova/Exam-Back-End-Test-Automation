using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class BookTests : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
        }

        [Test]
        public void Test_GetAllBooks()
        {
            var getRequest = new RestRequest("book", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(getResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                var books = JArray.Parse(getResponse.Content);

                Assert.That(books.Type, Is.EqualTo(JTokenType.Array), "Response content is not a JSON Array.");
                Assert.That(books.Count, Is.GreaterThan(0), "Expected at least one books in the response.");

                foreach (var book in books)
                {
                    Assert.That(book["title"]?.ToString(), Is.Not.Null.Or.Empty, "The title should not be null or empty.");
                    Assert.That(book["author"]?.ToString(), Is.Not.Null.Or.Empty, "The author should not be null or empty.");
                    Assert.That(book["description"]?.ToString(), Is.Not.Null.Or.Empty, "The description should not be null or empty.");
                    Assert.That(book["price"]?.Value<double>(), Is.Not.Null.Or.Empty, "The price should not be null or empty.");
                    Assert.That(book["pages"]?.Value<int>(), Is.Not.Null.Or.Empty, "The pages should not be null or empty.");
                    Assert.That(book["category"]?.ToString(), Is.Not.Null.Or.Empty, "The category should not be null or empty.");
                }
            });
        }

        [Test]
        public void Test_GetBookByTitle()
        {
            var getRequest = new RestRequest("book", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(getResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                var books = JArray.Parse(getResponse.Content);
                var book = books.FirstOrDefault(b => b["title"]?.ToString() == "The Great Gatsby");

                Assert.That(book["title"]?.ToString(), Is.EqualTo("The Great Gatsby"), "The book with title 'The Great Gatsby' was not present in the response.");
                Assert.That(book["author"]?.ToString(), Is.EqualTo("F. Scott Fitzgerald"), "The book author is not 'F. Scott Fitzgerald'");
            });
        }

        [Test]
        public void Test_AddBook()
        {
            var getCategoriesRequest = new RestRequest($"category", Method.Get);
            var getCategoriesResponse = client.Execute(getCategoriesRequest);

            var categories = JArray.Parse(getCategoriesResponse.Content);

            var category = categories.First();
            var categoryId = category["_id"]?.ToString();

            var createRequest = new RestRequest("book", Method.Post);
            createRequest.AddHeader("Authorization", $"Bearer {token}");

            var title = "New test book";
            var author = "Some test author";
            var description = "Great test description";
            var price = 789.55;
            var pages = 100;
            createRequest.AddJsonBody(new
            {
                title,
                author,
                description,
                price,
                pages,
                category = categoryId
            });

            var createResponse = client.Execute(createRequest);

            Assert.Multiple(() =>
            {
                Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(createResponse.Content, Is.Not.Empty, "Response content should not be empty.");
            });

            var createdBook = JObject.Parse(createResponse.Content);
            Assert.That(createdBook["_id"]?.ToString(), Is.Not.Empty, "The created book doesn't have an Id.");

            var createdBookId = createdBook["_id"]?.ToString();

            var getCreatedBookRequest = new RestRequest($"book/{createdBookId}", Method.Get);
            var getCreatedBookResponse = client.Execute(getCreatedBookRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getCreatedBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(getCreatedBookResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                var book = JObject.Parse(getCreatedBookResponse.Content);

                Assert.That(book["title"]?.ToString(), Is.EqualTo(title), "The book title should be 'New test book'.");
                Assert.That(book["author"]?.ToString(), Is.EqualTo(author), "The book author should be 'Some test author'.");
                Assert.That(book["price"]?.Value<double>(), Is.EqualTo(price), "The book price should be '789.55'.");
                Assert.That(book["pages"]?.Value<int>(), Is.EqualTo(pages), "The book pages should be '100'.");

                Assert.That(book["category"]?.ToString(), Is.Not.Null.Or.Empty, "The book category should not be null or empty.");
                Assert.That(book["category"]?["_id"]?.ToString(), Is.EqualTo(categoryId), $"The book category should be '{categoryId}'.");
            });
        }

        [Test]
        public void Test_UpdateBook()
        {
            var getRequest = new RestRequest("book", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
            Assert.That(getResponse.Content, Is.Not.Empty, "Response content should not be empty.");

            var books = JArray.Parse(getResponse.Content);
            var bookToUpdate = books.FirstOrDefault(b => b["title"]?.ToString() == "The Catcher in the Rye");
            Assert.That(bookToUpdate, Is.Not.Null, "A book with name 'The Catcher in the Rye' was not found in the response.");

            var bookId = bookToUpdate["_id"]?.ToString();

            var updateRequest = new RestRequest($"book/{bookId}", Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");

            var title = "Updated Book Title";
            var author = "Updated Author";

            updateRequest.AddJsonBody(new
            {
                title,
                author
            });

            var updateResponse = client.Execute(updateRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(getResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                var content = JObject.Parse(updateResponse.Content);

                Assert.That(content["title"]?.ToString(), Is.EqualTo(title), "The book title should be 'Updated Book Title'.");
                Assert.That(content["author"]?.ToString(), Is.EqualTo(author), "The book author should be 'Updated Author'.");
            });
        }

        [Test]
        public void Test_DeleteBook()
        {
            var getRequest = new RestRequest("book", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
            Assert.That(getResponse.Content, Is.Not.Empty, "Response content should not be empty.");

            var books = JArray.Parse(getResponse.Content);
            var bookToDelete = books.FirstOrDefault(b => b["title"]?.ToString() == "To Kill a Mockingbird");
            Assert.That(bookToDelete, Is.Not.Null, "The book with title 'To Kill a Mockingbird' was not found in the rsponse.");

            var bookId = bookToDelete["_id"]?.ToString();

            var deleteRequest = new RestRequest($"book/{bookId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");

                var verifyGetRequest = new RestRequest($"book/{bookId}", Method.Get);
                var verifyGetResponse = client.Execute(verifyGetRequest);

                Assert.That(verifyGetResponse.Content, Is.Null.Or.EqualTo("null"), "The response content was not null.");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
