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

namespace CosmosBooksApi.Functions
{
    public class DeleteBook
    {
        private readonly ILogger<DeleteBook> _logger;
        private readonly IBookRepository _bookRepository;

        public DeleteBook(
            ILogger<DeleteBook> logger,
            IBookRepository bookRepository)
        {
            _logger = logger;
            _bookRepository = bookRepository;
        }

        [FunctionName(nameof(DeleteBook))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Book/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result;

            try
            {
                var bookToDelete = await _bookRepository.GetBook(id);

                if (bookToDelete == null)
                {
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }

                await _bookRepository.RemoveBook(bookToDelete);
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
