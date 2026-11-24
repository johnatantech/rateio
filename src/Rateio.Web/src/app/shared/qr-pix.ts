import { Component, effect, input, signal } from '@angular/core';
import QRCode from 'qrcode';

/** Desenha o QR Code do Pix copia e cola. O mesmo texto vale para ler com a câmera ou colar no app do banco. */
@Component({
  selector: 'app-qr-pix',
  template: `
    @if (imagem(); as src) {
      <img [src]="src" [alt]="descricao()" width="200" height="200" />
    } @else {
      <div class="carregando" aria-hidden="true"></div>
    }
  `,
  styles: `
    :host { display: block; }
    img, .carregando { display: block; width: 100%; max-width: 200px; aspect-ratio: 1; border-radius: 12px; background: #fff; }
  `,
})
export class QrPix {
  readonly codigo = input.required<string>();
  readonly descricao = input('QR Code do Pix');
  protected readonly imagem = signal<string | null>(null);

  constructor() {
    effect(() => {
      const codigo = this.codigo();
      QRCode.toDataURL(codigo, {
        errorCorrectionLevel: 'M',
        margin: 2,
        width: 400,
        color: { dark: '#1d2a2bff', light: '#ffffffff' },
      }).then(src => this.imagem.set(src), () => this.imagem.set(null));
    });
  }
}
