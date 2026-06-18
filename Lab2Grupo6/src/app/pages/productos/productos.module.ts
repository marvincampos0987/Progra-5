import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ProductosPageRoutingModule } from './productos-routing.module';
import { ProductosPage } from './productos.page';

@NgModule({
  imports: [CommonModule, IonicModule, ProductosPageRoutingModule],
  declarations: [ProductosPage],
})
export class ProductosPageModule {}
