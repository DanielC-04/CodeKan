using DevBoard.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace DevBoard.Infrastructure.Security;

public sealed class DataProtectionTokenProtector(IDataProtectionProvider dataProtectionProvider) : ITokenProtector
{
    private const string ProtectorPurpose = "DevBoard.GitHubToken.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("GitHub token is required.");
        }

        return _protector.Protect(value.Trim());
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Encrypted GitHub token is required.");
        }

        return _protector.Unprotect(value);
    }
}
