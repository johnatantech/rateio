using System.Globalization;
using System.Text;

namespace Rateio.Dominio;

/// <summary>
/// Gera e lê o "Pix copia e cola" estático (BR Code), no padrão EMV QRCPS-MPM
/// definido no Manual de Padrões para Iniciação do Pix do Banco Central.
///
/// Cada campo é ID (2 dígitos) + tamanho (2 dígitos) + valor. O último campo (63) é um
/// CRC16-CCITT (polinômio 0x1021, valor inicial 0xFFFF) calculado sobre todo o resto.
/// O mesmo texto vira o QR Code.
/// </summary>
public static class PixCopiaECola
{
    private const string GuiPix = "br.gov.bcb.pix";
    private const int TamanhoMaximoNome = 25;
    private const int TamanhoMaximoCidade = 15;
    private const int TamanhoMaximoTxid = 25;

    /// <param name="chave">Chave Pix de quem recebe.</param>
    /// <param name="nomeRecebedor">Nome de quem recebe (até 25 caracteres, sem acentos).</param>
    /// <param name="cidade">Cidade de quem recebe (até 15 caracteres, sem acentos).</param>
    /// <param name="valor">Valor fixo; sem valor, quem paga digita no app do banco.</param>
    /// <param name="descricao">Mensagem opcional exibida ao pagador.</param>
    /// <param name="txid">Identificador alfanumérico da cobrança; "***" quando não há.</param>
    public static string Gerar(ChavePix chave, string nomeRecebedor, string cidade,
                               Dinheiro? valor = null, string? descricao = null, string txid = "***")
    {
        ArgumentNullException.ThrowIfNull(chave);
        if (valor is { EhPositivo: false })
            throw new ArgumentException("O valor do Pix precisa ser maior que zero.", nameof(valor));

        var nome = Limpar(nomeRecebedor, TamanhoMaximoNome);
        var cidadeLimpa = Limpar(cidade, TamanhoMaximoCidade);
        if (nome.Length == 0) throw new ArgumentException("O nome do recebedor é obrigatório.", nameof(nomeRecebedor));
        if (cidadeLimpa.Length == 0) throw new ArgumentException("A cidade é obrigatória.", nameof(cidade));

        if (txid != "***" && (txid.Length > TamanhoMaximoTxid || !txid.All(char.IsAsciiLetterOrDigit)))
            throw new ArgumentException("O txid deve ter até 25 caracteres alfanuméricos.", nameof(txid));

        // Campo 26: dados da conta (GUI + chave + descrição), limitado a 99 caracteres
        var conta = Campo("00", GuiPix) + Campo("01", chave.Valor);
        if (!string.IsNullOrWhiteSpace(descricao))
        {
            var espaco = 99 - conta.Length - 4;
            var texto = Limpar(descricao, espaco);
            if (texto.Length > 0)
                conta += Campo("02", texto);
        }

        var sb = new StringBuilder()
            .Append(Campo("00", "01"))                  // versão do payload
            .Append(Campo("26", conta))                 // dados da conta Pix
            .Append(Campo("52", "0000"))                // categoria do estabelecimento
            .Append(Campo("53", "986"));                // moeda: real (ISO 4217)

        if (valor is { } v)
            sb.Append(Campo("54", v.ParaPix()));        // valor

        sb.Append(Campo("58", "BR"))                    // país
          .Append(Campo("59", nome))                    // nome do recebedor
          .Append(Campo("60", cidadeLimpa))             // cidade
          .Append(Campo("62", Campo("05", txid)))       // dados adicionais: txid
          .Append("6304");                              // CRC: ID + tamanho entram no cálculo

        var semCrc = sb.ToString();
        return semCrc + Crc16(semCrc);
    }

    /// <summary>Gera o Pix copia e cola de uma transferência sugerida pelo grupo.</summary>
    public static string GerarParaTransferencia(Transferencia transferencia, string cidade, string? descricao = null)
    {
        var chave = transferencia.Para.Chave
            ?? throw new InvalidOperationException($"{transferencia.Para.Nome} não cadastrou uma chave Pix.");
        return Gerar(chave, transferencia.Para.Nome, cidade, transferencia.Valor, descricao);
    }

    /// <summary>Lê um Pix copia e cola, conferindo o CRC. Útil para validar códigos colados.</summary>
    public static DadosPix Ler(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 8)
            throw new FormatException("Código Pix vazio ou curto demais.");

        codigo = codigo.Trim();
        var crcInformado = codigo[^4..];
        var crcCalculado = Crc16(codigo[..^4]);
        if (!crcInformado.Equals(crcCalculado, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"CRC inválido: informado {crcInformado}, esperado {crcCalculado}.");

        var campos = LerCampos(codigo);
        if (!campos.TryGetValue("26", out var contaBruta))
            throw new FormatException("Código sem dados de conta Pix (campo 26).");

        var conta = LerCampos(contaBruta);
        if (!conta.TryGetValue("00", out var gui) || !gui.Equals(GuiPix, StringComparison.OrdinalIgnoreCase))
            throw new FormatException("O código não é um Pix (GUI diferente de br.gov.bcb.pix).");

        var adicionais = campos.TryGetValue("62", out var bruto) ? LerCampos(bruto) : [];

        return new DadosPix(
            Chave: conta.GetValueOrDefault("01") ?? throw new FormatException("Código sem chave Pix."),
            NomeRecebedor: campos.GetValueOrDefault("59") ?? "",
            Cidade: campos.GetValueOrDefault("60") ?? "",
            Valor: campos.TryGetValue("54", out var valor)
                ? Dinheiro.DeReais(decimal.Parse(valor, CultureInfo.InvariantCulture))
                : null,
            Descricao: conta.GetValueOrDefault("02"),
            Txid: adicionais.GetValueOrDefault("05"));
    }

    // ------------------------------------------------------------ auxiliares

    private static string Campo(string id, string valor)
    {
        if (valor.Length > 99)
            throw new ArgumentException($"O campo {id} excede 99 caracteres.");
        return id + valor.Length.ToString("00", CultureInfo.InvariantCulture) + valor;
    }

    private static Dictionary<string, string> LerCampos(string texto)
    {
        var campos = new Dictionary<string, string>();
        var i = 0;
        while (i < texto.Length)
        {
            if (i + 4 > texto.Length || !int.TryParse(texto.AsSpan(i + 2, 2), out var tamanho) || i + 4 + tamanho > texto.Length)
                throw new FormatException($"Estrutura inválida na posição {i}.");
            campos[texto.Substring(i, 2)] = texto.Substring(i + 4, tamanho);
            i += 4 + tamanho;
        }
        return campos;
    }

    /// <summary>Remove acentos e caracteres fora do ASCII imprimível e corta no tamanho máximo.</summary>
    private static string Limpar(string texto, int tamanhoMaximo)
    {
        var semAcento = new string(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(c => c is >= ' ' and <= '~')
            .ToArray());
        var limpo = string.Join(' ', semAcento.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpo.Length <= tamanhoMaximo ? limpo : limpo[..tamanhoMaximo].TrimEnd();
    }

    /// <summary>CRC16-CCITT-FALSE: polinômio 0x1021, valor inicial 0xFFFF, resultado em 4 dígitos hexadecimais.</summary>
    internal static string Crc16(string dados)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(dados))
        {
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}

public sealed record DadosPix(string Chave, string NomeRecebedor, string Cidade, Dinheiro? Valor, string? Descricao, string? Txid);
