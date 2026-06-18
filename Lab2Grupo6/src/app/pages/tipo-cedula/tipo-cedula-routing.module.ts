import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TipoCedulaPage } from './tipo-cedula.page';

const routes: Routes = [{ path: '', component: TipoCedulaPage }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class TipoCedulaPageRoutingModule {}
