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
    public class DeleteBook
    {
        private readonly ILogger<DeleteBook> _logger;
        private readonly IBookService _bookService;

        public DeleteBook(
            ILogger<DeleteBook> logger,
            IBookService bookService)
        {
            _logger = logger;
            _bookService = bookService;
        }

        [FunctionName(nameof(DeleteBook))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Book/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result;

            try
            {
                var bookToDelete = await _bookService.GetBook(id);

                if (bookToDelete == null)
                {
                    _logger.LogWarning($"Book with id: {id} doesn't exist.");
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }

                await _bookService.RemoveBook(bookToDelete);
                result = new StatusCodeResult(StatusCodes.Status204NoContent);
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
