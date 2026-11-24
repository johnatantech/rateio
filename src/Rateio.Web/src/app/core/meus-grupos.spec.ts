import { TestBed } from '@angular/core/testing';
import { MeusGrupos } from './meus-grupos';

describe('MeusGrupos', () => {
  beforeEach(() => localStorage.clear());

  it('lembra os grupos abertos, o mais recente primeiro, sem repetir', () => {
    const servico = TestBed.inject(MeusGrupos);
    servico.lembrar('1', 'Casa');
    servico.lembrar('2', 'Viagem');
    servico.lembrar('1', 'Casa');

    expect(servico.lista().map(g => g.id)).toEqual(['1', '2']);
    expect(JSON.parse(localStorage.getItem('rateio:grupos')!)).toHaveLength(2);
  });

  it('esquece um grupo', () => {
    const servico = TestBed.inject(MeusGrupos);
    servico.lembrar('1', 'Casa');
    servico.esquecer('1');
    expect(servico.lista()).toEqual([]);
  });

  it('ignora dados corrompidos no armazenamento', () => {
    localStorage.setItem('rateio:grupos', '{isso não é json');
    expect(TestBed.inject(MeusGrupos).lista()).toEqual([]);
  });
});
