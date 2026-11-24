const TONS = ['#cfe8df', '#f3d9c2', '#d8dcf2', '#f1d3dd', '#e3e8c4', '#cfe3ee', '#ead7ef', '#f2e3b8'];

export function iniciais(nome: string): string {
  const partes = nome.trim().split(/\s+/);
  const letras = partes.length > 1 ? partes[0][0] + partes[partes.length - 1][0] : partes[0].slice(0, 2);
  return letras.toUpperCase();
}

/** Mesma pessoa, mesma cor: a cor vem do nome, não da posição na lista. */
export function corDoAvatar(nome: string): string {
  let hash = 0;
  for (const letra of nome.toLowerCase()) hash = (hash * 31 + letra.charCodeAt(0)) >>> 0;
  return TONS[hash % TONS.length];
}
