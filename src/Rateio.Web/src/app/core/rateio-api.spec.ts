import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ErroDaApi, RateioApi } from './rateio-api';

describe('RateioApi', () => {
  let api: RateioApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(RateioApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('cria um grupo', async () => {
    const promessa = api.criarGrupo('Casa', 'São Paulo');
    const req = http.expectOne('/api/grupos');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ nome: 'Casa', cidade: 'São Paulo' });
    req.flush({ id: '1', nome: 'Casa', cidade: 'São Paulo', criadoEm: '', totalGasto: 0, quantidadeDespesas: 0, participantes: [] });
    expect((await promessa).id).toBe('1');
  });

  it('envia chave Pix vazia como null', async () => {
    const promessa = api.adicionarParticipante('g1', 'Caio', '');
    const req = http.expectOne('/api/grupos/g1/participantes');
    expect(req.request.body).toEqual({ nome: 'Caio', chavePix: null });
    req.flush({ id: 'p1', nome: 'Caio', tipoChavePix: null, chavePixMascarada: null });
    await promessa;
  });

  it('usa a mensagem do ProblemDetails em caso de erro', async () => {
    const promessa = api.adicionarParticipante('g1', 'Ana');
    http.expectOne('/api/grupos/g1/participantes').flush(
      { title: 'Operação não permitida', detail: 'Já existe um participante chamado Ana no grupo.' },
      { status: 422, statusText: 'Unprocessable Entity' });

    await expect(promessa).rejects.toThrow('Já existe um participante chamado Ana no grupo.');
  });

  it('explica quando a API está fora do ar', async () => {
    const promessa = api.obterGrupo('g1');
    http.expectOne('/api/grupos/g1').error(new ProgressEvent('error'), { status: 0 });

    const erro = await promessa.catch(e => e);
    expect(erro).toBeInstanceOf(ErroDaApi);
    expect(erro.message).toContain('Sem conexão com a API');
  });

  it('inicia uma nova rodada', async () => {
    const promessa = api.iniciarNovaRodada('g1');
    const req = http.expectOne('/api/grupos/g1/nova-rodada');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'g1', nome: 'Casa', cidade: 'SP', criadoEm: '', totalGasto: 0, quantidadeDespesas: 0, participantes: [] });
    expect((await promessa).quantidadeDespesas).toBe(0);
  });

  it('mostra o motivo quando ainda há acertos pendentes', async () => {
    const promessa = api.excluirGrupo('g1');
    const req = http.expectOne('/api/grupos/g1');
    expect(req.request.method).toBe('DELETE');
    req.flush({ detail: 'Ainda falta 1 acerto para excluir o grupo. Registre os pagamentos primeiro.' },
      { status: 422, statusText: 'Unprocessable Entity' });
    await expect(promessa).rejects.toThrow('Ainda falta 1 acerto');
  });
});
