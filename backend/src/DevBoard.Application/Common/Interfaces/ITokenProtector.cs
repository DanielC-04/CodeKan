namespace DevBoard.Application.Common.Interfaces;

public interface ITokenProtector
{
    string Protect(string value);
    string Unprotect(string value);
}
