using AppMotorGrafico.figuras3d;
using System;

namespace AppMotorGrafico.Animaciones
{
    public abstract class Transformacion
    {
        public double Duracion { get; set; }
        public double TiempoInicio { get; set; }

        public abstract void EjecutarInterpolado(double tiempoActual);
    }
        public class Traslacion : Transformacion
        {
            private Figura3D objeto;
            private double deltaX, deltaY, deltaZ;
            private double posXInicial, posYInicial, posZInicial;
            private double posXAnterior, posYAnterior, posZAnterior; // Posición en la última actualización

            public Traslacion(Figura3D objeto, double deltaX, double deltaY, double deltaZ, double duracion)
            {
                this.objeto = objeto;
                this.deltaX = deltaX;
                this.deltaY = deltaY;
                this.deltaZ = deltaZ;
                this.Duracion = duracion;
                CalcularPosicionInicial();
                posXAnterior = posXInicial;
                posYAnterior = posYInicial;
                posZAnterior = posZInicial;
            }

            private void CalcularPosicionInicial()
            {
                var centro = objeto.CalcularCentroDeMasa();
                this.posXInicial = centro.X;
                this.posYInicial = centro.Y;
                this.posZInicial = centro.Z;
            }

            public override void EjecutarInterpolado(double tiempoActual)
            {
                if (tiempoActual < TiempoInicio) return;

                double progreso = Math.Min(1.0, (tiempoActual - TiempoInicio) / Duracion);

                double desplazamientoX = deltaX * progreso;
                double desplazamientoY = deltaY * progreso;
                double desplazamientoZ = deltaZ * progreso;

                double dx = desplazamientoX - (posXAnterior - posXInicial);
                double dy = desplazamientoY - (posYAnterior - posYInicial);
                double dz = desplazamientoZ - (posZAnterior - posZInicial);

                // Actualizar la posición anterior
                posXAnterior = posXInicial + desplazamientoX;
                posYAnterior = posYInicial + desplazamientoY;
                posZAnterior = posZInicial + desplazamientoZ;

                objeto.Trasladar(dx, dy, dz);
            }
        }

        public class Rotacion : Transformacion
        {
            private Figura3D objeto;
            private double anguloX, anguloY, anguloZ;
            private Func<UncPunto> obtenerCentro;
            private double anguloXInicial, anguloYInicial, anguloZInicial;
            private double anguloXAnterior, anguloYAnterior, anguloZAnterior;

            public Rotacion(Figura3D objeto, double anguloX, double anguloY, double anguloZ, Func<UncPunto> obtenerCentro, double duracion)
            {
                this.objeto = objeto;
                this.anguloX = anguloX;
                this.anguloY = anguloY;
                this.anguloZ = anguloZ;
                this.obtenerCentro = obtenerCentro;
                this.Duracion = duracion;
                anguloXInicial = 0;
                anguloYInicial = 0;
                anguloZInicial = 0;
                anguloXAnterior = anguloXInicial;
                anguloYAnterior = anguloYInicial;
                anguloZAnterior = anguloZInicial;
            }

            public override void EjecutarInterpolado(double tiempoActual)
            {
                if (tiempoActual < TiempoInicio) return;

                double progreso = Math.Min(1.0, (tiempoActual - TiempoInicio) / Duracion);

                double rotacionX = anguloX * progreso;
                double rotacionY = anguloY * progreso;
                double rotacionZ = anguloZ * progreso;

                double deltaX = rotacionX - anguloXAnterior;
                double deltaY = rotacionY - anguloYAnterior;
                double deltaZ = rotacionZ - anguloZAnterior;

                anguloXAnterior = rotacionX;
                anguloYAnterior = rotacionY;
                anguloZAnterior = rotacionZ;

                UncPunto centroActual = obtenerCentro(); // Obtener el centro dinámicamente

                objeto.Rotar(deltaX, deltaY, deltaZ, centroActual);
            }
    }



    public class MovimientoParabolico : Transformacion
    {
        private Figura3D objeto;
        private double velocidadInicialX;
        private double velocidadInicialY;
        private double gravedad;
        private double tiempoAnterior;

        public MovimientoParabolico(Figura3D objeto, double velocidadInicialX, double velocidadInicialY, double gravedad, double duracion)
        {
            this.objeto = objeto;
            this.velocidadInicialX = velocidadInicialX;
            this.velocidadInicialY = velocidadInicialY;
            this.gravedad = gravedad;
            this.Duracion = duracion;
            this.tiempoAnterior = 0.0;
        }

        public override void EjecutarInterpolado(double tiempoActual)
        {
            if (tiempoActual < TiempoInicio) return;

            double t = tiempoActual - TiempoInicio;

            if (t > Duracion)
                t = Duracion;

            // Calcular la posición actual en X y Y
            double x = velocidadInicialX * t;
            double y = velocidadInicialY * t - 0.5 * gravedad * t * t;

            // Calcular la posición anterior en X y Y
            double xAnterior = velocidadInicialX * tiempoAnterior;
            double yAnterior = velocidadInicialY * tiempoAnterior - 0.5 * gravedad * tiempoAnterior * tiempoAnterior;

            // Ca
            double deltaX = x - xAnterior;
            double deltaY = y - yAnterior;

            // Aplicar los desplazamientos al objeto
            objeto.Trasladar(deltaX, deltaY, 0.0);

            tiempoAnterior = t;

         
        }
    }
    public class Escalado : Transformacion
        {
            private Figura3D objeto;
            private double factorInicial, factorFinal;
            private UncPunto centro;
            private double factorAnterior;
            private double tiempoOffset;

            public Escalado(Figura3D objeto, double factorFinal, UncPunto centro, double duracion, double tiempoOffset = 0.0)
            {
                this.objeto = objeto;
                this.factorFinal = factorFinal;
                this.centro = centro;
                this.Duracion = duracion;
                this.factorInicial = 1.0;
                this.factorAnterior = factorInicial;
                this.tiempoOffset = tiempoOffset;
            }

            public override void EjecutarInterpolado(double tiempoActual)
            {
                if (tiempoActual < TiempoInicio + tiempoOffset) return;

                double tiempoRelativo = tiempoActual - TiempoInicio - tiempoOffset;
                double progreso = Math.Min(1.0, tiempoRelativo / Duracion);

                double factorActual = factorInicial + (factorFinal - factorInicial) * progreso;
                double factorEscala = factorActual / factorAnterior;

                objeto.Escalar(factorEscala, centro);

                factorAnterior = factorActual;
            }
        }


    }


