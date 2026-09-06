using System.Security.Cryptography;
using System.Text;

namespace TravelPlanningSystem.Data;

public static class PasswordHashing
{
    public static string Hash(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
