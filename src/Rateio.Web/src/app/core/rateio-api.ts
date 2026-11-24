import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { Acerto, Despesa, Grupo, NovaDespesa, Pagamento, Participante, Saldo } from './modelos';

/**
 * Cliente da API. Em desenvolvimento, /api é encaminhado para a API .NET pelo proxy do Angular;
 * em produção, as chamadas vão direto para o endereço configurado em environment.production.ts.
 */
@Injectable({ providedIn: 'root' })
export class RateioApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/grupos`;

  /** Chamada leve para acordar o servidor enquanto a pessoa ainda está lendo a tela. */
  aquecer() {
    this.http.get(`${environment.apiUrl}/health`, { responseType: 'text' }).subscribe({ error: () => {} });
  }

  criarGrupo(nome: string, cidade: string) {
    return this.enviar<Grupo>(this.http.post<Grupo>(this.base, { nome, cidade }));
  }

  obterGrupo(id: string) {
    return this.enviar<Grupo>(this.http.get<Grupo>(`${this.base}/${id}`));
  }

  /** Apaga despesas e pagamentos, mantendo as pessoas. A API só aceita com todos quites. */
  iniciarNovaRodada(grupoId: string) {
    return this.enviar<Grupo>(this.http.post<Grupo>(`${this.base}/${grupoId}/nova-rodada`, {}));
  }

  /** Exclui o grupo inteiro. A API só aceita com todos quites. */
  excluirGrupo(grupoId: string) {
    return this.enviar<void>(this.http.delete<void>(`${this.base}/${grupoId}`));
  }

  adicionarParticipante(grupoId: string, nome: string, chavePix?: string) {
    return this.enviar<Participante>(
      this.http.post<Participante>(`${this.base}/${grupoId}/participantes`, { nome, chavePix: chavePix || null }));
  }

  listarDespesas(grupoId: string) {
    return this.enviar<Despesa[]>(this.http.get<Despesa[]>(`${this.base}/${grupoId}/despesas`));
  }

  adicionarDespesa(grupoId: string, despesa: NovaDespesa) {
    return this.enviar<Despesa>(this.http.post<Despesa>(`${this.base}/${grupoId}/despesas`, despesa));
  }

  removerDespesa(grupoId: string, despesaId: string) {
    return this.enviar<void>(this.http.delete<void>(`${this.base}/${grupoId}/despesas/${despesaId}`));
  }

  saldos(grupoId: string) {
    return this.enviar<Saldo[]>(this.http.get<Saldo[]>(`${this.base}/${grupoId}/saldos`));
  }

  acertos(grupoId: string) {
    return this.enviar<Acerto[]>(this.http.get<Acerto[]>(`${this.base}/${grupoId}/acertos`));
  }

  pagamentos(grupoId: string) {
    return this.enviar<Pagamento[]>(this.http.get<Pagamento[]>(`${this.base}/${grupoId}/pagamentos`));
  }

  registrarPagamento(grupoId: string, deId: string, paraId: string, valor: number) {
    return this.enviar<Pagamento>(
      this.http.post<Pagamento>(`${this.base}/${grupoId}/pagamentos`, { deId, paraId, valor }));
  }

  private async enviar<T>(requisicao: import('rxjs').Observable<T>): Promise<T> {
    try {
      return await firstValueFrom(requisicao);
    } catch (erro) {
      throw new ErroDaApi(mensagemDeErro(erro), erro instanceof HttpErrorResponse ? erro.status : 0);
    }
  }
}

export class ErroDaApi extends Error {
  constructor(mensagem: string, readonly status: number) {
    super(mensagem);
  }
}

/** Usa o "detail" do ProblemDetails da API; sem ele, explica o que fazer. */
export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof HttpErrorResponse) {
    if (erro.status === 0) return 'Sem conexão com a API. Confira se ela está rodando e tente de novo.';
    if (erro.status === 404) return 'Grupo não encontrado. Confira o link.';
    if (erro.status === 429) return 'Muitas alterações seguidas. Aguarde um minuto e tente de novo.';
    if ([502, 503, 504].includes(erro.status)) return 'O servidor está acordando. Tente de novo em alguns segundos.';
    const detalhe = (erro.error as { detail?: string } | null)?.detail;
    if (detalhe) return detalhe;
    if (erro.status >= 500) return 'A API encontrou um erro inesperado. Tente de novo em instantes.';
  }
  return 'Não foi possível concluir a operação.';
}
