using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Game.Utility;
using Game.AtomPhysics;
using CameraUtility;
using Color = SFML.Graphics.Color;

namespace Simulation;

class Programm
{
    private static Programm _instance;
    public static Programm Instance => _instance;
    private RenderWindow window;
    private Camera camera = new Camera();
    private Atoms atoms = new Atoms();
    public Bonds bonds = new Bonds();
    public Atoms Atoms => atoms;
    public Bonds Bonds => bonds;

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
        window.Closed += (s, e) =>
        {
            window.Close();
        };

        window.MouseWheelScrolled += (sender, e) =>
        {
            float steps = e.Delta;
            float zoomSpeed = 0.05f;
            float power = steps * zoomSpeed;

            float zoomFactor = (float)Math.Exp(power);

            Vector2f oldPos = window.MapPixelToCoords(e.Position, camera.cameraView);

            camera.Zoom(1 / zoomFactor);

            Vector2f newPos = window.MapPixelToCoords(e.Position, camera.cameraView);

            Vector2f delta = oldPos - newPos;
            delta *= 1.5f;

            camera.Move(delta);
        };

        window.KeyPressed += (sender, e) =>
        {
            switch (e.Code)
            {
                case Keyboard.Key.R:
                    Camera.Reset();
                    break;
                case Keyboard.Key.Space:
                    Parameters.IsPause = !Parameters.IsPause;
                    break;  
            }
        };

        Vector2f oldPosition = (Vector2f)Mouse.GetPosition();
     
        window.MouseMoved += (s, e) =>
        {
            if (Mouse.IsButtonPressed(Mouse.Button.Middle))
            {
                float power = 0.5f;

                Vector2f delta = oldPosition - ((Vector2f)e.Position);

                camera.Move(delta * power / camera.GetZoomValue());
            }

            oldPosition = (Vector2f)e.Position;
        };

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

        UISlider sliderSimSpeed = new UISlider(new Vector2f(0,0), new Vector2f(100, 40), UParameter.DefaultPanelColor, UParameter.DefaultMovealbleElement, 1, 50, 1, 1);
        UI.AddToList(sliderSimSpeed);

        float averageKinetic = 1;
        float averageTemperature = 1;

        while (window.IsOpen)
        {
            frameCount++;
            if (!Parameters.IsPause)
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

            Parameters.TimeSpeed = sliderSimSpeed.Value;

            if (!Parameters.IsPause)
            {
                for (int i = 0; i < Parameters.TimeSpeed; i++)
                {
                    bonds.Update();
                    atoms.Update(Const.Delta);
                }
            }

            bonds.Draw(window);
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
