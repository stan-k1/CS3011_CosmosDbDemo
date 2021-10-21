using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CosmosDbDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private CosmosClient CosmosClient { get; }
        private Microsoft.Azure.Cosmos.Container Container { get; }
        public BooksController(CosmosClient cosmosClient)
        {
            CosmosClient = cosmosClient;
            Container = cosmosClient.GetContainer("cs3011", "cosmos-demo");
        }

        // GET: api/<BooksController>
        [HttpGet]
        public async Task<List<Book>> Get()
        {
            using FeedIterator<Book> feedIterator = Container.GetItemLinqQueryable<Book>().ToFeedIterator();

            List<Book> result = new();
            while (feedIterator.HasMoreResults)
            {
                foreach (var item in await feedIterator.ReadNextAsync())
                {
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        // GET api/<BooksController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> Get(string id)
        {
            using FeedIterator<Book> feedIterator = Container.GetItemLinqQueryable<Book>().Where(b => b.Id == id).ToFeedIterator();
            var results = await feedIterator.ReadNextAsync();
            if (results is null) return NotFound("Could not find the requested book");
            return Ok(results.SingleOrDefault());
        }

        // POST api/<BooksController>
        [HttpPost]
        public async Task Post([FromBody] Book book)
        {
            book.Id = Guid.NewGuid().ToString();
            await Container.CreateItemAsync(book, new PartitionKey(book.Genre));
        }

        // PUT api/<BooksController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromBody] Book book, [FromRoute]string Id)
        {
            using FeedIterator<Book> feedIterator = Container.GetItemLinqQueryable<Book>().Where(b => b.Id == Id).ToFeedIterator();
            var results = await feedIterator.ReadNextAsync();
            Book bookToUpdate = results.SingleOrDefault(); //Get the book to be replaced

            if (bookToUpdate is null) return NotFound("Could not find a book with the requested id");

            book.Genre = bookToUpdate.Genre; //Partition Keys are Immutable
            book.Id = Id; //Set the Id of the new Book to always be the Id of the book to be updated as per the route parameter

            // Replace the existing book with the updated book
            await Container.ReplaceItemAsync<Book>(book, bookToUpdate.Id, new PartitionKey(bookToUpdate.Genre));
            return Ok();
        }

        // DELETE api/<BooksController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string Id)
        {
            using FeedIterator<Book> feedIterator = Container.GetItemLinqQueryable<Book>().Where(b => b.Id == Id).ToFeedIterator();
            var results = await feedIterator.ReadNextAsync();
            Book bookToDelete = results.SingleOrDefault(); //Get the book to be deleted
            if (bookToDelete is null) return NotFound("Could not find a book with the requested id");

            await Container.DeleteItemAsync<Book>(bookToDelete.Id, new PartitionKey(bookToDelete.Genre));
            return Ok("Deletion Completed");
        }
    }
}
