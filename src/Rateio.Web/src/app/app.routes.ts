import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/inicio/inicio').then(m => m.Inicio), title: 'Rateio' },
  { path: 'g/:id', loadComponent: () => import('./pages/grupo/grupo').then(m => m.GrupoPagina), title: 'Rateio' },
  { path: '**', redirectTo: '' },
];
