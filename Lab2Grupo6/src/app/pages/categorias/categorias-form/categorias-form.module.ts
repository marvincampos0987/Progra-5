import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { CategoriasFormPageRoutingModule } from './categorias-form-routing.module';
import { CategoriasFormPage } from './categorias-form.page';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, IonicModule, CategoriasFormPageRoutingModule],
  declarations: [CategoriasFormPage],
})
export class CategoriasFormPageModule {}
