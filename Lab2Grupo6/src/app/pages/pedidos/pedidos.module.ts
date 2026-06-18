import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { PedidosPageRoutingModule } from './pedidos-routing.module';
import { PedidosPage } from './pedidos.page';

@NgModule({
  imports: [CommonModule, IonicModule, PedidosPageRoutingModule],
  declarations: [PedidosPage],
})
export class PedidosPageModule {}
