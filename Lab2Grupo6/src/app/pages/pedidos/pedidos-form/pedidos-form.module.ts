import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { PedidosFormPageRoutingModule } from './pedidos-form-routing.module';
import { PedidosFormPage } from './pedidos-form.page';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, IonicModule, PedidosFormPageRoutingModule],
  declarations: [PedidosFormPage],
})
export class PedidosFormPageModule {}
