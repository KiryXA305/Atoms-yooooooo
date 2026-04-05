using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection.Metadata;
using Microsoft.VisualBasic.FileIO;
using OpenTK.Graphics.OpenGL;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

static class UParameter
{
    public static Font font1 = new Font(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets", "fonts", "font1.ttf"));
    public static Color OutlineMultiplier = new Color(200, 200, 200);
    public static Color DefaultMovealbleElement = new Color(81, 143, 250);
    public static Color DefaultTextColor = Color.White;
    public static Color DefaultPanelColor = new Color(48, 64, 72);

    public static bool IsPaused = false;

}
public interface IUIElement
{
    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public Color Color { get; set; }
    public bool IsVisible { get; set; }

    public FloatRect GetLocalBounds()
    {
        return new FloatRect();
    }
    public FloatRect GetGlobalBounds()
    {
        return new FloatRect();
    }
    public void Draw(RenderWindow w);
}

public class UIButton : IUIElement
{
    private static readonly Color onClickedColor = new Color(200, 200, 200);
    private static readonly Color onSelectedColor = new Color(230, 230, 230);

    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public Color Color { get; set; }

    public string Text { get; set; }
    public bool IsSelected { get; private set; }
    public bool IsPressed { get; private set; }
    public bool IsVisible { get; set; }

    public event Action OnClick;

    public UIButton(Vector2f position, Vector2f size, Color color, string text)
    {
        Position = position;
        Size = size;
        Color = color;
        Text = text;
        IsVisible = true;
        IsSelected = false;

        Initialize();
    }

    private void Initialize()
    {
        RectangleShape body = new RectangleShape(Size);

        UI.Window.MouseMoved += (sender, e) =>
        {
            body.Position = Position;
            var bounds = body.GetGlobalBounds();

            var worldPos = UI.Window.MapPixelToCoords(e.Position);

            IsSelected = bounds.Contains(worldPos);
        };

        UI.Window.MouseButtonPressed += (sender, e) =>
        {
            if (!IsSelected) return;
            if (e.Button != Mouse.Button.Left) return;

            IsPressed = true;
        };

        UI.Window.MouseButtonReleased += (sender, e) =>
        {
            if (e.Button != Mouse.Button.Left) return;

            if (IsPressed)
                OnClick?.Invoke();

            IsPressed = false;
        };
    }

    public void Draw(RenderWindow w)
    {
        RectangleShape body = new RectangleShape(Size);

        body.Position = Position;
        body.FillColor = Color;
        if (IsSelected && !IsPressed)
            body.FillColor *= onSelectedColor;
        else if (IsPressed)
            body.FillColor *= onClickedColor;


        body.OutlineColor = body.FillColor * new Color(200, 200, 200);
        body.OutlineThickness = 4;

        UITextPanel textPanel = new UITextPanel(Position, Size, Color.Black, (uint)Size.Y, Text);
        textPanel.Align(body.GetGlobalBounds());

        w.Draw(body);
        textPanel.Draw(w);
    }

    public FloatRect GetLocalBounds()
    {
        var rect = new RectangleShape(Size);
        return rect.GetLocalBounds();
    }

    public FloatRect GetGlobalBounds()
    {
        var rect = new RectangleShape(Size);
        return rect.GetGlobalBounds();
    }
}

public class UISlider : IUIElement
{
    public static readonly Color OnSelectedColor = new Color(230, 230, 230);

    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public Color Color { get; set; }
    public Color ColorThumb { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSelected { get; set; }

    public float Min { get; set; }
    public float Max { get; set; }
    public float Value { get; set; }
    public float Step { get; set; }

    private RectangleShape thumb;
    private RectangleShape track;

    public UISlider(Vector2f position, Vector2f size, Color color, Color colorThumb, float min, float max, float startValue, float step)
    {
        IsVisible = true;
        IsSelected = false;

        Position = position;
        Size = size;
        Color = color;
        ColorThumb = colorThumb;
        Min = min;
        Max = max;
        Step = step;
        Value = Math.Clamp(startValue, Min, Max);

        track = new RectangleShape(Size);
        thumb = new RectangleShape(new Vector2f(Size.X / 7, Size.Y));

        Initialize();
    }

    private void Initialize()
    {
        Vector2f oldPosition = new Vector2f(0, 0);

        UI.Window.MouseMoved += (s, e) =>
        {
            var worldPos = UI.Window.MapPixelToCoords(e.Position);

            if (!UI.ButtonLeftPressed)
                IsSelected = thumb.GetGlobalBounds().Contains(worldPos);


            if (IsSelected && UI.ButtonLeftPressed)
            {
                float x = thumb.Position.X + worldPos.X - oldPosition.X;
                x = Math.Clamp(x, track.Position.X, track.Position.X + track.Size.X - thumb.Size.X);

                Value = Min + ((x - track.Position.X) / (track.Size.X-thumb.Size.X)) * (Max - Min);
                Value = MathF.Round(Value / Step) * Step;
            }

            oldPosition = worldPos;
        };
    }

    public void Draw(RenderWindow window)
    {
        track.Position = Position;
        track.FillColor = Color;
        track.OutlineColor = track.FillColor * UParameter.OutlineMultiplier;
        track.OutlineThickness = 3;

        UITextPanel valueText = new UITextPanel(Position, new Vector2f(1, 1), Color.Black, (uint)(Size.Y / 1.5f), Value.ToString());
        valueText.Align(track.GetGlobalBounds());

        Vector2f thumbPosition = new Vector2f(0, track.Position.Y);
        thumbPosition.X = Position.X + ((Value - Min) / (Max - Min)) * (Size.X-thumb.Size.X);
        thumb.Position = thumbPosition;
        thumb.FillColor = ColorThumb;
        if (IsSelected) thumb.FillColor *= UParameter.OutlineMultiplier;

        thumb.OutlineColor = thumb.FillColor * UParameter.OutlineMultiplier;
        thumb.OutlineThickness = 3;

        window.Draw(track);
        window.Draw(thumb);
        valueText.Draw(window);
    }
}

public class UIPanel : IUIElement
{
    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public Color Color { get; set; }
    public bool IsVisible { get; set; }
    public bool IsAligned { get; set; }

