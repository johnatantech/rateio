import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="topo">
      <a routerLink="/" class="marca" aria-label="Rateio, página inicial">
        <svg viewBox="0 0 32 32" width="28" height="28" aria-hidden="true">
          <circle cx="16" cy="16" r="15" fill="var(--verde)" />
          <path d="M16 1 A15 15 0 0 1 31 16 L16 16 Z" fill="var(--framboesa)" />
          <circle cx="16" cy="16" r="6" fill="var(--papel)" />
        </svg>
        Rateio
      </a>
    </header>
    <main class="conteudo">
      <router-outlet />
    </main>
    <footer class="rodape">
      O Rateio não movimenta dinheiro: ele gera o Pix, e cada pessoa paga pelo app do próprio banco.
    </footer>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100dvh; }
    .topo {
      max-width: var(--largura); width: 100%; margin: 0 auto;
      padding: calc(1rem + env(safe-area-inset-top, 0px)) 1.25rem 0.5rem;
    }
    .marca {
      display: inline-flex; align-items: center; gap: 0.55rem;
      font-family: var(--fonte-titulo); font-weight: 700; font-size: 1.35rem;
      color: var(--tinta); text-decoration: none;
    }
    .conteudo { flex: 1; width: 100%; max-width: var(--largura); margin: 0 auto; padding: 0.75rem 1.25rem 3rem; }
    .rodape {
      max-width: var(--largura); width: 100%; margin: 0 auto; padding: 1.25rem;
      font-size: 0.875rem; color: var(--tinta-suave); border-top: 1px solid var(--linha);
    }
  `,
})
export class App {}
