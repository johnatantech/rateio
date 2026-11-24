import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Participante } from '../../core/modelos';
import { Despesas } from './despesas';
import { GrupoEstado } from './grupo-estado';

const pessoas: Participante[] = [
  { id: 'a', nome: 'Ana', tipoChavePix: null, chavePixMascarada: null },
  { id: 'b', nome: 'Bia', tipoChavePix: null, chavePixMascarada: null },
];

describe('Despesas', () => {
  // Acessa membros protegidos do componente para testar a lógica do formulário
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let componente: any;
  const adicionarDespesa = vi.fn();

  beforeEach(() => {
    adicionarDespesa.mockReset().mockResolvedValue(undefined);
    const estado = { participantes: signal(pessoas), despesas: signal([]), adicionarDespesa };
    TestBed.configureTestingModule({
      imports: [Despesas],
      providers: [{ provide: GrupoEstado, useValue: estado }],
    });
    componente = TestBed.createComponent(Despesas).componentInstance;
    componente.abrirFormulario();
  });

  it('avisa quanto falta na divisão por valores', () => {
    componente.valorTexto.set('100,00');
    componente.tipo.set('Valores');
    componente.entradas.set({ a: '60', b: '30' });
    expect(componente.conferencia()).toEqual({ ok: false, texto: 'Faltam R$ 10,00.' });

    componente.entradas.set({ a: '60', b: '40' });
    expect(componente.conferencia().ok).toBe(true);
  });

  it('exige percentuais somando 100%', () => {
    componente.tipo.set('Percentual');
    componente.entradas.set({ a: '70', b: '20' });
    expect(componente.conferencia().ok).toBe(false);
    componente.entradas.set({ a: '70', b: '30' });
    expect(componente.conferencia().ok).toBe(true);
  });

  it('envia a divisão igual só com quem está marcado', async () => {
    componente.descricao.set('Pizza');
    componente.valorTexto.set('80');
    componente.incluidos.set({ b: false });

    await componente.lancar(new Event('submit'));

    expect(adicionarDespesa).toHaveBeenCalledWith({
      descricao: 'Pizza',
      pagoPorId: 'a',
      valor: 80,
      divisao: { tipo: 'Igual', participantes: [{ participanteId: 'a' }] },
    });
  });

  it('não envia valor inválido', async () => {
    componente.descricao.set('Pizza');
    componente.valorTexto.set('dez reais');
    await componente.lancar(new Event('submit'));
    expect(adicionarDespesa).not.toHaveBeenCalled();
    expect(componente.erro()).toContain('valor válido');
  });
});
