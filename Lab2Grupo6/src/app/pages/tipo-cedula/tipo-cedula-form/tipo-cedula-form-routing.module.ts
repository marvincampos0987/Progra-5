import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TipoCedulaFormPage } from './tipo-cedula-form.page';

const routes: Routes = [{ path: '', component: TipoCedulaFormPage }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class TipoCedulaFormPageRoutingModule {}
