using Lab1_Grupo6.Dominio.Entidades;
using Lab1_Grupo6.Dominio.EntidadesTipadas;
using Lab1_Grupo6.Utilidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lab1_Grupo6.Dominio.InterfacesLN
{
    internal interface IClienteLN
    {
        Respuesta<Cliente> Insertar(TCliente clase);

        Respuesta<Cliente> Modificar(TCliente clase);

        Respuesta<bool> Eliminar(TCliente pedido);

        Respuesta<IEnumerable<Cliente>> Obtener(TCliente clase);

        Respuesta<Cliente> Buscar(TCliente clase);

        Respuesta<IEnumerable<TCliente>> Listar();
    }
}
