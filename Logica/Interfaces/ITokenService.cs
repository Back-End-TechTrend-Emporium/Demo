using Data.Entities;

namespace Logica.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}