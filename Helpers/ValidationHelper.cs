using API.Constants;
using System.Text.RegularExpressions;

namespace API.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > ApiConstants.Validation.MaxEmailLength)
                return false;

            return Regex.IsMatch(email, ApiConstants.RegexPatterns.Email);
        }

        public static bool IsValidCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            return Regex.IsMatch(cpf, ApiConstants.RegexPatterns.Cpf);
        }

        public static bool IsValidCnpj(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            return Regex.IsMatch(cnpj, ApiConstants.RegexPatterns.Cnpj);
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= ApiConstants.Validation.MinPasswordLength &&
                   password.Length <= ApiConstants.Validation.MaxPasswordLength;
        }

        public static bool IsValidPageSize(int pageSize)
        {
            return pageSize >= ApiConstants.Defaults.MinPageSize &&
                   pageSize <= ApiConstants.Defaults.MaxPageSize;
        }

        public static (int page, int pageSize) ValidatePagination(int page, int pageSize)
        {
            var validPage = Math.Max(1, page);
            var validPageSize = pageSize <= 0 ? ApiConstants.Defaults.PageSize
                              : Math.Min(pageSize, ApiConstants.Defaults.MaxPageSize);

            return (validPage, validPageSize);
        }
    }
}