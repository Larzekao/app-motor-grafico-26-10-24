using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AppMotorGrafico.Pantalla;
using AppMotorGrafico.seializacion;
using AppMotorGrafico.figuras3d;
using AppMotorGrafico.Animaciones;
using AppMotorGrafico.Importacion;
using System.Globalization;

namespace AppMotorGrafico
{
    public partial class Form1 : Form
    {
        private GLControl glControl1;
        private TreeView treeView1;
        private System.Windows.Forms.Timer timer;

        private Escenario escenario;
        private Camara3D camara;
        private MenuStrip menuStrip1;

        private enum ModoTransformacion { Ninguno, Trasladar, Rotar, Escalar }
        private ModoTransformacion modoActual = ModoTransformacion.Ninguno;

        private enum Eje { Ninguno, X, Y, Z }
        private Eje ejeActual = Eje.Ninguno;

        private bool mouseTransforming = false;
        private Point lastMousePos;

        // Lista para manejar múltiples selecciones
        private List<Figura3D> objetosSeleccionados = new List<Figura3D>();

        // Variables para la selección de objetos mediante rectángulo
        private bool isSelecting = false;
        private Point selectionStart;
        private Point selectionEnd;
        private Rectangle selectionRectangle;


        private Libreto libreto;
        private DateTime tiempoInicioAnimacion;
        private ListBox listBoxPartes;

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.WindowState = FormWindowState.Maximized;

            InicializarMenuStrip();
            InicializarTreeView();
            InicializarGLControl();

            camara = new Camara3D();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 16; //|
            timer.Tick += Timer_Tick;
            timer.Start();

            // Inicializar el ListBox para las partes predeterminadas
            InicializarListBoxPartes();

         


            // Asume que tienes botones con estos nombres en tu formulario
            button1.Click += BtnTrasladar_Click;
            button2.Click += BtnRotar_Click;
            button3.Click += BtnEscalar_Click;
            button4.Click += BtnCargarOBJ_Click;
            button5.Click += BtnAnimarEscena_Click;
            // Crear y configurar el botón "Crear Objeto"
            Button botonCrearObjeto = new Button();
            botonCrearObjeto.Text = "Crear Objeto";
            botonCrearObjeto.Size = new Size(100, 30);
            botonCrearObjeto.Location = new Point(10, 90); // Ajusta la posición según sea necesario
            botonCrearObjeto.Click += BtnAnimarEscena_Click;
            this.Controls.Add(botonCrearObjeto);


        }

