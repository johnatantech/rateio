using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Rateio.Api;

/// <summary>
/// Converte as exceções de regra de negócio do domínio em respostas ProblemDetails (RFC 9457):
/// ArgumentException → 400 (dado inválido); InvalidOperationException → 422 (regra violada).
/// Qualquer outro erro vira 500 sem expor detalhes internos.
/// </summary>
public sealed class TratadorDeErros(IProblemDetailsService problemDetails, ILogger<TratadorDeErros> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception erro, CancellationToken ct)
    {
        var (status, titulo) = erro switch
        {
            ArgumentException or FormatException => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "Operação não permitida"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado"),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(erro, "Erro não tratado em {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);

        contexto.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = erro,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = status == StatusCodes.Status500InternalServerError ? null : MensagemLimpa(erro),
            },
        });
    }

    /// <summary>Remove o sufixo "(Parameter 'x')" que o .NET acrescenta em ArgumentException.</summary>
    private static string MensagemLimpa(Exception erro) =>
        erro is ArgumentException { ParamName: { } nome } a ? a.Message.Replace($" (Parameter '{nome}')", "") : erro.Message;
}
