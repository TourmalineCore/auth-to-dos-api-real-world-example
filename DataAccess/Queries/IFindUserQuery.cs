using DataAccess.Models;

namespace DataAccess.Queries
{
    public interface IFindUserQuery
    {
        Task<User?> FindUserByLoginAsync(string corporateEmail);
    }
}