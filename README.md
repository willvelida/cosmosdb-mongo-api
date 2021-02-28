# Serverless Web API with Azure Functions, Azure Cosmos DB MongoDB API and C#

In this tutorial, we’ll build a Web API using Azure Functions that stores data in Azure Cosmos DB with MongoDB API in C#

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/gbqg9bg7kxqvqv1r2frk.png)
 
[Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/) is a globally distributed, multi-model, NoSQL database service that allows us to build highly available and scalable applications. Cosmos DB supports applications that use Document model data through it’s [SQL API](https://docs.microsoft.com/en-us/azure/cosmos-db/introduction) and [MongoDB API](https://docs.microsoft.com/en-us/azure/cosmos-db/mongodb-introduction).

I’ve been meaning to produce more content on Cosmos DB’s Mongo API, so in this article, I’m going to be developing a Serverless API in [Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/) that uses a Cosmos DB MongoDB API account. This article has been loosely based on this fantastic tutorial on [creating a Web API with ASP.NET Core and MongoDB](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mongo-app?view=aspnetcore-5.0&tabs=visual-studio#prerequisites).

By the end of this article, you’ll know how to create a Cosmos DB account that uses the MongoDB API. You’ll also know how to create a simple CRUD Web API in C# that interacts with a Mongo DB API account.

If you want to see the whole code before diving into the article, you can check it out on my GitHub [here](https://github.com/willvelida/cosmosdb-mongo-api).

What you’ll need to follow along with this article:
- Visual Studio 2019 with the Azure Development workload.
- Azure Subscription.
- [Postman](https://www.postman.com/).

## How does Azure Cosmos DB support a MongoDB API?

[Azure Cosmos DB implements the wire protocol for MongoDB](https://docs.microsoft.com/en-us/azure/cosmos-db/mongodb-introduction#wire-protocol-compatibility), allowing us to use client drivers and tools that we’re used to, but allow us to host our data in Azure Cosmos DB.

This is great if we already have applications that use MongoDB. We can change our applications to use Azure Cosmos DB without having to make significant changes to our codebase. We can also gain the benefits of Azure Cosmos DB, such as Turnkey distribution and elastic scalability in both throughput and storage.

## Setting up an Account with the MongoDB API

Let’s set up our Cosmos DB account. Login to Azure and click on Create a resource. Look for Azure Cosmos DB and click ‘New’.
On the ‘Create Azure Cosmos DB Account’ page, provide the following configuration:

- *Resource Group* — Resource groups in Azure are a logical collection of resources. For this tutorial, you can create a new one or add your account to an existing one.
- *Account Name* — This will be the name of your account. It needs to be unique across Azure, so make it a good one :)
API — Azure Cosmos DB is a multi-model database, but when we create a Cosmos DB account, we can only pick one API and that will be the API for the lifetime of the account. Since this is a Mongo DB API tutorial, pick the ‘**Azure Cosmos DB for MongoDB API**’ option.
- *Location* — Where we want to provision the account. I’ve chosen Australia East as that’s the datacenter close to me, but pick a datacenter close to you.
- *Capacity Mode* — In a nutshell, this is how throughput will be provisioned in this account. I have written articles on how throughput works in Cosmos DB if you are interested, but for now choose ‘**Serverless**’ (still in preview at time of writing).
- *Account Type* — Choose production
- *Version* — This is the version of the MongoDB wire protocol that the account will support. Choose 3.6
- *Availability Zones* — Disable this for now.

You configuration should look like the figure below:

![](https://miro.medium.com/max/1400/1*Y92WfhaI0ysHCQqMaAln2g.png)

Click **Review+Create**, then **Create** to create your Cosmos DB account. Feel free to grab a cup of tea while you wait.
Once our account is set up, we can create our Database and collection we need for this tutorial. In the original blog post, we would create our Database and Collection via the Mongo Shell. But to keep things simple, we can do this in the portal.

In your Cosmos DB account, head into your Data Explorer and click ‘**New Collection**’. Enter ‘**BookstoreDB**’ as your database name and Books as your collection name. We are then asked to choose a shard key.

Sharding in Mongo DB is a method for distributing data across multiple machines. If you’ve used the SQL API in Azure Cosmos DB, this is similar to a Partition Key. Essential this shard will help your application scale in terms of both throughput and storage through horizontal scaling.

For this tutorial, I’m going to pick the category as the shard key. Click OK to create your database and collection.

![](https://miro.medium.com/max/1400/1*CsAZZxJCmCRzKrmLfxTVpg.png)

We now have our Database, collection and account all set up. We just need one more thing before setting up, our connection string. Click on Connection String and copy the *PRIMARY CONNECTION STRING* value. Save this for later.

## Creating our Function Application.

Let’s head into Visual Studio and create our Serverless API. Open Visual Studio and click ‘**Create a new project**’.

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/2z1yuqjmgg1sj9kvehxa.png)

Choose ‘**Azure Functions**’ as our template to create the project. (Make sure C# is the selected language).

![](https://miro.medium.com/max/1400/1*Ae3YhG297CE1alhpTh4JWQ.png)

Call this project ‘**CosmosBooksApi**’, store the project in a location of your choice and click create:

![](https://miro.medium.com/max/1400/1*TPuFyeo0g4gh8x_Nng_ZhQ.png)

Select Azure Functions v3 (.NET Core) as our Runtime and create an empty project with no triggers:

![](https://miro.medium.com/max/1400/1*CkUYZaBs7V6CspQPUA30vA.png)

Before we start creating any of our Functions, we need to install the **MongoDB.Driver** package. To do this, right click your project and select ‘**Manage NuGet Packages**’. In the Browse section, type in MongoDB.Driver and install the latest stable version.

![](https://miro.medium.com/max/1400/1*ALbTXna1ofZNMqgvuht89w.png)

Once that’s installed, let’s create a *Startup.cs* file that will instantiate our *MongoClient*:

```
using CosmosBooksApi;
using CosmosBooksApi.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.IO;
using System.Security.Authentication;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CosmosBooksApi
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(config["ConnectionString"]));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

            builder.Services.AddSingleton((s) => new MongoClient(settings));
            builder.Services.AddTransient<IBookService, BookService>();
        }
    }
}
```

Since v2 of Azure Functions, [we have support for Dependency Injection](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection). This will help us instantiate our *MongoClient* as a Singleton, so we can share our *MongoClient* amongst all of our Functions rather than creating a new instance of our client every time we want to invoke our Functions.

Let’s go through this class. To register our services, we need to add components to a *IFunctionsHostBuilder* instance, which we pass as a parameter in our *Configure* method.

In order to use this method, we need to add the *FunctionsStartup* assembly attribute to the Startup class itself.

We then create a new configuration of type *IConfiguration*. All this does is pick up configuration for the Function application from a *local.settings.json* file. We then add the *IConfiguration* service as a Singleton.

We can then set up our *MongoClient*. We start off by setting our connection string by passing in our *PRIMARY CONNECTION STRING* from earlier as a *MongoUrl()* object. Save this in your *local.settings.json* file. For reference, this is what my file looks like:

```
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "ConnectionString": "<PRIMARY_CONNECTION_STRING>",
    "DatabaseName": "BookstoreDB",
    "CollectionName":  "Books"
  }
}
```

We can then pass the key of the setting (“**ConnectionString**”) into our *MongoUrl* object.

We then need to enable SSL by using the Tls12 protocol in the *SslSettings* for our *MongoClientSettings*. This is required by Azure Cosmos DB to connect to our MongoDB API account.

Once we’ve set up our *MongoClientSettings*, we can just pass this into our *MongoClient* object, which we set up as a Singleton Service.

Now we need to create a basic class to represent our Book model. Let’s write the following:

```
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CosmosBooksApi.Models
{
    public class Book
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string BookName { get; set; }
        [BsonElement("price")]
        public decimal Price { get; set; }
        [BsonElement("category")]
        public string Category { get; set; }
        [BsonElement("author")]
        public string Author { get; set; }
    }
}
```

In this class, we have properties for the Book’s Id, name, price, category and author. The Id property has been annotated with the *BsonId* property to indicate that this will be the document’s primary key. We have also annotated the Id property with *[BsonRepresentation(BsonType.ObjectId)]* to pass our id as type string, rather than *ObjectId*. Mongo will handle the conversion from string to *ObjectId* for us.

The rest of our properties have been annotated with *[BsonElement()]*. This will determine how our properties look within our collection.

Now we’re going to create a service that will that handle the logic that works with our Cosmos DB account. Let’s define an interface called *IBookService.cs*.

```
using CosmosBooksApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosBooksApi.Services
{
    public interface IBookService
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
        Task CreateBook(Book bookIn);

        /// <summary>
        /// Updates an existing book in the Books collection
        /// </summary>
        /// <param name="id"></param>
        /// <param name="book"></param>
        /// <returns></returns>
        Task UpdateBook(string id, Book bookIn);

        /// <summary>
        /// Removes a book from the Books collection
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        Task RemoveBook(Book bookIn);

        /// <summary>
        /// Removes a book with the specified id from the Books collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task RemoveBookById(string id);
    }
}
```

This is just a simple CRUD interface that defines the contract that our service should implement. Now let’s implement this interface:

```
using CosmosBooksApi.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosBooksApi.Services
{
    public class BookService : IBookService
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Book> _books;

        public BookService(
            MongoClient mongoClient,
            IConfiguration configuration)
        {
            _mongoClient = mongoClient;
            _database = _mongoClient.GetDatabase(configuration["DatabaseName"]);
            _books = _database.GetCollection<Book>(configuration["CollectionName"]);
        }

        public async Task CreateBook(Book bookIn)
        {
            await _books.InsertOneAsync(bookIn);
        }

        public async Task<Book> GetBook(string id)
        {
            var book = await _books.FindAsync(book => book.Id == id);
            return book.FirstOrDefault();
        }

        public async Task<List<Book>> GetBooks()
        {
            var books = await _books.FindAsync(book => true);
            return books.ToList();
        }

        public async Task RemoveBook(Book bookIn)
        {
            await _books.DeleteOneAsync(book => book.Id == bookIn.Id);
        }

        public async Task RemoveBookById(string id)
        {
            await _books.DeleteOneAsync(book => book.Id == id);
        }

        public async Task UpdateBook(string id, Book bookIn)
        {
            await _books.ReplaceOneAsync(book => book.Id == id, bookIn);
        }
    }
}
```

I’ve injected my dependencies to my *MongoClient* and *IConfiguration*, then I create my database and collection so I can perform operations against them. Lets explore the different methods one by one.

[InsertOneAsync](https://mongodb.github.io/mongo-csharp-driver/2.5/apidocs/html/M_MongoDB_Driver_IMongoCollection_1_InsertOneAsync.htm) — This will insert a single document into our *IMongoCollection* asynchronously. Here, we pass in the document we want to persist, this case being the Book object. We could also pass in some custom options (*InsertOneOptions*) and a *CancellationToken*. We’re not returning anything here expect the result of the insert operation ‘Task’.

[FindAsync](https://mongodb.github.io/mongo-csharp-driver/2.4/apidocs/html/M_MongoDB_Driver_IMongoCollectionExtensions_FindAsync__1_1.htm) — This will find a document that matches our filter asynchronously. Here, we use a lambda expression to find a book with the same id that we have provided in the method. We then use Linq to return the matching book.

[DeleteOneAsync](https://mongodb.github.io/mongo-csharp-driver/2.4/apidocs/html/M_MongoDB_Driver_IMongoCollectionExtensions_DeleteOneAsync__1_1.htm) — This will delete a single document that matches our expression asynchronously. Again, we use a lambda expression to find the book we wish to delete. We don’t return anything except the result of the operation.

[ReplaceOneAsync](https://mongodb.github.io/mongo-csharp-driver/2.4/apidocs/html/M_MongoDB_Driver_IMongoCollectionExtensions_ReplaceOneAsync__1.htm) — This will replace a single document asynchronously.

So we’ve created our *MongoClient* and have a basic CRUD service that we can use to interact with our Cosmos DB account. We’re now ready to start creating our Functions.

For this tutorial we will create the following functions:
- CreateBook
- DeleteBook
- GetAllBooks
- GetBookById
- UpdateBook

To create a new Function, we right-click our solution file and select ‘**Add New Azure Function**’. We should see a pop-up like this. Select Http Trigger and select Anonymous as the Function’s Authorization level.

![](https://miro.medium.com/max/1400/1*nLIGCbjpGLpjngkfl9qWAA.png)

Let’s start with our *CreateBook* function. Here is the code:

```
using CosmosBooksApi.Models;
using CosmosBooksApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CosmosBooksApi.Functions
{
    public class CreateBook
    {
        private readonly ILogger<CreateBook> _logger;
        private readonly IBookService _bookService;

        public CreateBook(
            ILogger<CreateBook> logger,
            IBookService bookService)
        {
            _logger = logger;
            _bookService = bookService;
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
                    Id = ObjectId.GenerateNewId().ToString(),
                    BookName = bookRequest.BookName,
                    Price = bookRequest.Price,
                    Category = bookRequest.Category,
                    Author = bookRequest.Author
                };

                await _bookService.CreateBook(book);

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
```

Here we are injecting our *IBookService* and our *ILogger* into the function. We invoke this Function by making a post request to the ‘**/Book**’ Route. We take the incoming HttpRequest and Deserialize it into a Book object. We then insert the Book into our Books collection. If we’re successful, we get a 201 response (Created). If not, we’ll throw a 500 response.

It’s a bit of a dramatic response code. We would want to throw a different code if there was a bad request of if our Cosmos DB account is unavailable, but for this basic example it will suffice.

Now let’s take a look at the *DeleteBook* function:

```
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
```

This time, we pass in an id to our Function (‘**/Book/id**’) to find the Book that we want to delete from our collection. We first look for the book using *IBookService* method *.GetBook(id)*. If we the book doesn’t exist, the Function will throw a 404 (not found) response.

If we can find the book, we then pass this book into our *RemoveBook(book)* method to delete the book from our collection in Cosmos DB. If successful, we return a 204 response.

Here is the code for the *GetAllBooks* function:

```
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
```

In this function, we simply make a GET request to the ‘**/Books**’ route. This Function will call the *.GetBooks()* method on our *IBookService* to retrieve all the books in our collection. If there are no books, we throw 404. If there are books returned to us, the function will return these as an array back to the user.

Our *GetBookById* function is similar to our *GetAllBooks* function, but this time we pass the id of the Book that we want to return to us:

```
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
    public class GetBookById
    {
        private readonly ILogger<GetBookById> _logger;
        private readonly IBookService _bookService;

        public GetBookById(
            ILogger<GetBookById> logger,
            IBookService bookService)
        {
            _logger = logger;
            _bookService = bookService;
        }

        [FunctionName(nameof(GetBookById))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Book/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result;

            try
            {
                var book = await _bookService.GetBook(id);

                if (book == null)
                {
                    _logger.LogWarning($"Book with id: {id} doesn't exist.");
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }

                result = new OkObjectResult(book);
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
```

We also pass an id to our *UpdateBook* function. We first make a call to our *.GetBook(id)* method to find the book we want to update. Once we’ve found the book, we then read the incoming request and deserialize it into a Book object. We then use the deserialized request to update our Book object and then pass this object into out *.UpdateBook()* method along with the id that we used to invoke the Function.

```
using CosmosBooksApi.Models;
using CosmosBooksApi.Services;
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
        private readonly IBookService _bookService;

        public UpdateBook(
            ILogger<UpdateBook> logger,
            IBookService bookService)
        {
            _logger = logger;
            _bookService = bookService;
        }

        [FunctionName(nameof(UpdateBook))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Book/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result;

            try
            {
                var bookToUpdate = await _bookService.GetBook(id);

                if (bookToUpdate == null)
                {
                    _logger.LogWarning($"Book with id: {id} doesn't exist.");
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }

                var input = await new StreamReader(req.Body).ReadToEndAsync();

                var updateBookRequest = JsonConvert.DeserializeObject<Book>(input);

                Book updatedBook = new Book
                {
                    Id = id,
                    BookName = updateBookRequest.BookName,
                    Author = updateBookRequest.Author,
                    Category = bookToUpdate.Category,
                    Price = updateBookRequest.Price
                };

                await _bookService.UpdateBook(id, updatedBook);

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
```

## Testing our Function

Now that we’ve finished coding up our Functions, let’s spin it up and test it! Press F5 to start running our function locally. Give it a second to spin up and you should see the following endpoints for each of our Functions.

![](https://miro.medium.com/max/1400/1*TGejFS-m5T0Y0euZaE1Y7Q.png)

As you can see, our functions are running on localhost. We can use Postman to test these endpoints.

Let’s start with our *CreateBook* Function. Copy and Paste the endpoint for the Function into Postman. Set the request method to POST and click on the body tab. We need to send our request as a JSON payload, so set it to JSON and add the following body:

```
{
  "BookName" : "Computer Science: Distilled",
  "Price": 11.99,
  "Category": "Technology",
  "Author": "Wladston Ferreira Filho"
}
```

Your Postman request should look like this:

![](https://miro.medium.com/max/1400/1*idOuRJxoInuRYS34nDGovQ.png)

Hit Send to send the request. We should get the following response (201).

![](https://miro.medium.com/max/1400/1*D0X1yOqZhoMNe631QdxE3Q.png)

We can view the document in our Cosmos DB account to make sure that we added the document to our account, which it has:

![](https://miro.medium.com/max/1400/1*TVnhiVMl6U5TzbHu4dFUIQ.png)

Insert a couple more books before moving on. We will now try to retrieve all the books in our collection using the *GetAllBooks* Function. Clear the JSON payload from the Body and change the request method to GET. Hit Send to make the request:

![](https://miro.medium.com/max/1400/1*NR_HCnBOFPQ12BWufPtYSg.png)

We should get a response like this:

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/rx44arlwgc0950lh681l.png)

Here we have all the books in our collection returned back to us as a JSON array. Now, let’s test our *GetBookById* Function. In the response of our *GetAllBooks* function, grab an id and add it as a parameter in your route. Keep the body clear and keep the GET request method. All that has changed here is that we are looking for a specific book by using its Id. 

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/fmhb9g1wbxyx3lr59rre.png)

Hit Send to make the request. We should have the book object returned to us like so:

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/3ievidqwisth067ocbmp.png)

Now let’s remove this book from our Cosmos DB collection. Change your request method to DELETE in Postman and hit Send.

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/kcjbgvnunqpycz7injta.png)

We should get the following response:

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/dmlst0mr846oy1n5b26z.png)
  
Checking our Collection in Azure Cosmos DB, we can see that the book is no longer there:

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ide40rvpg1uzmt0tt2hj.png)

Finally, let’s try updating a book. Let’s take the following book in our collection:

```
{
  "id": "603ae1b621786dd7fd92d5c0",
  "bookName": "The Dark Net",
  "price": 18.99,
  "category": "Technology",
  "author": "Jamie Bartlett"
}
```

Grab the id and use it as a parameter to our *UpdateBook* function. Change the method to a PUT request and add the following body to our request:
   
```
{
  "bookName": "The Dark Net v2",
  "price": 11.99,
  "author": "Jamie Bartlett"
}
```

We send this body as a JSON payload. Hit Send to update this Book document.

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/3ns4qwn63ufucgrqi81y.png)

We should get the following response.

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/96cv19zzgjvtt4jp0fmn.png)

We can also verify that the document has updated successfully in our Cosmos DB account.

![image](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/qv4p0i2vr5akp907mumb.png)

## Conclusion

In this tutorial, we built an CRUD Web API using Azure Functions that manages books in a Cosmos DB Mongo API account. While this was a simple tutorial, hopefully you can see that if you’ve already built applications using MongoDB as a datastore, you can easily change to Azure Cosmos DB without making drastic changes to your code base.

If you want to download the full code, check it out on my [GitHub](https://github.com/willvelida/cosmosdb-mongo-api) (If you notice anything wrong and want to help fix it, please feel free to make a PR!)

If you have any questions, please feel to comment below or reach out to me via [Twitter](https://twitter.com/willvelida).   