    public string TitleText { get; set; }
    public float Offset { get; set; }
    public float StartOffset { get; set; }
    public List<IUIElement> Children { get; set; }

    public UIPanel(Vector2f position, Vector2f size, string text, float offset, float startOffset, bool align = false)
    {
        Children = new();

        Position = position;
        Size = size;
        TitleText = text;
        Offset = offset;
        StartOffset = startOffset;
        IsVisible = true;
        IsAligned = align;
    }

    public void Draw(RenderWindow w)
    {
        UITextPanel title = new UITextPanel(Position, Size, Color.White, 40, TitleText);

        title.Draw(w);

        int i = 0;
        var childPos = Position;
        childPos.Y += StartOffset;

        foreach (var child in Children)
        {
            var cp = childPos;

            if (IsAligned)
            {
                cp.X = Position.X + title.GetGlobalBounds().Left + (title.GetGlobalBounds().Width - child.GetLocalBounds().Width) / 2f - child.GetLocalBounds().Left;
                // float x = rect.Left + (rect.Width - textBounds.Width) / 2f - textBounds.Left;
            }

            child.Position = cp;
            child.Draw(w);
            i++;
            childPos.Y += Offset;
        }
    }
}

public class UITextPanel : IUIElement
{
    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public Color Color { get; set; }
    public bool IsVisible { get; set; }

    public string Text { get; set; }

    public uint CharacterSize { get; set; }

    public UITextPanel(Vector2f position, Vector2f size, Color color, uint charSize, string text)
    {
        Position = position;
        Size = size;
        Color = color;
        IsVisible = true;
        CharacterSize = charSize;
        Text = text;
    }

    public void Draw(RenderWindow w)
    {
        Text text = new Text(UParameter.font1);
        text.Position = Position;
        text.FillColor = Color;
        text.CharacterSize = CharacterSize;
        text.Scale = new Vector2f(1, 1);
        text.DisplayedString = Text;

        w.Draw(text);
    }

    public void Align(FloatRect rect)
    {
        Text text = new Text(UParameter.font1);
        text.Position = Position;
        text.CharacterSize = CharacterSize;
        text.Scale = Size;
        text.DisplayedString = Text;

        FloatRect textBounds = text.GetLocalBounds();

        float x = rect.Left + (rect.Width - textBounds.Width) / 2f - textBounds.Left;
        float y = rect.Top + (rect.Height - textBounds.Height) / 2f - textBounds.Top;

        Position = new Vector2f(x, y);
    }

    public FloatRect GetLocalBounds()
    {
        var text = new Text(UParameter.font1, Text, CharacterSize);
        return text.GetLocalBounds();
    }

    public FloatRect GetGlobalBounds()
    {
        var text = new Text(UParameter.font1, Text, CharacterSize);
        return text.GetGlobalBounds();
    }
}

static class UI
{
    private static RenderWindow window;
    private static List<IUIElement> elements = new List<IUIElement>();
    public static RenderWindow Window => window;

    public static bool ButtonLeftPressed;

    public static void Initialize(RenderWindow w)
    {
        window = w;

        window.MouseButtonPressed += (s, e) =>
        {
            ButtonLeftPressed = e.Button == Mouse.Button.Left;
        };
        window.MouseButtonReleased += (s, e) =>
        {
            ButtonLeftPressed = !(e.Button == Mouse.Button.Left);
        };
    }

    public static void AddToList(IUIElement element) => elements.Add(element);
    public static void RemoveFromList(IUIElement element) => elements.Remove(element);

    public static void Draw()
    {
        foreach (var element in elements)
        {
            if (!element.IsVisible) return;

            element.Draw(window);
        }
    }
}