import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ESPERA_ENTRE_TENTATIVAS, EstadoApi, aguardarApiAcordar } from './estado-api';

describe('aguardarApiAcordar', () => {
  let http: HttpClient;
  let controle: HttpTestingController;
  let estado: EstadoApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([aguardarApiAcordar])),
        provideHttpClientTesting(),
        { provide: ESPERA_ENTRE_TENTATIVAS, useValue: 0 },
      ],
    });
    http = TestBed.inject(HttpClient);
    controle = TestBed.inject(HttpTestingController);
    estado = TestBed.inject(EstadoApi);
  });

  afterEach(() => controle.verify());

  const aguardarTentativa = () => new Promise(r => setTimeout(r, 5));

  it('repete leituras enquanto o servidor está acordando', async () => {
    const resposta = firstValueFrom(http.get<{ ok: boolean }>('/api/grupos/1'));

    controle.expectOne('/api/grupos/1').flush(null, { status: 503, statusText: 'Service Unavailable' });
    expect(estado.acordando()).toBe(true);
    await aguardarTentativa();
    controle.expectOne('/api/grupos/1').flush({ ok: true });

    expect(await resposta).toEqual({ ok: true });
    expect(estado.acordando()).toBe(false);
  });

  it('não repete gravações', async () => {
    const resposta = firstValueFrom(http.post('/api/grupos', {}));
    controle.expectOne('/api/grupos').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await expect(resposta).rejects.toBeTruthy();
  });

  it('não repete erros de regra de negócio', async () => {
    const resposta = firstValueFrom(http.get('/api/grupos/1'));
    controle.expectOne('/api/grupos/1').flush({ detail: 'Não encontrado' }, { status: 404, statusText: 'Not Found' });
    await expect(resposta).rejects.toBeTruthy();
    expect(estado.acordando()).toBe(false);
  });
});
