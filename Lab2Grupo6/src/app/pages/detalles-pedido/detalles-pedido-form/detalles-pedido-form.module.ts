import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { DetallesPedidoFormPageRoutingModule } from './detalles-pedido-form-routing.module';
import { DetallesPedidoFormPage } from './detalles-pedido-form.page';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, IonicModule, DetallesPedidoFormPageRoutingModule],
  declarations: [DetallesPedidoFormPage],
})
export class DetallesPedidoFormPageModule {}
