using CosmosBooksApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CosmosBooksApi.Functions
{
    public class GetAllBooks
    {
        private readonly ILogger<GetAllBooks> _logger;
        private readonly IBookService _bookService;

        public GetAllBooks(
            ILogger<GetAllBooks> logger,
            IBookService bookService)
        {
            _logger = logger;
            _bookService = bookService;
        }

        [FunctionName(nameof(GetAllBooks))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Books")] HttpRequest req)
        {
            IActionResult result;

            try
            {
                var books = await _bookService.GetBooks();

                if (books == null)
                {
                    _logger.LogWarning("No books found!");
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }

                result = new OkObjectResult(books);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Internal Server Error. Exception thrown: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
