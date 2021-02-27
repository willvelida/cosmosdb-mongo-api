using CosmosBooksApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosBooksApi.Repositories
{
    public interface IBookRepository
    {
        /// <summary>
        /// Get all books from the Books collection
        /// </summary>
        /// <returns></returns>
        Task<List<Book>> GetBooks();

        /// <summary>
        /// Get a book by its id from the Books collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Book> GetBook(string id);

        /// <summary>
        /// Insert a book into the Books collection
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        Task CreateBook(Book book);

        /// <summary>
        /// Updates an existing book in the Books collection
        /// </summary>
        /// <param name="id"></param>
        /// <param name="book"></param>
        /// <returns></returns>
        Task UpdateBook(string id, Book book);

        /// <summary>
        /// Removes a book from the Books collection
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        Task RemoveBook(Book book);

        /// <summary>
        /// Removes a book with the specified id from the Books collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task RemoveBookById(string id);
    }
}
