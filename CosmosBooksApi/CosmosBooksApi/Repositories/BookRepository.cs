using CosmosBooksApi.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosBooksApi.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Book> _books;

        public BookRepository(
            MongoClient mongoClient,
            IConfiguration configuration)
        {
            _mongoClient = mongoClient;
            _database = _mongoClient.GetDatabase(configuration["DatabaseName"]);
            _books = _database.GetCollection<Book>(configuration["CollectionName"]);
        }

        public async Task CreateBook(Book book)
        {
            await _books.InsertOneAsync(book);
        }

        public async Task<Book> GetBook(string id)
        {
            var book = await _books.FindAsync(book => book.id == id);
            return book.FirstOrDefault();
        }

        public async Task<List<Book>> GetBooks()
        {
            var books = await _books.FindAsync(book => true);
            return books.ToList();
        }

        public async Task RemoveBook(Book book)
        {
            await _books.DeleteOneAsync(book => book.id == book.id);
        }

        public async Task RemoveBookById(string id)
        {
            await _books.DeleteOneAsync(book => book.id == id);
        }

        public async Task UpdateBook(string id, Book book)
        {
            await _books.ReplaceOneAsync(book => book.id == id, book);
        }
    }
}
