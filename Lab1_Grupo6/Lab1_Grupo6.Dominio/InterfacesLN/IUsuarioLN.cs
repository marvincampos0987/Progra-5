using Lab1_Grupo6.Utilidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lab1_Grupo6.Dominio.InterfacesLN
{
    internal interface IUsuarioLN
    {
        Respuesta<IUsuarioLN> Insertar(IUsuarioLN clase);

        Respuesta<Clasetipada> Modificar(Clasetipada clase);

        Respuesta<bool> Eliminar(Clasetipada pedido);

        Respuesta<IEnumerable<Clasetipada>> Obtener(Clasetipada clase);

        Respuesta<Clasetipada> Buscar(Clasetipada clase);

        Respuesta<IEnumerable<Clasetipada>> Listar();
    }
}
