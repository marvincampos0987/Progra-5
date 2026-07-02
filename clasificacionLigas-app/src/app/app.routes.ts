import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'clasification-table',
    loadComponent: () => import('./pages/clasification-table/clasification-table.page').then( m => m.ClasificationTablePage)
  },
  {
    path: '',
    redirectTo: 'clasification-table',
    pathMatch: 'full',
  },
];