        private void BotonCrearObjeto_Click(object sender, EventArgs e)
        {
            // Verificar que haya figuras seleccionadas
            if (objetosSeleccionados.Count > 0)
            {
                // Crear un nuevo UncObjeto
                UncObjeto nuevoObjeto = new UncObjeto(Color4.White);

                // Lista para almacenar IDs de figuras a eliminar
                List<string> figurasAEliminar = new List<string>();

                // Diccionario para mapear figuras a sus IDs en el escenario
                var figurasDict = new Dictionary<Figura3D, string>();
                foreach (var id in escenario.ListarFiguras())
                {
                    figurasDict[escenario.ObtenerFigura(id)] = id;
                }

                // Recorrer las figuras seleccionadas
                foreach (var figura in objetosSeleccionados)
                {
                    // Si la figura es un UncObjeto
                    if (figura is UncObjeto uncObjeto)
                    {
                        // Añadir sus partes al nuevo objeto
                        foreach (var parteEntry in uncObjeto.Partes)
                        {
                            string idNuevaParte = "Parte_" + Guid.NewGuid().ToString();
                            nuevoObjeto.AñadirParte(idNuevaParte, parteEntry.Value);
                        }

                        // Marcar el objeto original para eliminar
                        if (figurasDict.TryGetValue(uncObjeto, out string idFigura))
                        {
                            figurasAEliminar.Add(idFigura);
                        }
                    }
                    // Si la figura es un UncParte
                    else if (figura is UncParte uncParte)
                    {
                        string idNuevaParte = "Parte_" + Guid.NewGuid().ToString();
                        nuevoObjeto.AñadirParte(idNuevaParte, uncParte);

                        // Si la parte pertenece a un objeto, eliminarla de ese objeto
                        foreach (var objetoId in escenario.ListarFiguras())
                        {
                            var obj = escenario.ObtenerFigura(objetoId) as UncObjeto;
                            if (obj != null && obj.Partes.ContainsValue(uncParte))
                            {
                                string keyToRemove = null;
                                foreach (var key in obj.Partes.Keys)
                                {
                                    if (obj.Partes[key] == uncParte)
                                    {
                                        keyToRemove = key;
                                        break;
                                    }
                                }
                                if (keyToRemove != null)
                                {
                                    obj.Partes.Remove(keyToRemove);
                                }
                            }
                        }
                    }
                    // Si la figura es un UncPoligono
                    else if (figura is UncPoligono uncPoligono)
                    {
                        // Crear una nueva parte para el polígono
                        UncParte nuevaParte = new UncParte(uncPoligono.Color);
                        string idNuevoPoligono = "Poligono_" + Guid.NewGuid().ToString();
                        nuevaParte.AñadirPoligono(idNuevoPoligono, uncPoligono);

                        string idNuevaParte = "Parte_" + Guid.NewGuid().ToString();
                        nuevoObjeto.AñadirParte(idNuevaParte, nuevaParte);

                        // Eliminar el polígono de su parte original
                        foreach (var figuraEntry in escenario.ListarFiguras())
                        {
                            var obj = escenario.ObtenerFigura(figuraEntry) as UncObjeto;
                            if (obj != null)
                            {
                                foreach (var parte in obj.Partes.Values)
                                {
                                    if (parte.Poligonos.ContainsValue(uncPoligono))
                                    {
                                        string keyToRemove = null;
                                        foreach (var key in parte.Poligonos.Keys)
                                        {
                                            if (parte.Poligonos[key] == uncPoligono)
                                            {
                                                keyToRemove = key;
                                                break;
                                            }
                                        }
                                        if (keyToRemove != null)
                                        {
                                            parte.Poligonos.Remove(keyToRemove);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Eliminar las figuras originales del escenario
                foreach (var id in figurasAEliminar)
                {
                    escenario.EliminarFigura(id);
                }

                // Añadir el nuevo objeto al escenario
                string idNuevoObjeto = "Objeto_" + Guid.NewGuid().ToString();
                escenario.AgregarFigura(idNuevoObjeto, nuevoObjeto);

                // Actualizar el TreeView
                ActualizarTreeView();

                // Limpiar la selección
                objetosSeleccionados.Clear();
                DeseleccionarTodos();

                // Notificar al usuario
                MessageBox.Show("Se creó un nuevo objeto a partir de las figuras seleccionadas.");
            }
            else
            {
                MessageBox.Show("No hay figuras seleccionadas para crear un objeto.");
            }
        }

        private void InicializarListBoxPartes()
        {
            listBoxPartes = new ListBox();
            listBoxPartes.Width = 200;
            listBoxPartes.Height = (this.ClientSize.Height / 2) - menuStrip1.Height - 20;
            listBoxPartes.Location = new Point(this.ClientSize.Width - listBoxPartes.Width - 10, treeView1.Bottom + 10);
            listBoxPartes.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Añadir partes predeterminadas al ListBox
            listBoxPartes.Items.Add("Cubo");
            listBoxPartes.Items.Add("Rectangulo");
            listBoxPartes.Items.Add("Esfera");
            // Añadir más partes si es necesario

            listBoxPartes.SelectedIndexChanged += ListBoxPartes_SelectedIndexChanged;

            this.Controls.Add(listBoxPartes);
        }

        private void ListBoxPartes_SelectedIndexChanged(object sender, EventArgs e)
        {
            string nombreParteSeleccionada = listBoxPartes.SelectedItem as string;

            if (nombreParteSeleccionada != null)
            {
                // Crear la parte seleccionada y añadirla a la escena
                UncParte nuevaParte = null;
                Color4 color = Color4.Red; // Color predeterminado, se puede personalizar

                switch (nombreParteSeleccionada)
                {
                    case "Cubo":
                        nuevaParte = CrearCuboParte(color, 1.0); // Tamaño 1.0
                        break;
                    case "Esfera":
                        nuevaParte = CrearEsferaParte(color, 0.5, 16); // Radio 0.5, 16 segmentos
                        break;
                        // Manejar otras partes si se añaden
                }

                if (nuevaParte != null)
                {
                    // Añadir la nueva parte como un objeto temporal a la escena
                    UncObjeto objetoTemporal = new UncObjeto(color);
                    string idParte = "Parte_" + Guid.NewGuid().ToString();
                    objetoTemporal.AñadirParte(idParte, nuevaParte);

                    // Añadir el objeto temporal a la escena
                    string idObjeto = "Objeto_" + Guid.NewGuid().ToString();
                    escenario.AgregarFigura(idObjeto, objetoTemporal);

                    // Actualizar el TreeView
                    ActualizarTreeView();
                }
            }
        }
        private void BotonAgruparPartes_Click(object sender, EventArgs e)
        {
            // Agrupar las partes seleccionadas en un nuevo objeto

            if (objetosSeleccionados.Count > 0)
            {
                // Crear un nuevo UncObjeto
                UncObjeto nuevoObjeto = new UncObjeto(Color4.White);

                List<string> objetosAEliminar = new List<string>();

                // Mapear objetos a sus IDs
                var figurasDict = new Dictionary<Figura3D, string>();
                foreach (var id in escenario.ListarFiguras())
                {
                    figurasDict[escenario.ObtenerFigura(id)] = id;
                }

                foreach (var figura in objetosSeleccionados)
                {
                    if (figura is UncObjeto objeto)
                    {
                        foreach (var parteEntry in objeto.Partes)
                        {
                            // Añadir la parte al nuevo objeto
                            string idNuevaParte = "Parte_" + Guid.NewGuid().ToString();
                            nuevoObjeto.AñadirParte(idNuevaParte, parteEntry.Value);
                        }

                        // Marcar el objeto viejo para eliminación
                        if (figurasDict.TryGetValue(objeto, out string idObjeto))
                        {
                            objetosAEliminar.Add(idObjeto);
                        }
                    }
                }

                // Eliminar los objetos antiguos de la escena
                foreach (var id in objetosAEliminar)
                {
                    escenario.EliminarFigura(id);
                }

                // Añadir el nuevo objeto a la escena
                string idNuevoObjeto = "Objeto_" + Guid.NewGuid().ToString();
                escenario.AgregarFigura(idNuevoObjeto, nuevoObjeto);

                // Actualizar el TreeView
                ActualizarTreeView();

                // Serializar el nuevo objeto
                Serializador serializador = new Serializador();
                serializador.Serializar(nuevoObjeto, idNuevoObjeto);

                MessageBox.Show("Las partes se agruparon en un nuevo objeto y se serializaron.");
            }
            else
            {
                MessageBox.Show("No hay partes seleccionadas para agrupar.");
            }
        }


        // Función auxiliar para crear un polígono (una cara del cubo)
        private UncPoligono CrearPoligono(UncPunto[] vertices, Color4 color)
        {
            UncPoligono poligono = new UncPoligono(color);
            for (int i = 0; i < vertices.Length; i++)
            {
                poligono.AñadirVertice("v" + i, vertices[i]);
            }
            return poligono;
        }

        private void InicializarMenuStrip()
        {
            menuStrip1 = new MenuStrip();
            var archivo = new ToolStripMenuItem("Archivo");
            var nuevo = new ToolStripMenuItem("Nuevo");
            var abrir = new ToolStripMenuItem("Abrir");
            var guardar = new ToolStripMenuItem("Guardar");
            var salir = new ToolStripMenuItem("Salir");
            salir.Click += (s, e) => this.Close();

            archivo.DropDownItems.Add(nuevo);
            archivo.DropDownItems.Add(abrir);
            archivo.DropDownItems.Add(guardar);
            archivo.DropDownItems.Add(new ToolStripSeparator());
            archivo.DropDownItems.Add(salir);

            var opciones = new ToolStripMenuItem("Opciones");
            var ayuda = new ToolStripMenuItem("Ayuda");

            menuStrip1.Items.Add(archivo);
            menuStrip1.Items.Add(opciones);
            menuStrip1.Items.Add(ayuda);
            this.MainMenuStrip = menuStrip1;
            this.Controls.Add(menuStrip1);
        }

        private void InicializarTreeView()
        {
            treeView1 = new TreeView();
            treeView1.Width = 200;
            treeView1.Height = (this.ClientSize.Height / 2) - menuStrip1.Height - 20;
            treeView1.Location = new Point(this.ClientSize.Width - treeView1.Width - 10, menuStrip1.Height + 10);
            treeView1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            treeView1.AfterSelect += TreeView1_AfterSelect;
            this.Controls.Add(treeView1);
        }

        private void InicializarGLControl()
        {
            glControl1 = new GLControl(new GraphicsMode(32, 24, 0, 4));
            glControl1.BackColor = Color.Black;
            glControl1.Location = new Point(0, menuStrip1.Height + 10);
            glControl1.Size = new Size(this.ClientSize.Width - treeView1.Width - 30, this.ClientSize.Height - menuStrip1.Height - 20);
            glControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            glControl1.Load += glControl1_Load;
            glControl1.Paint += glControl1_Paint;
            glControl1.Resize += glControl1_Resize;
            glControl1.MouseDown += GlControl1_MouseDown;
            glControl1.MouseMove += GlControl1_MouseMove;
            glControl1.MouseUp += GlControl1_MouseUp;
            glControl1.MouseWheel += GlControl1_MouseWheel;
            this.Controls.Add(glControl1);
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);
            camara.IniciarMatrices(glControl1.Width, glControl1.Height);
            InicializarEscena();
        }
      private async void BtnAnimarEscena_Click(object sender, EventArgs e)
{
    Figura3D objetoT1 = escenario.ObtenerFigura("objetoT1");
    Figura3D pelota = escenario.ObtenerFigura("Pelota");
    Figura3D objetoPredeterminado = escenario.ObtenerFigura("objetoPredeterminado");

    libreto = new Libreto();
    Escena escena = new Escena();

    // Posición inicial del 
    UncPunto posicionInicialObjeto = objetoPredeterminado.CalcularCentroDeMasa();
    UncPunto posicionFinalObjeto = new UncPunto(1.5, 3.2, 0); 

    // Calcular diferencias para la traslación hacia arriba
    double deltaXObjeto = posicionFinalObjeto.X - posicionInicialObjeto.X;
    double deltaYObjeto = posicionFinalObjeto.Y - posicionInicialObjeto.Y;
    double deltaZObjeto = posicionFinalObjeto.Z - posicionInicialObjeto.Z;

    // Animar el movimiento del objeto predeterminado
    double duracionMovimientoObjeto = 3.0;
    Accion moverObjeto = new Accion(0.0, duracionMovimientoObjeto);
    moverObjeto.AgregarTransformacion(new Traslacion(objetoPredeterminado, deltaXObjeto, deltaYObjeto, deltaZObjeto, duracionMovimientoObjeto));

    // Simular el empuje de la pelota en 1.5 segundos (después de llegar a la posición final)
    double duracionEmpujePelota = 1.5;
    Accion empujarPelota = new Accion(duracionMovimientoObjeto, duracionEmpujePelota);
    // El objeto predeterminado rota para simular el empuje y se mueve ligeramente hacia adelante
    empujarPelota.AgregarTransformacion(new Rotacion(objetoPredeterminado, 0.0, 0.0, 30.0, () => objetoPredeterminado.CalcularCentroDeMasa(), duracionEmpujePelota));
    empujarPelota.AgregarTransformacion(new Traslacion(pelota, 0.8, 0.5, 0.0, duracionEmpujePelota));

    // Movimiento parabólico de la pelota 
    double duracionCaidaPelota = 3.0;
    double tiempoInicioCaida = duracionMovimientoObjeto + duracionEmpujePelota; // 3.0 + 1.5 = 4.5 segundos
    Accion caidaPelota = new Accion(tiempoInicioCaida, duracionCaidaPelota);

    // Parámetros del movimiento parabólico
    double velocidadInicialX = 1.2; 
    double velocidadInicialY = 0.8; 
    double gravedad = 9.8 * 0.2;

    caidaPelota.AgregarTransformacion(new MovimientoParabolico(pelota, velocidadInicialX, velocidadInicialY, gravedad, duracionCaidaPelota));

    // Agregar rotación para simular el giro de la pelota en el aire
    caidaPelota.AgregarTransformacion(new Rotacion(pelota, 0.0, 0.0, 720.0, () => pelota.CalcularCentroDeMasa(), duracionCaidaPelota));

    // Agregar las acciones a la escena
    escena.AgregarAccion("MoverObjeto", moverObjeto);
    escena.AgregarAccion("EmpujarPelota", empujarPelota);
    escena.AgregarAccion("CaidaPelota", caidaPelota);

    // Agregar la escena al libreto
    libreto.AgregarEscena("AnimacionPatearPelota", escena);

    // Ejecutar la animación
    await EjecutarEscenaAsincrona(libreto, "AnimacionPatearPelota");
}




        // Función para crear una parte de cubo
        private UncParte CrearCuboParte(Color4 color, double tamaño)
        {
            UncParte parteCubo = new UncParte(color);

            double mitadTamaño = tamaño / 2;
            UncPunto[] vertices = new UncPunto[]
            {
        new UncPunto(-mitadTamaño, -mitadTamaño, -mitadTamaño),
        new UncPunto(mitadTamaño, -mitadTamaño, -mitadTamaño),
        new UncPunto(mitadTamaño, mitadTamaño, -mitadTamaño),
        new UncPunto(-mitadTamaño, mitadTamaño, -mitadTamaño),
        new UncPunto(-mitadTamaño, -mitadTamaño, mitadTamaño),
        new UncPunto(mitadTamaño, -mitadTamaño, mitadTamaño),
        new UncPunto(mitadTamaño, mitadTamaño, mitadTamaño),
        new UncPunto(-mitadTamaño, mitadTamaño, mitadTamaño)
            };

            // Cara frontal
            UncPoligono caraFrontal = CrearPoligono(new UncPunto[] { vertices[4], vertices[5], vertices[6], vertices[7] }, color);
            parteCubo.AñadirPoligono("Frontal", caraFrontal);

            // Cara trasera
            UncPoligono caraTrasera = CrearPoligono(new UncPunto[] { vertices[0], vertices[1], vertices[2], vertices[3] }, color);
            parteCubo.AñadirPoligono("Trasera", caraTrasera);

            // Cara izquierda
            UncPoligono caraIzquierda = CrearPoligono(new UncPunto[] { vertices[0], vertices[3], vertices[7], vertices[4] }, color);
            parteCubo.AñadirPoligono("Izquierda", caraIzquierda);

            // Cara derecha
            UncPoligono caraDerecha = CrearPoligono(new UncPunto[] { vertices[1], vertices[5], vertices[6], vertices[2] }, color);
            parteCubo.AñadirPoligono("Derecha", caraDerecha);

            // Cara superior
            UncPoligono caraSuperior = CrearPoligono(new UncPunto[] { vertices[3], vertices[2], vertices[6], vertices[7] }, color);
            parteCubo.AñadirPoligono("Superior", caraSuperior);

            // Cara inferior
            UncPoligono caraInferior = CrearPoligono(new UncPunto[] { vertices[0], vertices[1], vertices[5], vertices[4] }, color);
            parteCubo.AñadirPoligono("Inferior", caraInferior);

            return parteCubo;
        }


        private async Task EjecutarEscenaAsincrona(Libreto libreto, string nombreEscena)
        {
            tiempoInicioAnimacion = DateTime.Now;

            while (!libreto.EstaCompletado(GetTiempoActual()))
            {
                double tiempoActual = GetTiempoActual();
                libreto.Ejecutar(tiempoActual);
                glControl1.Invalidate();
                await Task.Delay(16);
            }
        }

        private double GetTiempoActual()
        {
            return (DateTime.Now - tiempoInicioAnimacion).TotalSeconds;
        }
        private UncParte CrearEsferaParte(Color4 color, double radio, int segmentos)
        {
            UncParte parteEsfera = new UncParte(color);

            List<UncPunto> puntos = new List<UncPunto>();

            for (int numeroLatitud = 0; numeroLatitud <= segmentos; numeroLatitud++)
            {
                double theta = numeroLatitud * Math.PI / segmentos;
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);

                for (int numeroLongitud = 0; numeroLongitud <= segmentos; numeroLongitud++)
                {
                    double phi = numeroLongitud * 2 * Math.PI / segmentos;
                    double sinPhi = Math.Sin(phi);
                    double cosPhi = Math.Cos(phi);

                    double x = cosPhi * sinTheta;
                    double y = cosTheta;
                    double z = sinPhi * sinTheta;

                    puntos.Add(new UncPunto(radio * x, radio * y, radio * z));
                }
            }

            // Crear polígonos (triángulos) a partir de los puntos
            for (int numeroLatitud = 0; numeroLatitud < segmentos; numeroLatitud++)
            {
                for (int numeroLongitud = 0; numeroLongitud < segmentos; numeroLongitud++)
                {
                    int primero = (numeroLatitud * (segmentos + 1)) + numeroLongitud;
                    int segundo = primero + segmentos + 1;

                    UncPunto[] triangulo1 = new UncPunto[]
                    {
                puntos[primero],
                puntos[segundo],
                puntos[primero + 1]
                    };

                    UncPunto[] triangulo2 = new UncPunto[]
                    {
                puntos[segundo],
                puntos[segundo + 1],
                puntos[primero + 1]
                    };

                    UncPoligono poligono1 = CrearPoligono(triangulo1, color);
                    UncPoligono poligono2 = CrearPoligono(triangulo2, color);

                    string poligonoId1 = "Poligono_" + Guid.NewGuid().ToString();
                    string poligonoId2 = "Poligono_" + Guid.NewGuid().ToString();

                    parteEsfera.AñadirPoligono(poligonoId1, poligono1);
                    parteEsfera.AñadirPoligono(poligonoId2, poligono2);
                }
            }

            return parteEsfera;
        }




     






        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            camara.ConfigurarMatrices();
            escenario.Dibujar();

            //  rectángulo de selección si est selección
            if (isSelecting)
            {
                DrawSelectionRectangle();
            }

            glControl1.SwapBuffers();
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            if (glControl1.ClientSize.Height == 0)
                glControl1.ClientSize = new Size(glControl1.ClientSize.Width, 1);

            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
            camara.IniciarMatrices(glControl1.Width, glControl1.Height);

            treeView1.Height = (this.ClientSize.Height / 2) - menuStrip1.Height - 20;
            treeView1.Location = new Point(this.ClientSize.Width - treeView1.Width - 10, menuStrip1.Height + 10);

            glControl1.Size = new Size(this.ClientSize.Width - treeView1.Width - 30, this.ClientSize.Height - menuStrip1.Height - 20);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            glControl1.Invalidate();
        }

        private void InicializarEscena()
        {
            escenario = new Escenario(Color4.Black);
            Serializador ser = new Serializador();

            Figura3D objetoT1 = ser.Deserializar("ObjetoT");
            

 


            objetoT1.Trasladar(3, 4, 0);

            objetoT1.Escalar(1.5, objetoT1.CalcularCentroDeMasa());

          

            escenario.AgregarFigura("objetoT1", objetoT1);
         
            // Agregar un archivo OBJ predeterminado
            string rutaArchivoOBJ = @"C:\Users\Lenovo\Desktop\Universidad\6 Semestre\programacion grafica\practicas\Programacion-grafica-full-main\Modelos3D\modele e\untitledMInecrafs.obj"; // Asegúrate de especificar la ruta correcta
            var importer = new OBJImporter();
            UncObjeto objetoPredeterminado = importer.Importar(rutaArchivoOBJ, Color4.LightGreen);

            objetoPredeterminado.Trasladar(-5,0,0);
            objetoPredeterminado.Rotar(0,90,0);
                escenario.AgregarFigura("objetoPredeterminado", objetoPredeterminado);
            Figura3D Esfera = ser.Deserializar("Pelota");
            Esfera.Escalar(0.4);
            Esfera.Trasladar(1.5, 2.8, 0); // Ajustar la posición vertical para que esté encima de la 'T'
           
            escenario.AgregarFigura("Pelota", Esfera);

            ActualizarTreeView();
        }

        private void ActualizarTreeView()
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            foreach (var figuraEntry in escenario.ListarFiguras())
            {
                var objeto = escenario.ObtenerFigura(figuraEntry);
                TreeNode nodoObjeto = new TreeNode(figuraEntry) { Tag = objeto };

                if (objeto.IsSelected)
                {
                    nodoObjeto.BackColor = Color.LightBlue;
                }

                if (objeto is UncObjeto uncObjeto)
                {
                    foreach (var parteEntry in uncObjeto.Partes)
                    {
                        var parte = parteEntry.Value;
                        TreeNode nodoParte = new TreeNode(parteEntry.Key) { Tag = parte };

                        if (parte.IsSelected)
                        {
                            nodoParte.BackColor = Color.LightBlue;
                        }

                        foreach (var poligonoEntry in parte.Poligonos)
                        {
                            var poligono = poligonoEntry.Value;
                            TreeNode nodoPoligono = new TreeNode(poligonoEntry.Key) { Tag = poligono };

                            if (poligono.IsSelected)
                            {
                                nodoPoligono.BackColor = Color.LightBlue;
                            }

                            foreach (var puntoEntry in poligono.Puntos)
                            {
                                var punto = puntoEntry.Value;
                                TreeNode nodoPunto = new TreeNode(puntoEntry.Key) { Tag = punto };
                                nodoPoligono.Nodes.Add(nodoPunto);
                            }

                            nodoParte.Nodes.Add(nodoPoligono);
                        }

                        nodoObjeto.Nodes.Add(nodoParte);
                    }
                }

                treeView1.Nodes.Add(nodoObjeto);
            }

            treeView1.EndUpdate();
            treeView1.ExpandAll();
        }

        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var seleccionado = e.Node.Tag;

            // Deseleccionamos todo antes de aplicar la selección
            DeseleccionarTodos();

            // Limpiar la lista de seleccionados
            objetosSeleccionados.Clear();

            // Marcamos lo que seleccionamos como "objetoSeleccionado"
            if (seleccionado is Figura3D figura)
            {
                figura.IsSelected = true;
                objetosSeleccionados.Add(figura);

            }
            else if (seleccionado is UncParte parte)
            {
                parte.IsSelected = true;
                objetosSeleccionados.Add(parte);

            }
            else if (seleccionado is UncPoligono poligono)
            {
                poligono.IsSelected = true;
                objetosSeleccionados.Add(poligono);

            }
            else if (seleccionado is UncPunto punto)
            {
                // No asignamos puntos como objetos seleccionados directamente
                Console.WriteLine($"Vértice seleccionado: {e.Node.Text}");
            }

            glControl1.Invalidate(); // Para refrescar la pantalla después de la selección
        }

        private void DeseleccionarTodos()
        {
            foreach (var figuraEntry in escenario.ListarFiguras())
            {
                var objeto = escenario.ObtenerFigura(figuraEntry);
                objeto.IsSelected = false;

                if (objeto is UncObjeto uncObjeto)
                {
                    foreach (var parte in uncObjeto.Partes.Values)
                    {
                        parte.IsSelected = false;
                        foreach (var poligono in parte.Poligonos.Values)
                        {
                            poligono.IsSelected = false;
                        }
                    }
                }
            }
        }





        private void BtnTrasladar_Click(object sender, EventArgs e)
        {
            modoActual = ModoTransformacion.Trasladar;
        }

        private void BtnRotar_Click(object sender, EventArgs e)
        {
            modoActual = ModoTransformacion.Rotar;
        }

        private void BtnEscalar_Click(object sender, EventArgs e)
        {
            modoActual = ModoTransformacion.Escalar;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.X:
                    ejeActual = Eje.X;
                    break;
                case Keys.Y:
                    ejeActual = Eje.Y;
                    break;
                case Keys.Z:
                    ejeActual = Eje.Z;
                    break;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.X || e.KeyCode == Keys.Y || e.KeyCode == Keys.Z)
                ejeActual = Eje.Ninguno;
        }

        private void GlControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Control.ModifierKeys.HasFlag(Keys.Shift))
            {
                isSelecting = true;
                selectionStart = e.Location;
                selectionEnd = e.Location;
            }
            else if (e.Button == MouseButtons.Right && modoActual != ModoTransformacion.Ninguno && ejeActual != Eje.Ninguno)
            {
                if (objetosSeleccionados.Count > 0)
                {
                    mouseTransforming = true;
                    lastMousePos = e.Location;
                }
            }
            else
            {
                camara.OnMouseDown(e);
            }
        }

        private void GlControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                selectionEnd = e.Location;
                selectionRectangle = GetRectangleFromPoints(selectionStart, selectionEnd);
                glControl1.Invalidate();
            }
            else if (mouseTransforming && objetosSeleccionados.Count > 0)
            {
                int deltaX = e.X - lastMousePos.X;
                int deltaY = e.Y - lastMousePos.Y;
                lastMousePos = e.Location;

                switch (modoActual)
                {
                    case ModoTransformacion.Trasladar:
                        AplicarTraslacion(deltaX, deltaY);
                        break;
                    case ModoTransformacion.Rotar:
                        AplicarRotacion(deltaX, deltaY);
                        break;
                    case ModoTransformacion.Escalar:
                        AplicarEscalado(deltaX, deltaY);
                        break;
                }

                glControl1.Invalidate();
            }
            else
            {
                camara.OnMouseMove(e);
                glControl1.Invalidate();
            }
        }

        private void GlControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;
                PerformSelection();
                glControl1.Invalidate();
            }
            else if (mouseTransforming)
            {
                mouseTransforming = false;
            }
            else
            {
                camara.OnMouseUp(e);
            }
        }

        private void GlControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            camara.OnMouseWheel(e);
            glControl1.Invalidate();
        }

        private Rectangle GetRectangleFromPoints(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y));
        }

        private void DrawSelectionRectangle()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, glControl1.Width, glControl1.Height, 0, -1, 1);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Disable(EnableCap.DepthTest);
            GL.Color4(1.0f, 1.0f, 1.0f, 0.3f); // Blanco semitransparente
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex2(selectionRectangle.Left, selectionRectangle.Top);
            GL.Vertex2(selectionRectangle.Right, selectionRectangle.Top);
            GL.Vertex2(selectionRectangle.Right, selectionRectangle.Bottom);
            GL.Vertex2(selectionRectangle.Left, selectionRectangle.Bottom);
            GL.End();
            GL.Enable(EnableCap.DepthTest);

            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
        }

        private void PerformSelection()
        {
            objetosSeleccionados.Clear(); // Limpiar la lista antes de la selección

            foreach (var figuraEntry in escenario.ListarFiguras())
            {
                var objeto = escenario.ObtenerFigura(figuraEntry);

                // Revisar si el objeto completo está dentro de la selección
                if (IsObjectInSelection(objeto))
                {
                    objeto.IsSelected = true;
                    objetosSeleccionados.Add(objeto);
                }
                else
                {
                    objeto.IsSelected = false;
                }

                // Si es un objeto compuesto, revisar sus partes y polígonos
                if (objeto is UncObjeto objetoCompuesto)
                {
                    foreach (var parte in objetoCompuesto.Partes.Values)
                    {
                        if (IsObjectInSelection(parte))
                        {
                            parte.IsSelected = true;
                            objetosSeleccionados.Add(parte);
                        }
                        else
                        {
                            parte.IsSelected = false;
                        }

                        foreach (var poligono in parte.Poligonos.Values)
                        {
                            if (IsObjectInSelection(poligono))
                            {
                                poligono.IsSelected = true;
                                objetosSeleccionados.Add(poligono);
                            }
                            else
                            {
                                poligono.IsSelected = false;
                            }
                        }
                    }
                }
            }

            // Actualizar el TreeView para reflejar la selección
            ActualizarTreeView();
        }

        private bool IsObjectInSelection(Figura3D objeto)
        {
            // Obtener el centro de masa del objeto o parte/polígono
            UncPunto centro = objeto.CalcularCentroDeMasa();

            // Convertir las coordenadas del mundo a coordenadas de clip space
            Vector4 worldPosition = new Vector4((float)centro.X, (float)centro.Y, (float)centro.Z, 1.0f);
            Vector4 clipSpacePos = Vector4.Transform(worldPosition, camara.GetModelViewProjectionMatrix());


            if (clipSpacePos.W == 0)
                return false;

            Vector3 ndcSpacePos = new Vector3(
                clipSpacePos.X / clipSpacePos.W,
                clipSpacePos.Y / clipSpacePos.W,
                clipSpacePos.Z / clipSpacePos.W);


            Point screenPos = new Point(
                (int)(((ndcSpacePos.X + 1.0f) / 2.0f) * glControl1.Width),
                (int)(((1.0f - ndcSpacePos.Y) / 2.0f) * glControl1.Height));

            // Verificar si las coordenadas del objeto están dentro del rectángulo de selección
            return selectionRectangle.Contains(screenPos);
        }


        private void AplicarTraslacion(int deltaX, int deltaY)
        {
            double factor = 0.01;
            double dx = 0, dy = 0, dz = 0;

            switch (ejeActual)
            {
                case Eje.X:
                    dx = deltaX * factor;
                    break;
                case Eje.Y:
                    dy = -deltaY * factor;
                    break;
                case Eje.Z:
                    dz = deltaX * factor;
                    break;
            }

            foreach (var objeto in objetosSeleccionados)
            {
                if (objeto is UncPoligono poligono)  // Si es una cara (polígono)
                {
                    poligono.Trasladar(dx, dy, dz); // Solo trasladamos la cara
                }
                else if (objeto is UncParte parte)  // Si es una parte
                {
                    parte.Trasladar(dx, dy, dz); // Trasladamos toda la parte
                }
                else  // Si es un objeto completo
                {
                    objeto.Trasladar(dx, dy, dz);
                }
            }
        }
        private void AplicarRotacion(int deltaX, int deltaY)
        {
            // Factor de sensibilidad para controlar la velocidad de rotación
            double factor = 0.5;
            double angleX = 0, angleY = 0, angleZ = 0;

            // Asignar el ángulo de rotación dependiendo del eje actual
            switch (ejeActual)
            {
                case Eje.X:
                    angleX = deltaY * factor;
                    break;
                case Eje.Y:
                    angleY = deltaX * factor;
                    break;
                case Eje.Z:
                    angleZ = deltaX * factor;
                    break;
            }



            UncPunto centroGlobal = CalcularCentroDeSeleccion();

            // Aplicar la rotación a cada objeto seleccionado
            foreach (var objeto in objetosSeleccionados)
            {
                objeto.Rotar(angleX, angleY, angleZ, centroGlobal);
            }

            // Redibujar la escena
            glControl1.Invalidate();

        }

        // Método para calcular el centro de masa o centro de los objetos seleccionados
        private UncPunto CalcularCentroDeSeleccion()
        {
            if (objetosSeleccionados.Count == 0)
            {
                return new UncPunto(0, 0, 0);
            }


            double xProm = objetosSeleccionados.Average(obj => obj.CalcularCentroDeMasa().X);
            double yProm = objetosSeleccionados.Average(obj => obj.CalcularCentroDeMasa().Y);
            double zProm = objetosSeleccionados.Average(obj => obj.CalcularCentroDeMasa().Z);

            return new UncPunto(xProm, yProm, zProm);
        }
        public static UncObjeto ImportarModeloOBJ(string rutaArchivo)
        {
            UncObjeto nuevoObjeto = new UncObjeto(Color4.White);
            UncParte nuevaParte = new UncParte(Color4.White);

            // Diccionario para eliminar vértices duplicados
            Dictionary<string, UncPunto> verticesUnicos = new Dictionary<string, UncPunto>();

            using (StreamReader lector = new StreamReader(rutaArchivo))
            {
                string linea;
                while ((linea = lector.ReadLine()) != null)
                {
                    string[] partes = linea.Split(' ');

                    if (partes[0] == "v") // Línea que define un vértice
                    {
                        string keyVertice = partes[1] + "_" + partes[2] + "_" + partes[3];
                        if (!verticesUnicos.ContainsKey(keyVertice))
                        {
                            double x = double.Parse(partes[1], CultureInfo.InvariantCulture);
                            double y = double.Parse(partes[2], CultureInfo.InvariantCulture);
                            double z = double.Parse(partes[3], CultureInfo.InvariantCulture);

                            verticesUnicos[keyVertice] = new UncPunto(x, y, z);
                        }
                    }
                    else if (partes[0] == "f") // Línea que define una cara (polígono)
                    {
                        UncPoligono poligono = new UncPoligono(Color4.White);

                        for (int i = 1; i < partes.Length; i++)
                        {
                            int indice = int.Parse(partes[i].Split('/')[0]) - 1; // Índice del vértice
                            string keyVertice = verticesUnicos.Keys.ElementAt(indice);
                            UncPunto punto = verticesUnicos[keyVertice];
                            poligono.AñadirVertice($"v{indice}", punto);
                        }

                        string idPoligono = "Poligono_" + Guid.NewGuid().ToString();
                        nuevaParte.AñadirPoligono(idPoligono, poligono);
                    }
                }
            }

            string idParte = "Parte_" + Guid.NewGuid().ToString();
            nuevoObjeto.AñadirParte(idParte, nuevaParte);
            nuevoObjeto.CalcularCentroDeMasa();

            return nuevoObjeto;
        }

        // Evento Click para cargar archivo .obj
        private async void BtnCargarOBJ_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivos OBJ (*.obj)|*.obj";
                openFileDialog.Title = "Seleccionar archivo OBJ";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string rutaArchivo = openFileDialog.FileName;
                    string idObjeto = "Objeto_" + DateTime.Now.Ticks; // ID único para el objeto
                    Color4 colorObjeto = Color4.LightGreen; // Puedes personalizar el color

                    var importer = new OBJImporter();
                    var objeto = await importer.ImportarAsync(rutaArchivo, colorObjeto);

                    if (objeto != null)
                    {
                        // Añadir el objeto importado al escenario
                        escenario.AgregarFigura(idObjeto, objeto);

                        // Ajustar la cámara para que el objeto sea visible
                      //  escenario.AjustarCamara(camara);

                        // Actualizar el TreeView
                        ActualizarTreeView();

                        // Recalcular matrices de la cámara y redibujar
                        camara.IniciarMatrices(glControl1.Width, glControl1.Height);
                        glControl1.Invalidate();

                        MessageBox.Show("Archivo OBJ cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al cargar el archivo OBJ.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }



        private void AplicarEscalado(int deltaX, int deltaY)
        {
            // Definir el factor de escalado basándonos en los movimientos del ratón
            double factor = 1.0 + deltaY * 0.01;


            if (objetosSeleccionados.Count > 0)
            {

                UncPunto centroGlobal = CalcularCentroDeSeleccion();


                foreach (var objeto in objetosSeleccionados)
                {
                    // Paso 1: Trasladar el objeto al origen (respecto al centro de masa)
                    objeto.Trasladar(-centroGlobal.X, -centroGlobal.Y, -centroGlobal.Z);


                    objeto.Escalar(factor, new UncPunto(0, 0, 0));


                    objeto.Trasladar(centroGlobal.X, centroGlobal.Y, centroGlobal.Z);
                }


                glControl1.Invalidate();
            }
        }





    }
}
