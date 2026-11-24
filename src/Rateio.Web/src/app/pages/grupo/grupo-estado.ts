import { Injectable, computed, inject, signal } from '@angular/core';
import { Acerto, Despesa, Grupo, NovaDespesa, Pagamento, Saldo } from '../../core/modelos';
import { MeusGrupos } from '../../core/meus-grupos';
import { ErroDaApi, RateioApi } from '../../core/rateio-api';

/**
 * Estado da página do grupo, compartilhado pelas abas. Depois de qualquer alteração,
 * recarrega tudo da API: saldos e acertos são sempre calculados pelo servidor, nunca no navegador.
 */
@Injectable()
export class GrupoEstado {
  private readonly api = inject(RateioApi);
  private readonly meusGrupos = inject(MeusGrupos);

  readonly grupo = signal<Grupo | null>(null);
  readonly despesas = signal<Despesa[]>([]);
  readonly saldos = signal<Saldo[]>([]);
  readonly acertos = signal<Acerto[]>([]);
  readonly pagamentos = signal<Pagamento[]>([]);
  readonly carregando = signal(true);
  readonly erroAoCarregar = signal<string | null>(null);

  readonly participantes = computed(() => this.grupo()?.participantes ?? []);

  private id = '';

  async carregar(id: string) {
    this.id = id;
    this.carregando.set(true);
    this.erroAoCarregar.set(null);
    try {
      await this.recarregar();
      const grupo = this.grupo();
      if (grupo) this.meusGrupos.lembrar(grupo.id, grupo.nome);
    } catch (e) {
      this.erroAoCarregar.set(e instanceof ErroDaApi ? e.message : 'Não foi possível abrir o grupo.');
      if (e instanceof ErroDaApi && e.status === 404) this.meusGrupos.esquecer(id);
    } finally {
      this.carregando.set(false);
    }
  }

  async recarregar() {
    const [grupo, despesas, saldos, acertos, pagamentos] = await Promise.all([
      this.api.obterGrupo(this.id),
      this.api.listarDespesas(this.id),
      this.api.saldos(this.id),
      this.api.acertos(this.id),
      this.api.pagamentos(this.id),
    ]);
    this.grupo.set(grupo);
    this.despesas.set([...despesas].reverse());   // mais recentes primeiro
    this.saldos.set(saldos);
    this.acertos.set(acertos);
    this.pagamentos.set([...pagamentos].reverse());
  }

  async adicionarParticipante(nome: string, chavePix?: string) {
    await this.api.adicionarParticipante(this.id, nome, chavePix);
    await this.recarregar();
  }

  async adicionarDespesa(despesa: NovaDespesa) {
    await this.api.adicionarDespesa(this.id, despesa);
    await this.recarregar();
  }

  async removerDespesa(despesaId: string) {
    await this.api.removerDespesa(this.id, despesaId);
    await this.recarregar();
  }

  async marcarComoPago(acerto: Acerto) {
    await this.api.registrarPagamento(this.id, acerto.de.id, acerto.para.id, acerto.valor);
    await this.recarregar();
  }
}
