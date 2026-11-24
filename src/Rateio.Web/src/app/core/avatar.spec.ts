import { corDoAvatar, iniciais } from './avatar';

describe('avatar', () => {
  it('usa a primeira e a última inicial', () => {
    expect(iniciais('Ana Souza Lima')).toBe('AL');
    expect(iniciais('caio')).toBe('CA');
  });

  it('a mesma pessoa tem sempre a mesma cor', () => {
    expect(corDoAvatar('Ana')).toBe(corDoAvatar('ana'));
  });
});
