using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CosmosBooksApi.Repositories;
using CosmosBooksApi.Models;
using MongoDB.Bson;

namespace CosmosBooksApi.Functions
{
    public class CreateBook
    {
        private readonly ILogger<CreateBook> _logger;
        private readonly IBookRepository _bookRepository;

        public CreateBook(
            ILogger<CreateBook> logger,
            IBookRepository bookRepository)
        {
            _logger = logger;
            _bookRepository = bookRepository;
        }

        [FunctionName(nameof(CreateBook))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Book")] HttpRequest req)
        {
            IActionResult result;

            try
            {
                var incomingRequest = await new StreamReader(req.Body).ReadToEndAsync();

                var bookRequest = JsonConvert.DeserializeObject<Book>(incomingRequest);

                var book = new Book
                {
                    id = ObjectId.GenerateNewId().ToString(),
                    BookName = bookRequest.BookName,
                    Price = bookRequest.Price,
                    Category = bookRequest.Category,
                    Author = bookRequest.Author
                };

                await _bookRepository.CreateBook(book);

                result = new StatusCodeResult(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Internal Server Error. Exception: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
