using System;
using System.Collections.Generic;
using AppMotorGrafico.figuras3d;
using AppMotorGrafico.Importacion;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace AppMotorGrafico.Pantalla
{
    public class Escenario
    {
        private Dictionary<string, Figura3D> figuras;
        private Dictionary<int, object> idToObject; // Mapeo de ID a objeto
        public Color4 FondoColor { get; set; }
        private PlanoCartesiano plano = new PlanoCartesiano(0.1, 0.02);

        // Eventos para notificar cambios en el escenario
        public event Action<string, Figura3D> FiguraAgregada;
        public event Action<string> FiguraEliminada;
       
        public Escenario(Color4 fondoColor)
        {
            figuras = new Dictionary<string, Figura3D>();
            idToObject = new Dictionary<int, object>();
            FondoColor = fondoColor;
        }

        // Método para calcular el bounding box de todas las figuras
        public void CalcularBoundingBox(out UncPunto min, out UncPunto max)
        {
            min = new UncPunto(double.MaxValue, double.MaxValue, double.MaxValue);
            max = new UncPunto(double.MinValue, double.MinValue, double.MinValue);

            foreach (var figura in figuras.Values)
            {
                if (figura is UncObjeto objeto)
                {
                    foreach (var parte in objeto.Partes.Values)
                    {
                        foreach (var poligono in parte.Poligonos.Values)
                        {
                            foreach (var punto in poligono.Puntos.Values)
                            {
                                if (punto.X < min.X) min.X = punto.X;
                                if (punto.Y < min.Y) min.Y = punto.Y;
                                if (punto.Z < min.Z) min.Z = punto.Z;

                                if (punto.X > max.X) max.X = punto.X;
                                if (punto.Y > max.Y) max.Y = punto.Y;
                                if (punto.Z > max.Z) max.Z = punto.Z;
                            }
                        }
                    }
                }
            }
        }

        // Método para calcular el bounding box total y ajustar la cámara
        public void AjustarCamara(Camara3D camara)
        {
            CalcularBoundingBox(out UncPunto min, out UncPunto max);

            // Calcular el centro del bounding box
            double centroX = (min.X + max.X) / 2.0;
            double centroY = (min.Y + max.Y) / 2.0;
            double centroZ = (min.Z + max.Z) / 2.0;

            // Calcular el tamaño máximo del bounding box
            double tamMax = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));

            // Ajustar la cámara
            camara.AjustarCamara(new UncPunto(centroX, centroY, centroZ), tamMax);
        }

        // Método para listar todas las figuras
        public List<string> ListarFiguras()
        {
            return new List<string>(figuras.Keys);
        }

        // Método para dibujar una figura específica
        public void DibujarFigura(string id)
        {
            if (figuras.TryGetValue(id, out Figura3D figura))
            {
                figura.Dibujar();
            }
            else
            {
                Console.WriteLine($"No se encontró la figura con el ID '{id}'.");
            }
        }

        // Método para agregar una figura al escenario
        public bool AgregarFigura(string id, Figura3D figura)
        {
            if (figura == null)
                return false;

            if (figuras.ContainsKey(id))
            {
                Console.WriteLine($"Una figura con el ID '{id}' ya existe. No se añadió.");
                return false; // Evitar duplicados
            }

            figuras[id] = figura;
            FiguraAgregada?.Invoke(id, figura); // Notificar que una figura ha sido agregada
            return true;
        }

        // Método para eliminar una figura
        public bool EliminarFigura(string id)
        {
            if (figuras.Remove(id))
            {
                FiguraEliminada?.Invoke(id); // Notificar que una figura ha sido eliminada
                return true;
            }

            Console.WriteLine($"No se encontró la figura con el ID '{id}' para eliminar.");
            return false;
        }

        // Método para obtener una figura por su id
        public Figura3D ObtenerFigura(string id)
        {
            if (figuras.TryGetValue(id, out Figura3D figura))
            {
                return figura;
            }

            Console.WriteLine($"No se encontró la figura con el ID '{id}'.");
            return null;
        }

        // Método para calcular el centro de masa de todas las figuras
        public UncPunto CalcularCentroDeMasa()
        {
            if (figuras.Count == 0)
                return new UncPunto();

            var centros = new List<UncPunto>();
            foreach (var figura in figuras.Values)
            {
                if (figura is UncObjeto objeto)
                {
                    centros.Add(objeto.CalcularCentroDeMasa());
                }
            }

            if (centros.Count == 0)
                return new UncPunto();

            double xProm = centros.Average(p => p.X);
            double yProm = centros.Average(p => p.Y);
            double zProm = centros.Average(p => p.Z);

            return new UncPunto(xProm, yProm, zProm);
        }

     

        // Método para dibujar todas las figuras del escenario
        public void Dibujar()
        {
            GL.ClearColor(FondoColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Dibujar el plano cartesiano
            plano.Dibujar();

            // Dibujar todas las figuras
            foreach (var figura in figuras.Values)
            {
                figura.Dibujar();
            }
        }

        // Método para trasladar todas las figuras
        public void TrasladarTodas(double tx, double ty, double tz)
        {
            foreach (var figura in figuras.Values)
            {
                figura.Trasladar(tx, ty, tz);
            }
        }

        // Método para escalar todas las figuras
        public void EscalarTodas(double factor)
        {
            foreach (var figura in figuras.Values)
            {
                figura.Escalar(factor);
            }
        }

        // Método para rotar todas las figuras
        public void RotarTodas(double anguloX, double anguloY, double anguloZ, UncPunto centro)
        {
            foreach (var figura in figuras.Values)
            {
                figura.Rotar(anguloX, anguloY, anguloZ, centro);
            }
        }

        // Método para importar un archivo OBJ y añadirlo al escenario
        public async Task<bool> ImportarObjetoDesdeOBJAsync(string id, string rutaArchivo, Color4 color)
        {
            var importer = new OBJImporter();
            var objeto = await importer.ImportarAsync(rutaArchivo, color);

            if (objeto != null)
            {
                return AgregarFigura(id, objeto);
            }

            return false;
        }
    }
}
