export interface Pedido {
  pedidoId: number;
  clienteId: number;
  fechaPedido: string;
  moneda: string;
  total: number | null;
  creadoEn: string;
  creadoPor: string | null;
  actualizadoEn: string | null;
  actualizadoPor: string | null;
}

export interface Cliente {
  clienteId: number;
  nombre: string;
  email: string | null;
  telefono: string | null;
  fechaRegistro: string;
  activo: boolean;
  creadoEn: string;
  creadoPor: string | null;
  actualizadoEn: string | null;
  actualizadoPor: string | null;
  tipoCedula: number;
}

export interface Producto {
  productoId: string;
  nombre: string;
  precio: number;
  stock: number;
  categoriaId: number;
  activo: boolean;
  creadoEn: string;
  creadoPor: string | null;
  actualizadoEn: string | null;
  actualizadoPor: string | null;
}

export interface Categoria {
  categoriaId: number;
  nombreCategoria: string;
  activo: boolean;
  creadoEn: string;
  creadoPor: string | null;
  actualizadoEn: string | null;
  actualizadoPor: string | null;
}

export interface DetallesPedido {
  detalleId: number;
  pedidoId: number;
  productoId: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
}

export interface TipoCedula {
  tipoCedula1: number;
  descripcion: string;
}

export interface SegUsuario {
  usuario: string;
  cedulaUsuario: string;
  tipoCedulaId: number;
  nombre: string;
  apellidos: string;
  direccion: string | null;
  codigoPerfil: number;
  email: string | null;
  telefono: string | null;
  estado: number;
  fechaActualizacion: string;
}

export interface SegPerfil {
  codigoPerfil: number;
  descripcion: string;
}

export interface SegPantalla {
  codigoPantalla: number;
  nombrePantalla: string;
  posicion: number;
}

export interface SegPerfilXpantalla {
  perfilXpantallaId: number;
  codigoPerfil: number;
  codigoPantalla: number;
}
