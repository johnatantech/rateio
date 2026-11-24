import { Component, inject, output, signal } from '@angular/core';
import { ErroDaApi } from '../../core/rateio-api';
import { Avatar } from '../../shared/avatar';
import { GrupoEstado } from './grupo-estado';

@Component({
  selector: 'app-pessoas',
  imports: [Avatar],
  template: `
    <ul class="lista">
      @for (pessoa of estado.participantes(); track pessoa.id) {
        <li>
          <app-avatar [nome]="pessoa.nome" />
          <div>
            <p class="nome">{{ pessoa.nome }}</p>
            <p class="chave suave">
              {{ pessoa.chavePixMascarada ? 'Pix: ' + pessoa.chavePixMascarada : 'Sem chave Pix' }}
            </p>
          </div>
        </li>
      } @empty {
        <li class="vazio">Ninguém no grupo ainda. Comece por você.</li>
      }
    </ul>

    <form class="formulario" (submit)="adicionar($event)" novalidate>
      <h2>Adicionar pessoa</h2>
      <div>
        <label for="nome-pessoa">Nome</label>
        <input id="nome-pessoa" type="text" maxlength="60" autocomplete="off"
               [value]="nome()" (input)="nome.set(valor($event))" />
      </div>
      <div>
        <label for="chave-pix">Chave Pix <span class="suave">(opcional)</span></label>
        <input id="chave-pix" type="text" maxlength="77" autocomplete="off" inputmode="email"
               [value]="chave()" (input)="chave.set(valor($event))" />
        <p class="ajuda">CPF, CNPJ, e-mail, chave aleatória ou celular com +55 na frente. Sem chave, a pessoa recebe combinando direto.</p>
      </div>
      @if (erro(); as mensagem) { <p class="erro" role="alert">{{ mensagem }}</p> }
      <div class="acoes">
        <button class="botao" type="submit" [disabled]="enviando()">Adicionar pessoa</button>
        @if (estado.participantes().length >= 2) {
          <button class="botao-texto" type="button" (click)="irPara.emit('despesas')">Ir para despesas</button>
        }
      </div>
    </form>
  `,
  styles: `
    .lista { list-style: none; margin: 0 0 2rem; padding: 0; }
    .lista li { display: flex; align-items: center; gap: 0.85rem; padding: 0.75rem 0; border-bottom: 1px solid var(--linha); }
    .nome { font-weight: 700; }
    .chave { font-size: 0.92rem; }
    .vazio { color: var(--tinta-suave); }
    .formulario { display: grid; gap: 1rem; }
    .formulario h2 { font-size: 1.3rem; }
    .acoes { display: flex; flex-wrap: wrap; align-items: center; gap: 1rem; }
  `,
})
export class Pessoas {
  protected readonly estado = inject(GrupoEstado);
  readonly irPara = output<'acertos' | 'despesas' | 'pessoas'>();

  protected readonly nome = signal('');
  protected readonly chave = signal('');
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected async adicionar(evento: Event) {
    evento.preventDefault();
    if (!this.nome().trim()) {
      this.erro.set('Informe o nome da pessoa.');
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    try {
      await this.estado.adicionarParticipante(this.nome().trim(), this.chave().trim() || undefined);
      this.nome.set('');
      this.chave.set('');
      document.getElementById('nome-pessoa')?.focus();
    } catch (e) {
      this.erro.set(e instanceof ErroDaApi ? e.message : 'Não foi possível adicionar a pessoa.');
    } finally {
      this.enviando.set(false);
    }
  }

  protected valor(evento: Event) {
    return (evento.target as HTMLInputElement).value;
  }
}
