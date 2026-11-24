import { formatarReais, lerValor, somar } from './dinheiro';

describe('lerValor', () => {
  it.each([
    ['12,50', 12.5],
    ['1.234,56', 1234.56],
    ['R$ 90', 90],
    ['45.5', 45.5],
    ['1.234', 1234],
    [' 0,05 ', 0.05],
  ])('lê "%s" como %d', (texto, esperado) => {
    expect(lerValor(texto)).toBe(esperado);
  });

  it.each(['', 'abc', '10,005', '1,2,3', '-5'])('recusa "%s"', texto => {
    expect(lerValor(texto)).toBeNull();
  });
});

describe('formatarReais', () => {
  it('usa o formato brasileiro', () => {
    expect(formatarReais(1234.5)).toBe('R$ 1.234,50');
  });
});

describe('somar', () => {
  it('soma em centavos sem erro de ponto flutuante', () => {
    expect(somar([0.1, 0.2])).toBe(0.3);
    expect(somar([33.34, 33.33, 33.33])).toBe(100);
  });
});
