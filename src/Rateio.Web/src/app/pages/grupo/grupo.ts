import { Component, effect, inject, input, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { formatarReais } from '../../core/dinheiro';
import { Acertos } from './acertos';
import { Despesas } from './despesas';
import { GrupoEstado } from './grupo-estado';
import { Pessoas } from './pessoas';

type Aba = 'acertos' | 'despesas' | 'pessoas';

@Component({
  selector: 'app-grupo',
  imports: [RouterLink, Acertos, Despesas, Pessoas],
  providers: [GrupoEstado],
  templateUrl: './grupo.html',
  styleUrl: './grupo.scss',
})
export class GrupoPagina {
  /** Vem da rota /g/:id */
  readonly id = input.required<string>();

  protected readonly estado = inject(GrupoEstado);
  private readonly titulo = inject(Title);
  protected readonly aba = signal<Aba>('acertos');
  protected readonly linkCopiado = signal(false);
  protected readonly reais = formatarReais;

  protected readonly abas: { id: Aba; rotulo: string }[] = [
    { id: 'acertos', rotulo: 'Acertos' },
    { id: 'despesas', rotulo: 'Despesas' },
    { id: 'pessoas', rotulo: 'Pessoas' },
  ];

  constructor() {
    effect(async () => {
      await this.estado.carregar(this.id());
      const grupo = this.estado.grupo();
      if (!grupo) return;
      this.titulo.setTitle(`${grupo.nome} | Rateio`);
      // Primeira visita: leva a pessoa para o próximo passo que falta
      if (grupo.participantes.length < 2) this.aba.set('pessoas');
      else if (grupo.quantidadeDespesas === 0) this.aba.set('despesas');
    });
  }

  protected async copiarLink() {
    try {
      await navigator.clipboard.writeText(location.href);
      this.linkCopiado.set(true);
      setTimeout(() => this.linkCopiado.set(false), 2500);
    } catch {
      prompt('Copie o link do grupo:', location.href);
    }
  }
}
