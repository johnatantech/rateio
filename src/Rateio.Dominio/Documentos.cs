namespace Rateio.Dominio;

/// <summary>Validação de CPF e CNPJ (numérico e alfanumérico) pelo dígito verificador.</summary>
public static class Documentos
{
    public static bool CpfValido(string digitos)
    {
        if (digitos.Length != 11 || !digitos.All(char.IsAsciiDigit) || digitos.Distinct().Count() == 1)
            return false;

        for (var tamanho = 9; tamanho <= 10; tamanho++)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++)
                soma += (digitos[i] - '0') * (tamanho + 1 - i);
            if (soma * 10 % 11 % 10 != digitos[tamanho] - '0')
                return false;
        }
        return true;
    }

    /// <summary>
    /// Aceita o CNPJ numérico e o alfanumérico (em vigor desde julho de 2026).
    /// Cada caractere vale seu código ASCII menos 48, o que mantém os CNPJs numéricos compatíveis.
    /// </summary>
    public static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
            return false;
        if (!cnpj[..12].All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)) || !cnpj[12..].All(char.IsAsciiDigit))
            return false;

        var dv1 = DigitoCnpj(cnpj[..12]);
        var dv2 = DigitoCnpj(cnpj[..12] + dv1);
        return cnpj[12] == dv1 && cnpj[13] == dv2;
    }

    private static char DigitoCnpj(string baseCnpj)
    {
        int[] pesos = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var deslocamento = pesos.Length - baseCnpj.Length;
        var soma = 0;
        for (var i = 0; i < baseCnpj.Length; i++)
            soma += (baseCnpj[i] - 48) * pesos[i + deslocamento];
        var resto = soma % 11;
        return resto < 2 ? '0' : (char)('0' + (11 - resto));
    }
}
