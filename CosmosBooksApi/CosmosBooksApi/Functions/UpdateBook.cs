using CosmosBooksApi.Models;
using CosmosBooksApi.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CosmosBooksApi.Functions
{
    public class UpdateBook
    {
        private readonly ILogger<UpdateBook> _logger;
        private readonly IBookRepository _bookRepository;

        public UpdateBook(
            ILogger<UpdateBook> logger,
            IBookRepository bookRepository)
        {
            _logger = logger;
            _bookRepository = bookRepository;
        }

        [FunctionName(nameof(UpdateBook))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Book/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result;

            try
            {
                var bookToUpdate = await _bookRepository.GetBook(id);

                if (bookToUpdate == null)
                {
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }

                var input = await new StreamReader(req.Body).ReadToEndAsync();

                var updateBookRequest = JsonConvert.DeserializeObject<Book>(input);

                Book updatedBook = new Book
                {
                    Id = id,
                    BookName = bookToUpdate.BookName,
                    Author = updateBookRequest.Author,
                    Category = updateBookRequest.Category,
                    Price = updateBookRequest.Price
                };

                await _bookRepository.UpdateBook(id, updatedBook);

                result = new StatusCodeResult(StatusCodes.Status202Accepted);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Internal Server Error: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
