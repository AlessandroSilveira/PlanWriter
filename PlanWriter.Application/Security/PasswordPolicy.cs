using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanWriter.Application.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    private static readonly HashSet<string> BlockedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "admin123",
        "password",
        "password123",
        "password123!",
        "123456",
        "12345678",
        "123456789",
        "1234567890",
        "qwerty",
        "letmein",
        "senha",
        "senha123",
        "planwriter"
    };

    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Senha é obrigatória.";
        }

        var value = password.Trim();

        if (value.Length < MinimumLength)
        {
            return $"A senha deve ter pelo menos {MinimumLength} caracteres.";
        }

        if (!value.Any(char.IsUpper))
        {
            return "A senha deve conter ao menos uma letra maiúscula.";
        }

        if (!value.Any(char.IsLower))
        {
            return "A senha deve conter ao menos uma letra minúscula.";
        }

        if (!value.Any(char.IsDigit))
        {
            return "A senha deve conter ao menos um número.";
        }

        if (!value.Any(ch => char.IsPunctuation(ch) || char.IsSymbol(ch)))
        {
            return "A senha deve conter ao menos um símbolo.";
        }

        if (BlockedPasswords.Contains(value))
        {
            return "Esta senha é muito comum. Escolha uma senha mais forte.";
        }

        return null;
    }
}
