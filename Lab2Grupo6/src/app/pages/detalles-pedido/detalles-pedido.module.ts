import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { DetallesPedidoPageRoutingModule } from './detalles-pedido-routing.module';
import { DetallesPedidoPage } from './detalles-pedido.page';

@NgModule({
  imports: [CommonModule, IonicModule, DetallesPedidoPageRoutingModule],
  declarations: [DetallesPedidoPage],
})
export class DetallesPedidoPageModule {}
