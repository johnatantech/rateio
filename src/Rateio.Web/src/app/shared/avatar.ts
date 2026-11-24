import { Component, computed, input } from '@angular/core';
import { corDoAvatar, iniciais } from '../core/avatar';

@Component({
  selector: 'app-avatar',
  template: `<span class="avatar" [style.background]="cor()" aria-hidden="true">{{ letras() }}</span>`,
})
export class Avatar {
  readonly nome = input.required<string>();
  protected readonly letras = computed(() => iniciais(this.nome()));
  protected readonly cor = computed(() => corDoAvatar(this.nome()));
}
