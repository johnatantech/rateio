import { Injectable, signal } from '@angular/core';

export interface GrupoSalvo { id: string; nome: string; abertoEm: string; }

const CHAVE = 'rateio:grupos';

/**
 * Lembra os grupos abertos neste navegador. Como ainda não há login,
 * é assim que a pessoa volta para os seus grupos.
 */
@Injectable({ providedIn: 'root' })
export class MeusGrupos {
  readonly lista = signal<GrupoSalvo[]>(this.ler());

  lembrar(id: string, nome: string) {
    const atualizada = [{ id, nome, abertoEm: new Date().toISOString() }, ...this.lista().filter(g => g.id !== id)].slice(0, 12);
    this.lista.set(atualizada);
    this.gravar(atualizada);
  }

  esquecer(id: string) {
    const atualizada = this.lista().filter(g => g.id !== id);
    this.lista.set(atualizada);
    this.gravar(atualizada);
  }

  private ler(): GrupoSalvo[] {
    try {
      const bruto = localStorage.getItem(CHAVE);
      const dados = bruto ? JSON.parse(bruto) : [];
      return Array.isArray(dados) ? dados.filter(g => typeof g?.id === 'string' && typeof g?.nome === 'string') : [];
    } catch {
      return [];
    }
  }

  private gravar(lista: GrupoSalvo[]) {
    try {
      localStorage.setItem(CHAVE, JSON.stringify(lista));
    } catch {
      // Navegação privada ou armazenamento cheio: o app segue funcionando, só não lembra os grupos
    }
  }
}
