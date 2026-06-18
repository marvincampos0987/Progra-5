import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { ProductosFormPageRoutingModule } from './productos-form-routing.module';
import { ProductosFormPage } from './productos-form.page';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, IonicModule, ProductosFormPageRoutingModule],
  declarations: [ProductosFormPage],
})
export class ProductosFormPageModule {}
