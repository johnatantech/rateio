# Rateio

[![CI](https://github.com/johnatantech/rateio/actions/workflows/ci.yml/badge.svg)](https://github.com/johnatantech/rateio/actions/workflows/ci.yml)

Divisor de despesas em grupo que calcula **quem deve para quem com o menor número de transferências** e gera o **Pix copia e cola** de cada acerto, com o valor exato e a chave de quem vai receber.

Feito para a casa, a viagem ou o churrasco: lança as despesas, vê os saldos, paga o Pix e pronto.

> 🚧 **Em construção.** Esta etapa entrega o **núcleo de regras de negócio** em .NET, com 351 testes. A API, o banco SQL Server e o app em Angular vêm a seguir (veja o [roteiro](#roteiro)).

## Exemplo

```csharp
var grupo = new Grupo("Churrasco");
var ana  = grupo.AdicionarParticipante("Ana", ChavePix.Criar(TipoChavePix.Email, "ana@email.com"));
var bia  = grupo.AdicionarParticipante("Bia");
var caio = grupo.AdicionarParticipante("Caio");

var carne = Dinheiro.DeReais(180m);
grupo.AdicionarDespesa(new Despesa("Carne", ana, carne, Dividir.Igual(carne, grupo.Participantes)));

var bebidas = Dinheiro.DeReais(60m);
grupo.AdicionarDespesa(new Despesa("Bebidas", bia, bebidas, Dividir.Igual(bebidas, grupo.Participantes)));

foreach (var acerto in grupo.SugerirAcertos())
{
    Console.WriteLine($"{acerto.De} paga {acerto.Valor} para {acerto.Para}");
    Console.WriteLine(PixCopiaECola.GerarParaTransferencia(acerto, "Sao Paulo", "Churrasco"));
}
// Caio paga R$ 80,00 para Ana
// Bia paga R$ 20,00 para Ana
```

## O que já funciona

| Recurso | Detalhe |
|---|---|
| **Quatro formas de dividir** | Partes iguais, por valores, por percentual e por cotas (ex.: casal = 2) |
| **Saldos** | Quanto cada pessoa pagou, consumiu e tem a receber ou a pagar |
| **Acertos simplificados** | No máximo *n − 1* transferências para *n* pessoas; dívidas em ciclo se anulam |
| **Pix copia e cola** | Padrão BR Code do Banco Central, com valor, descrição e conferência por CRC16 |
| **Leitura de Pix** | Lê e valida um código colado, recusando códigos adulterados |
| **Chaves Pix** | CPF, CNPJ (inclusive alfanumérico), e-mail, telefone e chave aleatória, com normalização e exibição mascarada |

## Decisões técnicas

- **Dinheiro em centavos inteiros (`long`).** Em `double`, 0,1 + 0,2 não é exatamente 0,3. O tipo `Dinheiro` só aceita valores em centavos exatos e rejeita frações como R$ 10,005.
- **Divisão pelo método dos maiores restos.** R$ 100,00 entre 3 pessoas vira R$ 33,34 + R$ 33,33 + R$ 33,33: cada um recebe a parte inteira e os centavos que sobram vão para as maiores frações, com desempate pela ordem. A soma sempre fecha e o resultado é determinístico.
- **Simplificação de dívidas com algoritmo guloso.** Casa repetidamente o maior devedor com o maior credor. Cada passo zera ao menos uma pessoa, o que garante no máximo *n − 1* transferências. Encontrar o mínimo absoluto é um problema NP-difícil (equivale a dividir o grupo em subconjuntos de soma zero), então o guloso é a escolha prática: rápido, previsível e com resultado próximo do ótimo.
- **BR Code implementado do zero.** Cada campo segue o formato ID + tamanho + valor do padrão EMV, e o último campo é um CRC16-CCITT (polinômio 0x1021, valor inicial 0xFFFF). O teste principal gera **exatamente o exemplo oficial do Manual de Padrões para Iniciação do Pix**, caractere por caractere.
- **Testes por propriedade.** Além dos casos fixos, 300 grupos aleatórios (com semente fixa, para serem reproduzíveis) verificam regras que precisam valer sempre: saldos somam zero, nunca passam de *n − 1* transferências, quem deve só paga, quem tem a receber só recebe, e depois dos acertos todos ficam zerados.
- **Domínio sem dependências.** A biblioteca não conhece banco, API nem interface. As regras ficam isoladas e 100% testáveis, e a API e o app só orquestram.
- **Avisos tratados como erro.** `TreatWarningsAsErrors` no `Directory.Build.props` vale para todos os projetos.

## Estrutura

```
rateio/
├── src/Rateio.Dominio/
│   ├── Dinheiro.cs                 # valor em centavos, formatação pt-BR e Pix
│   ├── ChavePix.cs                 # tipos de chave, validação, normalização e máscara
│   ├── Documentos.cs               # CPF e CNPJ (numérico e alfanumérico)
│   ├── Dividir.cs                  # igual, valores, percentual e cotas
│   ├── Despesa.cs                  # despesa, pagamento e transferência
│   ├── Grupo.cs                    # participantes, despesas, saldos e acertos
│   ├── SimplificadorDeDividas.cs   # algoritmo guloso
│   └── PixCopiaECola.cs            # geração e leitura do BR Code
├── tests/Rateio.Dominio.Tests/  # 351 testes, incluindo propriedades e exemplo do BCB
├── .devcontainer/                  # ambiente .NET 10 + SQL Server para Codespaces
├── .github/workflows/ci.yml
├── Directory.Build.props
└── Rateio.slnx
```

## Como executar

### No navegador, sem instalar nada

[![Abrir no GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/johnatantech/rateio)

O Codespace sobe com **.NET 10 e SQL Server** prontos e conectados (configuração em [`.devcontainer`](.devcontainer)). No terminal:

```bash
dotnet test
```

### Na sua máquina

Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download). Com Docker e a extensão Dev Containers do VS Code, use **Reopen in Container** para ter o mesmo ambiente do Codespace.

```bash
git clone https://github.com/johnatantech/rateio.git
cd rateio
dotnet test
```

## Roteiro

- [x] **Semana 1:** domínio (rateio, saldos, simplificação, Pix) com testes e CI
- [ ] **Semana 2:** API ASP.NET Core + SQL Server
- [ ] **Semana 3:** app Angular (grupos, despesas, saldos e QR Code)
- [ ] **Semana 4:** PWA instalável, compartilhamento por link, deploy e demo pública

## Importante

O Rateio **não movimenta dinheiro**: ele só gera o código, e o pagamento é feito no app do banco de cada pessoa. Como o Pix copia e cola é estático, o app não tem como saber se o pagamento aconteceu, por isso os acertos são marcados como pagos manualmente.

## Licença

[MIT](LICENSE)

## Autor

**Johnatan Flores** · [GitHub](https://github.com/johnatantech) · [LinkedIn](https://www.linkedin.com/in/johnatan-flores-67595a221/)
