import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MeusGrupos } from '../../core/meus-grupos';
import { RateioApi, ErroDaApi } from '../../core/rateio-api';

@Component({
  selector: 'app-inicio',
  imports: [RouterLink, DatePipe],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss',
})
export class Inicio {
  private readonly api = inject(RateioApi);
  private readonly router = inject(Router);
  protected readonly meusGrupos = inject(MeusGrupos);

  protected readonly nome = signal('');
  protected readonly cidade = signal('');
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected async criar(evento: Event) {
    evento.preventDefault();
    if (!this.nome().trim() || !this.cidade().trim()) {
      this.erro.set('Informe o nome do grupo e a cidade.');
      return;
    }

    this.enviando.set(true);
    this.erro.set(null);
    try {
      const grupo = await this.api.criarGrupo(this.nome().trim(), this.cidade().trim());
      this.meusGrupos.lembrar(grupo.id, grupo.nome);
      await this.router.navigate(['/g', grupo.id]);
    } catch (e) {
      this.erro.set(e instanceof ErroDaApi ? e.message : 'Não foi possível criar o grupo.');
    } finally {
      this.enviando.set(false);
    }
  }

  protected texto(evento: Event) {
    return (evento.target as HTMLInputElement).value;
  }
}
