using System;
using System.Collections.Generic;

namespace AppMotorGrafico.Animaciones
{
    public class Escena
    {
        public Dictionary<string, Accion> Acciones { get; private set; }

        public Escena()
        {
            Acciones = new Dictionary<string, Accion>();
        }

        public void AgregarAccion(string nombre, Accion accion)
        {
            Acciones[nombre] = accion;
        }

        public void Ejecutar(double tiempoActual)
        {
            foreach (var accion in Acciones.Values)
            {
           
                if (tiempoActual >= accion.TiempoInicio && !accion.EstaCompletada(tiempoActual))
                {
                    accion.Ejecutar(tiempoActual);
                }
            }
        }

        public bool EstaCompletada(double tiempoActual)
        {
            foreach (var accion in Acciones.Values)
            {
                if (!accion.EstaCompletada(tiempoActual))
                {
                    return false;  
                }
            }
            return true;  
        }
    }

}
