using System.Text.RegularExpressions;

namespace Rateio.Dominio;

public enum TipoChavePix { Cpf, Cnpj, Email, Telefone, Aleatoria }

/// <summary>Chave Pix validada e normalizada no formato aceito pelo Banco Central.</summary>
public sealed partial record ChavePix
{
    public TipoChavePix Tipo { get; }

    /// <summary>Valor normalizado: só dígitos (CPF/CNPJ), +55DDNNNNNNNNN (telefone), minúsculas (e-mail e aleatória).</summary>
    public string Valor { get; }

    private ChavePix(TipoChavePix tipo, string valor) => (Tipo, Valor) = (tipo, valor);

    public static ChavePix Criar(TipoChavePix tipo, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("A chave Pix não pode ser vazia.", nameof(valor));

        var normalizado = tipo switch
        {
            TipoChavePix.Cpf => NormalizarCpf(valor),
            TipoChavePix.Cnpj => NormalizarCnpj(valor),
            TipoChavePix.Email => NormalizarEmail(valor),
            TipoChavePix.Telefone => NormalizarTelefone(valor),
            TipoChavePix.Aleatoria => NormalizarAleatoria(valor),
            _ => null,
        };

        return normalizado is null
            ? throw new ArgumentException($"Chave Pix do tipo {tipo} inválida: {valor}.", nameof(valor))
            : new ChavePix(tipo, normalizado);
    }

    /// <summary>
    /// Identifica o tipo pela forma da chave. Telefone precisa começar com "+",
    /// porque 11 dígitos sem "+" podem ser CPF ou celular sem DDI.
    /// </summary>
    public static ChavePix? Identificar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        valor = valor.Trim();
        TipoChavePix? tipo = valor switch
        {
            _ when valor.Contains('@') => TipoChavePix.Email,
            _ when valor.StartsWith('+') => TipoChavePix.Telefone,
            _ when RegexUuid().IsMatch(valor) => TipoChavePix.Aleatoria,
            _ when NormalizarCpf(valor) is not null => TipoChavePix.Cpf,
            _ when NormalizarCnpj(valor) is not null => TipoChavePix.Cnpj,
            _ => null,
        };

        return tipo is null ? null : TentarCriar(tipo.Value, valor);
    }

    public static ChavePix? TentarCriar(TipoChavePix tipo, string valor)
    {
        try { return Criar(tipo, valor); }
        catch (ArgumentException) { return null; }
    }

    /// <summary>Exibição mascarada para listas, preservando privacidade (LGPD).</summary>
    public string Mascarada() => Tipo switch
    {
        TipoChavePix.Cpf => $"***.{Valor[3..6]}.{Valor[6..9]}-**",
        TipoChavePix.Cnpj => $"{Valor[..2]}.{Valor[2..5]}.***/****-{Valor[12..]}",
        TipoChavePix.Email => $"{Valor[0]}***{Valor[Valor.IndexOf('@')..]}",
        TipoChavePix.Telefone => $"{Valor[..5]}*****{Valor[^4..]}",
        _ => $"{Valor[..8]}-****-****-****-************",
    };

    public override string ToString() => Valor;

    // ------------------------------------------------------------ normalização

    private static string? NormalizarCpf(string valor)
    {
        var d = SomenteDigitos(valor);
        return Documentos.CpfValido(d) ? d : null;
    }

    private static string? NormalizarCnpj(string valor)
    {
        var s = RegexNaoAlfanumerico().Replace(valor, "").ToUpperInvariant();
        return Documentos.CnpjValido(s) ? s : null;
    }

    private static string? NormalizarEmail(string valor)
    {
        var email = valor.Trim().ToLowerInvariant();
        return email.Length <= 77 && RegexEmail().IsMatch(email) ? email : null;
    }

    private static string? NormalizarTelefone(string valor)
    {
        var d = SomenteDigitos(valor);
        if (!d.StartsWith("55"))
            d = "55" + d;
        // +55 + DDD (2) + número (8 ou 9 dígitos)
        return d.Length is 12 or 13 && d[2] != '0' ? "+" + d : null;
    }

    private static string? NormalizarAleatoria(string valor)
    {
        var uuid = valor.Trim().ToLowerInvariant();
        return RegexUuid().IsMatch(uuid) ? uuid : null;
    }

    private static string SomenteDigitos(string valor) => RegexNaoDigito().Replace(valor, "");

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex RegexUuid();

    [GeneratedRegex(@"^[a-z0-9._%+-]+@[a-z0-9-]+(\.[a-z0-9-]+)*\.[a-z]{2,}$")]
    private static partial Regex RegexEmail();

    [GeneratedRegex(@"\D")]
    private static partial Regex RegexNaoDigito();

    [GeneratedRegex(@"[^0-9A-Za-z]")]
    private static partial Regex RegexNaoAlfanumerico();
}
