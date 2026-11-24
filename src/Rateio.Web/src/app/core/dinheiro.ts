const reais = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

/** 1234.5 → "R$ 1.234,50" */
export function formatarReais(valor: number): string {
  return reais.format(valor).replace(/\u00a0/g, ' ');
}

/**
 * Lê valores digitados do jeito brasileiro ou com ponto:
 * "12,50", "1.234,56", "R$ 90", "45.5". Retorna null se não for um valor válido com até 2 casas.
 */
export function lerValor(texto: string): number | null {
  let limpo = texto.replace(/R\$|\s/g, '');
  if (!limpo) return null;

  if (limpo.includes(',')) {
    limpo = limpo.replace(/\./g, '').replace(',', '.');
  } else if (/^\d{1,3}(\.\d{3})+$/.test(limpo)) {
    limpo = limpo.replace(/\./g, '');   // "1.234" sem vírgula = milhar
  }

  if (!/^\d+(\.\d{1,2})?$/.test(limpo)) return null;
  return Math.round(Number(limpo) * 100) / 100;
}

/** Soma em centavos para não acumular erro de ponto flutuante. */
export function somar(valores: number[]): number {
  return valores.reduce((total, v) => total + Math.round(v * 100), 0) / 100;
}
