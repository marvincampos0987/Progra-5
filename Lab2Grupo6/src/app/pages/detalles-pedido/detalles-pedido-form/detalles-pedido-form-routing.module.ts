import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DetallesPedidoFormPage } from './detalles-pedido-form.page';

const routes: Routes = [{ path: '', component: DetallesPedidoFormPage }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DetallesPedidoFormPageRoutingModule {}
