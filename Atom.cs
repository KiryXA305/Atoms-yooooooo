/*

using System.Numerics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Game.Player;
using Game.Utility;

using Color = SFML.Graphics.Color;

namespace Game.AtomPhysics;

public class Atom
{
    public enum AtomVariation
    {
        Hydrogen,
        Oxygen,
        Custom
    }

    private Random random = new Random();
    private Vector3 position;
    private Vector3 force;
    private float radius;
    private float covalentRadius;
    private Color color;
    private float epsilion;
    private float sigma;
    private float mass;
    private Vector3 speed;
    private RenderWindow window;
    private bool isDrag;
    private bool isMark;
    private AtomVariation variation;

    public int Valence;
    public float Mass => mass;
    public Vector3 Speed => speed;
    public Vector3 Position => position;
    public AtomVariation Variation => variation;
    public float CovalentRadius => covalentRadius;
    public Color Color => color;

    public Atom(Vector3 pos, AtomVariation variation = AtomVariation.Custom)
    {
        this.position = pos;
        this.speed = Vector3.Zero;
        this.force = Vector3.Zero;
        this.variation = variation;

        switch (variation)
        {
            case AtomVariation.Hydrogen:
                mass = 1;
                radius = 1 * Const.Scale;
                color = Color.White;
                epsilion = 0.1f;
                sigma = 3.0f;
                Valence = 1;
                covalentRadius = 0.37f;
                break;
            case AtomVariation.Oxygen:
                mass = 16;
                radius = 1.2f * Const.Scale;
                color = Color.Red;
                epsilion = 0.16f;
                sigma = 2.6f;
                Valence = 2;
                covalentRadius = 0.73f;
                break;

            case AtomVariation.Custom:
                mass = 1;
                radius = 1.0f * Const.Scale;
                color = Color.Yellow;
                epsilion = 3f;
                sigma = 3f;
                Valence = 3;
                covalentRadius = 0.73f;
                break;
        }
        sigma = (sigma * Const.Scale) / Const.Pixel2Angstrom / 4;
        covalentRadius = (covalentRadius * Const.Scale) / Const.Pixel2Angstrom;

        Initialize();
    }

    private void Initialize()
    {
        MouseInfo.Instance.MouseClicked += (sender, e) =>
        {
            if (e.ClickType == Mouse.Button.Left)
                TryAddToDragedList();
        };
        MouseInfo.Instance.MouseReleased += (sender, e) =>
        {
            if (e.ClickType == Mouse.Button.Left)
                TryRemoveFromDragedList();
        };

        MouseInfo.Instance.MouseClicked += (sender, e) =>
        {
            if (e.ClickType != Mouse.Button.Right) return;
            if (GetDistanceToCursor() > radius * 1.1f) return;

            ChangeMarked();
        };
    }

    public void Update(float d, List<Atom> atoms, RenderWindow window)
    {
        this.window = window;
        force += ComputeForce(atoms);

        Vector3 acceleration = force / mass;

        position += speed * d + 0.5f * acceleration * d * d;

        Vector3 newForce = ComputeForce(atoms);
        Vector3 newAcceleration = newForce / mass;

        speed += 0.5f * (acceleration + newAcceleration) * d;

        force = newForce;

        if (Keyboard.IsKeyPressed(Keyboard.Key.Q))
            speed *= 0.98f;
        else if (Keyboard.IsKeyPressed(Keyboard.Key.E))
            speed.Y += 10f * Const.Delta;

        ApplyWall(ref position.X, ref speed.X, ref force.X, Const.MinEdgeX + radius, Const.MaxEdgeX - radius);
        ApplyWall(ref position.Y, ref speed.Y, ref force.Y, Const.MinEdgeY + radius, Const.MaxEdgeY - radius);
        ApplyWall(ref position.Z, ref speed.Z, ref force.Z, Const.MinEdgeZ + radius, Const.MaxEdgeZ - radius);
    }

    public void ApplyWall(ref float coord, ref float speed, ref float force, float min, float max)
    {
        const float border = 2f;
        const float strength = 500f;

        if (coord < min)
        {
            coord = min + 1;
            if (speed < 0) speed = speed * -0.7f;
            return;
        }

        if (coord > max)
        {
            coord = max - 1;
            if (speed > 0) speed = speed * -0.7f;
            return;
        }

        if (coord < min + border)
        {
            float dist = coord - (min + border);
            force += strength * dist;
        }
        else if (coord > max - border)
        {
            float dist = (max - border) - coord;
            force += strength * dist;
        }
    }

    public void ApplyForce(Vector3 force)
    {
        this.force += force;
    }

    private void TryAddToDragedList()
    {
        float dis = GetDistanceToCursor();

        if (dis > radius * 1.1f) return;

        isDrag = true;
        AtomController.Instance.DragedAtoms.Add(this);
    }

    private void TryRemoveFromDragedList()
    {
        if (!AtomController.Instance.DragedAtoms.Contains(this)) return;

        isDrag = false;
        AtomController.Instance.DragedAtoms.Remove(this);
    }

    public void ChangeMarked() => isMark = !isMark;
    public float GetDistanceToCursor()
    {
        float x1 = Position.X;
        float y1 = Position.Y;

        float x2 = MouseInfo.Instance.MouseWorldPosition.X;
        float y2 = MouseInfo.Instance.MouseWorldPosition.Y;

        return MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }

    private Vector3 ComputeForce(List<Atom> atoms)
    {
        Vector3 force = new Vector3();
        // force += Const.Gravity * Mass;

        foreach (var other in atoms)
        {
            if (other == this) continue;

            float distance = Vector3.Distance(other.position, position);

            if (MathF.Abs(distance * 2 - CovalentRadius + other.CovalentRadius) < 2.5f * Const.Scale && Valence > 0 && other.Valence > 0 && !BondManager.Instance.HasBond(this, other))
            {
                BondManager.Instance.CreateBond(this, other);
            }

            if (distance < 4 * sigma && !BondManager.Instance.HasBond(this, other) && !BondManager.Instance.HasCommon(this, other))
            {
                force += LennardJonesForce(distance, other);
            }
        }

        return force;
    }

    public void Draw(RenderWindow window)
    {
        float visualRadius = radius - (position.Z / Const.MaxEdgeZ) * (radius - 0.5f * radius);

        CircleShape circle = new CircleShape(visualRadius);
        circle.FillColor = color;
        circle.Position = new Vector2f(position.X - visualRadius, position.Y - visualRadius);
        if (isMark)
        {
            circle.OutlineThickness = -2f;
            circle.OutlineColor = Color.Cyan;
        }

        window.Draw(circle);

        if (!isDrag) return;

        VertexArray line = new VertexArray(PrimitiveType.Lines, 2);
        line[0] = new Vertex(new Vector2f(Position.X, Position.Y), color);
        line[1] = new Vertex(new Vector2f(MouseInfo.Instance.MouseWorldPosition.X, MouseInfo.Instance.MouseWorldPosition.Y), color);

        window.Draw(line);
    }

    private Vector3 LennardJonesForce(float distance, Atom other)
    {
        float sr = sigma / distance;
        float sr6 = sr * sr * sr * sr * sr * sr;
        // float energy = 4*epsilon*(sr6*sr6-sr6); //V = 4 * ε * ( (σ / r)^12 - (σ / r)^6 )

        float scalar = 24 * epsilion * (2 * sr6 * sr6 - sr6) / distance;
        Vector3 dir = position - other.position;

        return scalar * dir;
    }
}
*/