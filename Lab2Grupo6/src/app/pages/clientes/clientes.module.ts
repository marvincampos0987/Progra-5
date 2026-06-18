import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ClientesPageRoutingModule } from './clientes-routing.module';
import { ClientesPage } from './clientes.page';

@NgModule({
  imports: [CommonModule, IonicModule, ClientesPageRoutingModule],
  declarations: [ClientesPage],
})
export class ClientesPageModule {}
