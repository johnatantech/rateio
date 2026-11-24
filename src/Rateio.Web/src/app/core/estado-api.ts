import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, InjectionToken, inject, signal } from '@angular/core';
import { finalize, retry, timer } from 'rxjs';

/**
 * A API da demo roda no plano gratuito do Render, que hiberna após um tempo sem uso.
 * A primeira requisição depois disso pode levar perto de um minuto enquanto o servidor acorda.
 */
@Injectable({ providedIn: 'root' })
export class EstadoApi {
  /** Verdadeiro enquanto o app está esperando a API acordar */
  readonly acordando = signal(false);
}

/** Intervalo entre as tentativas, em milissegundos (configurável para os testes). */
export const ESPERA_ENTRE_TENTATIVAS = new InjectionToken<number>('ESPERA_ENTRE_TENTATIVAS', {
  factory: () => 5000,
});

const MAXIMO_DE_TENTATIVAS = 15;   // 15 × 5 s ≈ 75 s, o suficiente para o Render acordar
const STATUS_DE_SERVIDOR_DORMINDO = [0, 502, 503, 504];

/**
 * Repete leituras (GET) enquanto o servidor responde como se estivesse dormindo.
 * Gravações não são repetidas automaticamente, para nunca duplicar uma despesa.
 */
export const aguardarApiAcordar: HttpInterceptorFn = (requisicao, proximo) => {
  if (requisicao.method !== 'GET') return proximo(requisicao);

  const estado = inject(EstadoApi);
  const espera = inject(ESPERA_ENTRE_TENTATIVAS);
  let tentou = false;

  return proximo(requisicao).pipe(
    retry({
      count: MAXIMO_DE_TENTATIVAS,
      delay: erro => {
        if (!(erro instanceof HttpErrorResponse) || !STATUS_DE_SERVIDOR_DORMINDO.includes(erro.status)) throw erro;
        tentou = true;
        estado.acordando.set(true);
        return timer(espera);
      },
    }),
    finalize(() => { if (tentou) estado.acordando.set(false); }),
  );
};
