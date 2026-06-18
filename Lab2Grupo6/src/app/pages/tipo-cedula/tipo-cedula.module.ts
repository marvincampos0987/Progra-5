import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { TipoCedulaPageRoutingModule } from './tipo-cedula-routing.module';
import { TipoCedulaPage } from './tipo-cedula.page';

@NgModule({
  imports: [CommonModule, IonicModule, TipoCedulaPageRoutingModule],
  declarations: [TipoCedulaPage],
})
export class TipoCedulaPageModule {}
