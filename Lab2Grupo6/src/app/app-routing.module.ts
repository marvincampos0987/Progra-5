import { NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: 'home',
    loadChildren: () => import('./home/home.module').then((m) => m.HomePageModule),
  },
  {
    path: 'clientes',
    loadChildren: () => import('./pages/clientes/clientes.module').then((m) => m.ClientesPageModule),
  },
  {
    path: 'clientes/nuevo',
    loadChildren: () =>
      import('./pages/clientes/clientes-form/clientes-form.module').then((m) => m.ClientesFormPageModule),
  },
  {
    path: 'clientes/editar/:id',
    loadChildren: () =>
      import('./pages/clientes/clientes-form/clientes-form.module').then((m) => m.ClientesFormPageModule),
  },
  {
    path: 'pedidos',
    loadChildren: () => import('./pages/pedidos/pedidos.module').then((m) => m.PedidosPageModule),
  },
  {
    path: 'pedidos/nuevo',
    loadChildren: () =>
      import('./pages/pedidos/pedidos-form/pedidos-form.module').then((m) => m.PedidosFormPageModule),
  },
  {
    path: 'pedidos/editar/:id',
    loadChildren: () =>
      import('./pages/pedidos/pedidos-form/pedidos-form.module').then((m) => m.PedidosFormPageModule),
  },
  {
    path: 'productos',
    loadChildren: () => import('./pages/productos/productos.module').then((m) => m.ProductosPageModule),
  },
  {
    path: 'productos/nuevo',
    loadChildren: () =>
      import('./pages/productos/productos-form/productos-form.module').then((m) => m.ProductosFormPageModule),
  },
  {
    path: 'productos/editar/:id',
    loadChildren: () =>
      import('./pages/productos/productos-form/productos-form.module').then((m) => m.ProductosFormPageModule),
  },
  {
    path: 'categorias',
    loadChildren: () => import('./pages/categorias/categorias.module').then((m) => m.CategoriasPageModule),
  },
  {
    path: 'categorias/nuevo',
    loadChildren: () =>
      import('./pages/categorias/categorias-form/categorias-form.module').then((m) => m.CategoriasFormPageModule),
  },
  {
    path: 'categorias/editar/:id',
    loadChildren: () =>
      import('./pages/categorias/categorias-form/categorias-form.module').then((m) => m.CategoriasFormPageModule),
  },
  {
    path: 'detalles-pedido',
    loadChildren: () =>
      import('./pages/detalles-pedido/detalles-pedido.module').then((m) => m.DetallesPedidoPageModule),
  },
  {
    path: 'detalles-pedido/nuevo',
    loadChildren: () =>
      import('./pages/detalles-pedido/detalles-pedido-form/detalles-pedido-form.module').then(
        (m) => m.DetallesPedidoFormPageModule
      ),
  },
  {
    path: 'detalles-pedido/editar/:id',
    loadChildren: () =>
      import('./pages/detalles-pedido/detalles-pedido-form/detalles-pedido-form.module').then(
        (m) => m.DetallesPedidoFormPageModule
      ),
  },
  {
    path: 'tipo-cedula',
    loadChildren: () => import('./pages/tipo-cedula/tipo-cedula.module').then((m) => m.TipoCedulaPageModule),
  },
  {
    path: 'tipo-cedula/nuevo',
    loadChildren: () =>
      import('./pages/tipo-cedula/tipo-cedula-form/tipo-cedula-form.module').then((m) => m.TipoCedulaFormPageModule),
  },
  {
    path: 'tipo-cedula/editar/:id',
    loadChildren: () =>
      import('./pages/tipo-cedula/tipo-cedula-form/tipo-cedula-form.module').then((m) => m.TipoCedulaFormPageModule),
  },
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full',
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
