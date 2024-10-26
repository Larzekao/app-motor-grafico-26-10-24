using System;
using System.Collections.Generic;

namespace AppMotorGrafico.Animaciones
{
    public class Libreto
    {
        public Dictionary<string, Escena> Escenas { get; private set; }

        public Libreto()
        {
            Escenas = new Dictionary<string, Escena>();
        }

        public void AgregarEscena(string nombre, Escena escena)
        {
            Escenas[nombre] = escena;
        }

        public void Ejecutar(double tiempoActual)
        {
            foreach (var escena in Escenas.Values)
            {
                escena.Ejecutar(tiempoActual);
            }
        }

        public bool EstaCompletado(double tiempoActual)
        {
            foreach (var escena in Escenas.Values)
            {
                if (!escena.EstaCompletada(tiempoActual))
                {
                    return false; 
                }
            }
            return true;  
        }
    }

}
