using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using OpenTK.Graphics;

using System.Text.Json.Serialization.Metadata;
using AppMotorGrafico.figuras3d;
using AppMotorGrafico.Importacion;

namespace AppMotorGrafico.seializacion
{
    public class Serializador
    {
        private JsonSerializerOptions opciones;
        public String path = @"C:\Users\Lenovo\Desktop\Universidad\6 Semestre\programacion grafica\practicas\Programacion-grafica-full-main\Modelos3D";
        public Serializador()
        {
           
            opciones = new JsonSerializerOptions
            {
                WriteIndented = true, 
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new Color4Converter() 
                },
                TypeInfoResolver = new PolymorphicTypeResolver()
            };
        }

        // Método para serializar un objeto Figura3D y guardarlo en un archivo
        public void Serializar(Figura3D objeto, string nombreArchivo)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(objeto, opciones);
                File.WriteAllText(Path.Combine(path, nombreArchivo + ".json"), jsonString);
                Console.WriteLine($"Objeto serializado correctamente en {Path.Combine(path, nombreArchivo)}.json");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al serializar el objeto: " + e.Message);
            }
        }

        // Método para deserializar un objeto Figura3D desde un archivo
        public Figura3D Deserializar(string nombreArchivo)
        {
            try
            {
                string jsonString = File.ReadAllText(Path.Combine(path, nombreArchivo + ".json"));
                Figura3D objeto = JsonSerializer.Deserialize<Figura3D>(jsonString, opciones);
                Console.WriteLine("Objeto deserializado correctamente.");
                return objeto;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al deserializar el objeto: " + e.Message);
                return null; 
            }
        }


    }

    // Resolver para manejar tipos polimórficos
    public class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (type == typeof(Figura3D))
            {
                jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$tipo",
                    IgnoreUnrecognizedTypeDiscriminators = false,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(UncObjeto), "objeto"),
                        new JsonDerivedType(typeof(UncParte), "parte"),
                        new JsonDerivedType(typeof(UncPoligono), "poligono")
                    }
                };
            }

            return jsonTypeInfo;
        }
    }


}
