using System.Numerics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Game.Utility;
using Game.AtomPhysics;
using CameraUtility;
using Color = SFML.Graphics.Color;
using System.Configuration;
using System.Collections;

namespace Simulation;

class Programm
{
    private static Programm _instance;
    public static Programm Instance => _instance;
    private RenderWindow window;
    private Camera camera = new Camera();
    private Atoms atoms = new Atoms();
    public Atoms Atoms => atoms;

    public Camera Camera => camera;
    public RenderWindow Window => window;
    static Programm()
    {
        _instance = new Programm();

        _instance.window = new RenderWindow(VideoMode.DesktopMode, "Атомы йоу");
        _instance.window.SetMaximumSize(Const.MaxSizeWindow);
        _instance.window.SetFramerateLimit(60);

        UI.Initialize(_instance.window);
    }

    static void Main()
    {
        _instance.Simulation(_instance.window);
    }

    public void Simulation(RenderWindow window)
    {
        window.MouseWheelScrolled += (sender, e) =>
        {
            float steps = e.Delta;
            float zoomSpeed = 0.008f;
            float delta = steps * zoomSpeed;

            float zoomFactor = (float)Math.Exp(delta);

            camera.Zoom(1 / zoomFactor);
        };

        window.KeyPressed += (sender, e) =>
        {
            switch (e.Code)
            {
                case Keyboard.Key.R:
                    Camera.Reset();
                    break;  
            }
        };

        /*for (int i = 0; i < 1; i++)
            {
                List<Atom> molecule = Molecule.CreateMolecule(new Vector3f(500, 160 + i * 100, 0), Molecule.H2O);
            }*/
            
            // вот ето создаёт начальные атомы, а задать границы в const

            for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
            {
                Console.WriteLine(i + ", " + j);
                atoms.CreateHydrogen(new Vector3f(Const.MinEdgeX + 2 + i * 3, Const.MinEdgeY + 2 + j * 3, 3));
            }

        Vector2u sizeWindow = window.Size;

        Clock clock = new Clock();
        clock.Start();

        float stepCount = 0;
        float frameCount = 0;
        float fps = 0;

        UIPanel stats = new UIPanel(new Vector2f(0,sizeWindow.Y+45), new Vector2f(1,1), " ", 45, 0);
        stats.Children.Add(new UITextPanel(new Vector2f(0,0), new Vector2f(1,1), Color.White, 40, "алабуга политех"));
        stats.Children.Add(new UITextPanel(new Vector2f(0,0), new Vector2f(1,1), Color.White, 40, "алабуга политех"));
        stats.Children.Add(new UITextPanel(new Vector2f(0,0), new Vector2f(1,1), Color.White, 40, "алабуга политех"));

        UI.AddToList(stats);

        float averageKinetic = 1;
        float averageTemperature = 1;

        while (window.IsOpen)
        {
            frameCount++;
            stepCount++;

            window.DispatchEvents();
            MovementCamera();
            window.SetView(camera.cameraView);
            window.Clear(Color.Black);

            float left = Const.MinEdgeX / Const.Pixel2Angstrom;
            float top = Const.MinEdgeY / Const.Pixel2Angstrom;
            float w = (Const.MaxEdgeX - Const.MinEdgeX) / Const.Pixel2Angstrom;
            float h = (Const.MaxEdgeY - Const.MinEdgeY) / Const.Pixel2Angstrom;

            RectangleShape wall = new RectangleShape(new Vector2f(w, h));
            wall.Position = new Vector2f(left, top);
            wall.FillColor = Color.Black;
            wall.OutlineThickness = 1f;
            wall.OutlineColor = new Color(125, 125, 125);
            window.Draw(wall);

            float timer = 1;

            for (int i = 0; i < Const.TimeSpeed; i++)
                    atoms.Update(Const.Delta);

            atoms.Draw(window);

            averageKinetic = atoms.KineticEnergy / atoms.Count;
            averageTemperature = 0.802f * averageKinetic;

            string tString = averageTemperature > 1f ? $"Temperature: {Math.Round(averageTemperature, 5)} K" : $"Temperature: {MathF.Round(averageTemperature * 1000, 5)} mK";

            var st = stats.Children.Cast<UITextPanel>().ToList();
            st[0].Text = tString;
            st[1].Text = $"Avg K energy: {MathF.Round(averageKinetic, 5)}";
            st[2].Text = $"Fps: {fps} | Steps: {stepCount}";

            window.SetView(window.DefaultView);
            UI.Draw();

            Time elapsed = clock.ElapsedTime;
            float elapsedSeconds = elapsed.AsSeconds();

            if (timer <= elapsedSeconds)
            {
                fps = frameCount;
                frameCount = 0;
                clock.Restart();
            }

            if (Keyboard.IsKeyPressed(Keyboard.Key.Num1))
            {
                Const.MaxEdgeX -= 0.02f;
                Const.MaxEdgeY -= 0.02f;
                Const.MinEdgeX += 0.02f;
                Const.MinEdgeY += 0.02f;
            }
            else if (Keyboard.IsKeyPressed(Keyboard.Key.Num2))
            {
                Const.MaxEdgeX += 0.02f;
                Const.MaxEdgeY += 0.02f;
                Const.MinEdgeX -= 0.02f;
                Const.MinEdgeY -= 0.02f;
            }

            if (Keyboard.IsKeyPressed(Keyboard.Key.Num3))
                Const.MaxEdgeY -= 0.02f;
            if (Keyboard.IsKeyPressed(Keyboard.Key.Num4))
                Const.MaxEdgeY += 0.02f;

            window.Display();
        }
    }

    private void MovementCamera()
    {
        if (Keyboard.IsKeyPressed(Keyboard.Key.Left) || Keyboard.IsKeyPressed(Keyboard.Key.A))
            camera.Move(-new Vector2f(10f, 0) / camera.GetZoomValue());
        else if (Keyboard.IsKeyPressed(Keyboard.Key.Right) || Keyboard.IsKeyPressed(Keyboard.Key.D))
            camera.Move(new Vector2f(10f, 0) / camera.GetZoomValue());
        if (Keyboard.IsKeyPressed(Keyboard.Key.Up) || Keyboard.IsKeyPressed(Keyboard.Key.W))
            camera.Move(-new Vector2f(0f, 10f) / camera.GetZoomValue());
        else if (Keyboard.IsKeyPressed(Keyboard.Key.Down) || Keyboard.IsKeyPressed(Keyboard.Key.S))
            camera.Move(new Vector2f(0f, 10f) / camera.GetZoomValue());
    }
}