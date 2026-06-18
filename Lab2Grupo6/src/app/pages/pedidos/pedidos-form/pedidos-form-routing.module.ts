import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PedidosFormPage } from './pedidos-form.page';

const routes: Routes = [{ path: '', component: PedidosFormPage }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PedidosFormPageRoutingModule {}
