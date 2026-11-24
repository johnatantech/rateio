// Contratos da API Rateio (espelham src/Rateio.Api/Contratos)

export type TipoChavePix = 'Cpf' | 'Cnpj' | 'Email' | 'Telefone' | 'Aleatoria';
export type TipoDivisao = 'Igual' | 'Valores' | 'Percentual' | 'Cotas';
export type SituacaoSaldo = 'AReceber' | 'APagar' | 'Quite';

export interface Pessoa { id: string; nome: string; }

export interface Participante {
  id: string;
  nome: string;
  tipoChavePix: TipoChavePix | null;
  chavePixMascarada: string | null;
}

export interface Grupo {
  id: string;
  nome: string;
  cidade: string;
  criadoEm: string;
  totalGasto: number;
  quantidadeDespesas: number;
  participantes: Participante[];
}

export interface Parte { participanteId: string; nome: string; valor: number; }

export interface Despesa {
  id: string;
  descricao: string;
  pagoPor: Pessoa;
  valor: number;
  data: string;
  categoria: string | null;
  divisao: Parte[];
}

export interface Saldo { participanteId: string; nome: string; saldo: number; situacao: SituacaoSaldo; }

export interface Acerto { de: Pessoa; para: Pessoa; valor: number; pixCopiaECola: string | null; aviso: string | null; }

export interface Pagamento { id: string; de: Pessoa; para: Pessoa; valor: number; data: string; }

export interface ParteDaDivisao {
  participanteId: string;
  valor?: number;
  percentual?: number;
  cotas?: number;
}

export interface NovaDespesa {
  descricao: string;
  pagoPorId: string;
  valor: number;
  divisao: { tipo: TipoDivisao; participantes: ParteDaDivisao[] };
  categoria?: string;
}
