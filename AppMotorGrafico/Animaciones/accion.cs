using System;
using System.Collections.Generic;

namespace AppMotorGrafico.Animaciones
{
    public class Accion
    {
        public List<Transformacion> Transformaciones { get; private set; }
        public double TiempoInicio { get; private set; }  
        public double Duracion { get; private set; }      

        public Accion(double tiempoInicio, double duracion)
        {
            Transformaciones = new List<Transformacion>();
            this.TiempoInicio = tiempoInicio;
            this.Duracion = duracion;
        }

        public void AgregarTransformacion(Transformacion transformacion)
        {
            Transformaciones.Add(transformacion);
            transformacion.TiempoInicio = TiempoInicio;
            transformacion.Duracion = Duracion;
        }

        public void Ejecutar(double tiempoActual)
        {
            foreach (var transformacion in Transformaciones)
            {
                transformacion.EjecutarInterpolado(tiempoActual);
            }
        }

        public bool EstaCompletada(double tiempoActual)
        {
            return tiempoActual >= TiempoInicio + Duracion;
        }
    }

}
