import { Component, computed, inject, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { formatarReais } from '../../core/dinheiro';
import { Acerto } from '../../core/modelos';
import { ErroDaApi } from '../../core/rateio-api';
import { Avatar } from '../../shared/avatar';
import { QrPix } from '../../shared/qr-pix';
import { GrupoEstado } from './grupo-estado';

@Component({
  selector: 'app-acertos',
  imports: [Avatar, QrPix, DatePipe],
  templateUrl: './acertos.html',
  styleUrl: './acertos.scss',
})
export class Acertos {
  protected readonly estado = inject(GrupoEstado);
  readonly irPara = output<'acertos' | 'despesas' | 'pessoas'>();
  protected readonly reais = formatarReais;

  protected readonly copiado = signal<string | null>(null);
  protected readonly processando = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);

  /** Maior saldo em módulo: referência para o tamanho das barras */
  protected readonly maiorSaldo = computed(() =>
    Math.max(1, ...this.estado.saldos().map(s => Math.abs(s.saldo))));

  protected chave(acerto: Acerto) {
    return `${acerto.de.id}-${acerto.para.id}`;
  }

  protected largura(saldo: number) {
    return `${(Math.abs(saldo) / this.maiorSaldo()) * 100}%`;
  }

  protected async copiar(acerto: Acerto) {
    if (!acerto.pixCopiaECola) return;
    try {
      await navigator.clipboard.writeText(acerto.pixCopiaECola);
      this.copiado.set(this.chave(acerto));
      setTimeout(() => this.copiado.set(null), 2500);
    } catch {
      prompt('Copie o código Pix:', acerto.pixCopiaECola);
    }
  }

  protected async marcarComoPago(acerto: Acerto) {
    const ok = confirm(`Confirmar que ${acerto.de.nome} já pagou ${this.reais(acerto.valor)} para ${acerto.para.nome}?`);
    if (!ok) return;
    this.processando.set(this.chave(acerto));
    this.erro.set(null);
    try {
      await this.estado.marcarComoPago(acerto);
    } catch (e) {
      this.erro.set(e instanceof ErroDaApi ? e.message : 'Não foi possível registrar o pagamento.');
    } finally {
      this.processando.set(null);
    }
  }
}
