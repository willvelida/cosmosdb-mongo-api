using CosmosBooksApi.Repositories;
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
        private readonly IBookRepository _bookRepository;

        public GetAllBooks(
            ILogger<GetAllBooks> logger,
            IBookRepository bookRepository)
        {
            _logger = logger;
            _bookRepository = bookRepository;
        }

        [FunctionName(nameof(GetAllBooks))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Books")] HttpRequest req)
        {
            IActionResult result;

            try
            {
                var books = await _bookRepository.GetBooks();

                if (books == null)
                {
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
