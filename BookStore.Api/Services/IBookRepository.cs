using BookStore.Api.Models;

namespace BookStore.Api.Services;

public interface IBookRepository
{
    IEnumerable<Book> GetAll();
    Book? GetById(int id);
    Book Add(Book book);
    bool Delete(int id);
}