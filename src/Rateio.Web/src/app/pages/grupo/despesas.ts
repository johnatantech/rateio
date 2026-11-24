import { DatePipe } from '@angular/common';
import { Component, computed, inject, output, signal } from '@angular/core';
import { formatarReais, lerValor, somar } from '../../core/dinheiro';
import { Despesa, NovaDespesa, ParteDaDivisao, TipoDivisao } from '../../core/modelos';
import { ErroDaApi } from '../../core/rateio-api';
import { GrupoEstado } from './grupo-estado';

@Component({
  selector: 'app-despesas',
  imports: [DatePipe],
  templateUrl: './despesas.html',
  styleUrl: './despesas.scss',
})
export class Despesas {
  protected readonly estado = inject(GrupoEstado);
  readonly irPara = output<'acertos' | 'despesas' | 'pessoas'>();
  protected readonly reais = formatarReais;

  protected readonly tipos: { id: TipoDivisao; rotulo: string }[] = [
    { id: 'Igual', rotulo: 'Igual' },
    { id: 'Valores', rotulo: 'Valores' },
    { id: 'Percentual', rotulo: 'Percentual' },
    { id: 'Cotas', rotulo: 'Cotas' },
  ];

  protected readonly formularioAberto = signal(false);
  protected readonly descricao = signal('');
  protected readonly valorTexto = signal('');
  protected readonly pagoPorId = signal('');
  protected readonly tipo = signal<TipoDivisao>('Igual');
  /** Por participante: incluído na divisão igual, ou o texto digitado em valor/percentual/cotas */
  protected readonly incluidos = signal<Record<string, boolean>>({});
  protected readonly entradas = signal<Record<string, string>>({});
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly valor = computed(() => lerValor(this.valorTexto()));

  /** Conferência ao vivo da divisão, antes de enviar */
  protected readonly conferencia = computed(() => {
    const total = this.valor();
    const pessoas = this.estado.participantes();
    const entradas = this.entradas();

    switch (this.tipo()) {
      case 'Igual': {
        const n = pessoas.filter(p => this.incluidos()[p.id] !== false).length;
        return n === 0
          ? { ok: false, texto: 'Escolha ao menos uma pessoa.' }
          : { ok: true, texto: n === 1 ? 'Uma pessoa paga tudo.' : `Dividido igualmente entre ${n} pessoas.` };
      }
      case 'Valores': {
        const valores = pessoas.map(p => lerValor(entradas[p.id] || '0'));
        if (valores.some(v => v === null)) return { ok: false, texto: 'Há um valor inválido.' };
        if (total === null) return { ok: false, texto: 'Informe o valor da despesa.' };
        const diferenca = somar([total, -somar(valores as number[])]);
        if (diferenca === 0) return { ok: true, texto: 'Os valores fecham com o total.' };
        return { ok: false, texto: diferenca > 0 ? `Faltam ${this.reais(diferenca)}.` : `Passou ${this.reais(-diferenca)}.` };
      }
      case 'Percentual': {
        const soma = pessoas.reduce((t, p) => t + (Number((entradas[p.id] || '0').replace(',', '.')) || 0), 0);
        const arredondada = Math.round(soma * 100) / 100;
        return arredondada === 100
          ? { ok: true, texto: 'Os percentuais somam 100%.' }
          : { ok: false, texto: `Os percentuais somam ${arredondada.toLocaleString('pt-BR')}%, precisam somar 100%.` };
      }
      case 'Cotas': {
        const soma = pessoas.reduce((t, p) => t + (parseInt(entradas[p.id] ?? '1', 10) || 0), 0);
        return soma > 0
          ? { ok: true, texto: `${soma} ${soma === 1 ? 'cota' : 'cotas'} no total.` }
          : { ok: false, texto: 'Informe ao menos uma cota.' };
      }
    }
  });

  protected abrirFormulario() {
    const pessoas = this.estado.participantes();
    this.pagoPorId.set(pessoas[0]?.id ?? '');
    this.incluidos.set({});
    this.entradas.set({});
    this.tipo.set('Igual');
    this.erro.set(null);
    this.formularioAberto.set(true);
    setTimeout(() => document.getElementById('descricao')?.focus());
  }

  protected alternar(id: string, evento: Event) {
    this.incluidos.update(atual => ({ ...atual, [id]: (evento.target as HTMLInputElement).checked }));
  }

  protected definirEntrada(id: string, evento: Event) {
    this.entradas.update(atual => ({ ...atual, [id]: (evento.target as HTMLInputElement).value }));
  }

  protected entrada(id: string) {
    return this.entradas()[id] ?? (this.tipo() === 'Cotas' ? '1' : '');
  }

  protected async lancar(evento: Event) {
    evento.preventDefault();
    const valor = this.valor();
    if (!this.descricao().trim()) return this.erro.set('Descreva a despesa.');
    if (valor === null || valor <= 0) return this.erro.set('Informe um valor válido, como 45,90.');
    if (!this.conferencia().ok) return this.erro.set(this.conferencia().texto);

    const despesa: NovaDespesa = {
      descricao: this.descricao().trim(),
      pagoPorId: this.pagoPorId(),
      valor,
      divisao: { tipo: this.tipo(), participantes: this.montarPartes() },
    };

    this.enviando.set(true);
    this.erro.set(null);
    try {
      await this.estado.adicionarDespesa(despesa);
      this.descricao.set('');
      this.valorTexto.set('');
      this.formularioAberto.set(false);
    } catch (e) {
      this.erro.set(e instanceof ErroDaApi ? e.message : 'Não foi possível lançar a despesa.');
    } finally {
      this.enviando.set(false);
    }
  }

  protected async remover(despesa: Despesa) {
    if (!confirm(`Remover "${despesa.descricao}" de ${this.reais(despesa.valor)}? Os saldos serão recalculados.`)) return;
    try {
      await this.estado.removerDespesa(despesa.id);
    } catch (e) {
      this.erro.set(e instanceof ErroDaApi ? e.message : 'Não foi possível remover a despesa.');
    }
  }

  protected nomesDaDivisao(despesa: Despesa) {
    const nomes = despesa.divisao.filter(p => p.valor > 0).map(p => p.nome);
    if (nomes.length === this.estado.participantes().length && nomes.length > 2) return 'todos';
    return nomes.length <= 1 ? nomes.join('') : `${nomes.slice(0, -1).join(', ')} e ${nomes.at(-1)}`;
  }

  protected texto(evento: Event) {
    return (evento.target as HTMLInputElement | HTMLSelectElement).value;
  }

  private montarPartes(): ParteDaDivisao[] {
    const pessoas = this.estado.participantes();
    const entradas = this.entradas();
    switch (this.tipo()) {
      case 'Igual':
        return pessoas.filter(p => this.incluidos()[p.id] !== false).map(p => ({ participanteId: p.id }));
      case 'Valores':
        return pessoas.map(p => ({ participanteId: p.id, valor: lerValor(entradas[p.id] || '0') ?? 0 }));
      case 'Percentual':
        return pessoas.map(p => ({ participanteId: p.id, percentual: Number((entradas[p.id] || '0').replace(',', '.')) || 0 }));
      case 'Cotas':
        return pessoas.map(p => ({ participanteId: p.id, cotas: parseInt(entradas[p.id] ?? '1', 10) || 0 }));
    }
  }
}
