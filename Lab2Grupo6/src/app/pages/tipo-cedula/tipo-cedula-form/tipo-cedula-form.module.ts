import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { TipoCedulaFormPageRoutingModule } from './tipo-cedula-form-routing.module';
import { TipoCedulaFormPage } from './tipo-cedula-form.page';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, IonicModule, TipoCedulaFormPageRoutingModule],
  declarations: [TipoCedulaFormPage],
})
export class TipoCedulaFormPageModule {}
