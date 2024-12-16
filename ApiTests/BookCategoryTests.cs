using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;
using static System.Reflection.Metadata.BlobBuilder;

namespace ApiTests
{
    [TestFixture]
    public class BookCategoryTests : IDisposable
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
        public void Test_BookCategoryLifecycle()
        {
            // Step 1: Create a new book category
            var createRequest = new RestRequest("category", Method.Post);
            createRequest.AddHeader("Authorization", $"Bearer {token}");
            var title = "Fictional Literature";

            createRequest.AddJsonBody(new
            {
                title
            });

            var createResponse = client.Execute(createRequest);
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");

            var createdCategory = JObject.Parse(createResponse.Content);
            string categoryId = createdCategory["_id"]?.ToString();

            Assert.That(categoryId, Is.Not.Null.And.Not.Empty, "The category ID should not be null and empty.");
            Assert.That(createdCategory["title"]?.ToString(), Is.EqualTo(title), "The book category title should be 'Fictional Literature'.");

            // Step 2: Retrieve all book categories and verify the newly created category is present
            var getAllRequest = new RestRequest("category", Method.Get);
            var getAllResponse = client.Execute(getAllRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getAllResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(getAllResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                var categories = JArray.Parse(getAllResponse.Content);
                Assert.That(categories.Type, Is.EqualTo(JTokenType.Array), "Expected response content should be a JSON array.");
                Assert.That(categories.Count, Is.GreaterThan(0), "Expected at least one category in the response.");

                var getByIdRequest = new RestRequest($"category/{categoryId}", Method.Get);
                var getByIdResponse = client.Execute(getByIdRequest);

                Assert.Multiple(() =>
                {
                    Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                    Assert.That(getByIdResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                    var createdCategory = JObject.Parse(getByIdResponse.Content);
                    Assert.That(createdCategory["_id"]?.ToString(), Is.EqualTo(categoryId), "The category ID is not present in the response.");
                    Assert.That(createdCategory["title"]?.ToString(), Is.EqualTo(title), "The category title is not present in the response.");
                });
            });

            // Step 3: Update the category title
            var updateRequest = new RestRequest($"category/{categoryId}", Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            var updatedCategoryTitle = "Updated Fictional Literature";

            updateRequest.AddJsonBody(new
            {
                updatedCategoryTitle
            });

            var updateResponse = client.Execute(updateRequest);
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");

            // Step 4: Verify that the category details have been updated
            var getUpdatedCategoryRequest = new RestRequest($"category/{categoryId}", Method.Get);
            var getUpdatedCategoryResponse = client.Execute(getUpdatedCategoryRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getUpdatedCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");
                Assert.That(getUpdatedCategoryResponse.Content, Is.Not.Empty, "Response content should not be empty.");

                var updatedCategory = JObject.Parse(getUpdatedCategoryResponse.Content);
                Assert.That(updatedCategory["title"]?.ToString(), Is.EqualTo(title), "The updated category title should be 'Updated Fictional Literature'.");
            });

            // Step 5: Delete the category and validate it's no longer accessible
            var deleteRequest = new RestRequest($"category/{categoryId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is not OK.");

            // Step 6: Verify that the deleted category cannot be found
            var getDeletedCategoryRequest = new RestRequest($"category/{categoryId}", Method.Get);
            var getDeletedCategoryResponse = client.Execute(getDeletedCategoryRequest);

            Assert.That(getDeletedCategoryResponse.Content, Is.Empty.Or.EqualTo("null"), "Deleted category should not be found.");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
