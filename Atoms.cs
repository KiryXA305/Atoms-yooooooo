using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Game.Utility;
using Color = SFML.Graphics.Color;
using Simulation;

namespace Game.AtomPhysics;

public class Atoms
{
    public enum Atom
    {
        Hydrogen,
        Oxygen,
        Custom
    }

    public float KineticEnergy { get; private set; }
    public float PotentialEnergy { get; private set; }
    public List<Atom> AtomVariation => atomVariation;
    public List<float> X => x;
    public List<float> Y => y;
    public List<float> Z => z;
    public List<float> CovalentRadius => covalentRadius;
    public List<Color> Color => color;

    public List<int> Valence = new List<int>();

    private Vector2f mousePosition;
    private float maxVelocity;

    private List<float> x = new List<float>();
    private List<float> y = new List<float>();
    private List<float> z = new List<float>();
    private List<float> vx = new List<float>();
    private List<float> vy = new List<float>();
    private List<float> vz = new List<float>();
    private List<float> fx = new List<float>();
    private List<float> fy = new List<float>();
    private List<float> fz = new List<float>();

    private List<Vector3f> applyForce = new List<Vector3f>();

    private List<float> radius = new List<float>();
    private List<float> covalentRadius = new List<float>();
    private List<float> mass = new List<float>();
    private List<float> epsilion = new List<float>();
    private List<float> sigma = new List<float>();

    private List<Color> color = new List<Color>();
    private List<bool> isDraged = new List<bool>();

    private List<Atom> atomVariation = new List<Atom>();

    public int Count
    {
        get => x.Count;
    }

    public void CreateAtom(Vector3f pos, Atom variation = Atom.Custom)
    {
        x.Add(pos.X);
        y.Add(pos.Y);
        z.Add(pos.Z);

        atomVariation.Add(variation);

        vx.Add(0);
        vy.Add(0);
        vz.Add(0);
        fx.Add(0);
        fy.Add(0);
        fz.Add(0);
        isDraged.Add(false);

        applyForce.Add(new Vector3f(0, 0, 0));

        switch (variation)
        {
            case Atom.Hydrogen:
                mass.Add(1);
                radius.Add(1);
                color.Add(SFML.Graphics.Color.White);
                epsilion.Add(0.1f);
                sigma.Add(3f);
                Valence.Add(1);
                covalentRadius.Add(0.37f);
                break;
            case Atom.Oxygen:
                mass.Add(16);
                radius.Add(1.2f);
                color.Add(SFML.Graphics.Color.Red);
                epsilion.Add(0.16f);
                sigma.Add(2.6f);
                Valence.Add(2);
                covalentRadius.Add(0.73f);
                break;

            case Atom.Custom:
                mass.Add(1);
                radius.Add(1f);
                color.Add(SFML.Graphics.Color.Yellow);
                epsilion.Add(0.1f);
                sigma.Add(3f);
                Valence.Add(1);
                covalentRadius.Add(0.37f);
                break;
        }

        int i = Count - 1;

        Programm.Instance.Window.MouseButtonPressed += (s, e) =>
        {
            if (e.Button == Mouse.Button.Left)
            {
                float dx = mousePosition.X - x[i];
                float dy = mousePosition.Y - y[i];

                float r = dx * dx + dy * dy;

                if (r < radius[i] * radius[i])
                {
                    isDraged[i] = true;
                }
            }
        };
        Programm.Instance.Window.MouseButtonReleased += (s, e) =>
        {
            if (e.Button == Mouse.Button.Left)
                isDraged[i] = false;
        };
        Programm.Instance.Window.KeyPressed += (s, e) =>
        {
            switch (e.Code)
            {
                case Keyboard.Key.Q:
                    vx[i] *= 0.9f;
                    vy[i] *= 0.9f;
                    vz[i] *= 0.9f;
                    break;
                case Keyboard.Key.E:
                    vx[i] /= 0.9f;
                    vy[i] /= 0.9f;
                    vz[i] /= 0.9f;
                    break;
            }
        };
    }

    public float GetAtomKineticEnergy(int i) => 0.5f * mass[i] * (vx[i] * vx[i] + vy[i] * vy[i] + vz[i] * vz[i]);
    public void Update(float delta)
    {
        KineticEnergy = 0;
        PotentialEnergy = 0;
        mousePosition = Programm.Instance.Window.MapPixelToCoords(Mouse.GetPosition(Programm.Instance.Window)) * Const.Pixel2Angstrom;

        for (int i = 0; i < Count; i++)
        {
            Vector3f f = ComputeForce(i);

            fx[i] = f.X;
            fy[i] = f.Y;
            fz[i] = f.Z;

            float aX = fx[i] / mass[i];
            float aY = fy[i] / mass[i];
            float aZ = fz[i] / mass[i];

            x[i] += vx[i] * delta + 0.5f * aX * delta * delta;
            y[i] += vy[i] * delta + 0.5f * aY * delta * delta;
            z[i] += vz[i] * delta + 0.5f * aZ * delta * delta;

            Vector3f newF = ComputeForce(i);
            float newAX = newF.X / mass[i];
            float newAY = newF.Y / mass[i];
            float newAZ = newF.Z / mass[i];

            vx[i] += 0.5f * (aX + newAX) * delta;
            vy[i] += 0.5f * (aY + newAY) * delta;
            vz[i] += 0.5f * (aZ + newAZ) * delta;

            fx[i] = newF.X;
            fy[i] = newF.Y;
            fz[i] = newF.Z;

            vx[i] *= 0.99999f;
            vy[i] *= 0.99999f;
            vz[i] *= 0.99999f;

            SoftWalls(i);

            KineticEnergy += GetAtomKineticEnergy(i);
        }
    }
    public void Draw(RenderWindow window)
    {   
        float maxVX = vx.Max();
        float maxVY = vy.Max();
        float maxVZ = vz.Max();

        float newMaxVelocity = MathF.Abs(maxVX * maxVX + maxVY * maxVY + maxVZ + maxVZ);
        maxVelocity = maxVelocity * 0.999f + newMaxVelocity * 0.001f;
        for (int i = 0; i < Count; i++)
        {
            Vector2f position = new Vector2f(x[i] / Const.Pixel2Angstrom, y[i] / Const.Pixel2Angstrom);

            float r = radius[i] * Parameters.VisualRadiusMultiplier;
            float visualRadius = r / (Const.Pixel2Angstrom * 3) - (z[i] / Const.MaxEdgeZ) * (r / (Const.Pixel2Angstrom * 3) - 0.5f * r / (Const.Pixel2Angstrom * 3));

            CircleShape circle = new CircleShape(visualRadius);

            var v = MathF.Sqrt(vx[i] * vx[i] + vy[i] * vy[i] + vz[i] * vz[i]);
            var t = v / maxVelocity;

            var accelerationColor = new Color((byte)(t * 255), 0, (byte)((1 - t) * 255));

            circle.FillColor = color[i];
            circle.Position = position - new Vector2f(visualRadius, visualRadius);

            if (isDraged[i])
            {
                VertexArray dragLine = new VertexArray(PrimitiveType.Lines, 2);
                dragLine[0] = new Vertex(position, circle.FillColor);
                dragLine[1] = new Vertex(mousePosition / Const.Pixel2Angstrom, circle.FillColor);

                window.Draw(dragLine);
            }

            window.Draw(circle);
        }
    }

    public void SoftWalls(int i)
    {
        (float, float, float) paramX = ApplyWall(i, Const.MinEdgeX, Const.MaxEdgeX, "x");
        (float, float, float) paramY = ApplyWall(i, Const.MinEdgeY, Const.MaxEdgeY, "y");
        (float, float, float) paramZ = ApplyWall(i, Const.MinEdgeZ, Const.MaxEdgeZ, "z");

        x[i] = paramX.Item1;
        y[i] = paramY.Item1;
        z[i] = paramZ.Item1;

        vx[i] = paramX.Item2;
        vy[i] = paramY.Item2;
        vz[i] = paramZ.Item2;
    }

    public (float coord, float vel, float force) ApplyWall(int i, float min, float max, string axis)
    {
        float coord;
        float vel;
        float force;

        switch (axis.ToLower())
        {
            case "x":
                coord = x[i];
                vel = vx[i];
                force = fx[i];
                break;
            case "y":
                coord = y[i];
                vel = vy[i];
                force = fy[i];
                break;
            default:
            case "z":
                coord = z[i];
                vel = vz[i];
                force = fz[i];
                break;
        }

        if (coord > max - radius[i])
        {
            coord = max - radius[i];
            if (vel > 0) vel = -vel;
            return (coord, vel, force);
        }

        if (coord < min + radius[i])
        {
            coord = min + radius[i];
            if (vel < 0) vel = -vel;
            return (coord, vel, force);
        }

        float border = 2.5f;
        float strength = 500;

        if (coord < min + border + radius[i])
        {
            float penetration = (min + border + radius[i]) - coord;
            float p2 = penetration * penetration;
            float p4 = p2 * p2;

            force += strength * mass[i] * p4 * p2;
        } else if (coord > max - border - radius[i])
        {
            float penetration = coord - (max - border - radius[i]);
            float p2 = penetration * penetration;
            float p4 = p2 * p2;

            force -= strength * mass[i] * p4 * p2;
        }


        return (coord, vel, force);
    }


    public Vector3f ComputeForce(int i)
    {
        Vector3f total_force = Const.Gravity * mass[i];

        if (isDraged[i])
        {
            DragForce(i);
        }

        for (int j = i + 1; j < x.Count; j++)
        {
            float dis = MyMath.GetDistanceSqr(x[i], x[j], y[i], y[j], z[i], z[j]);

            if (dis < 4 * sigma[i] * 4 * sigma[i])
            {
                LennardJonesForce(i, j);
            }
        }

        var aF = applyForce[i];
        if (aF.X * aF.X + aF.Y * aF.Y + aF.Z * aF.Z > 0)
        {
            total_force += applyForce[i];
            applyForce[i] = new Vector3f(0, 0, 0);
        }

        return total_force;
    }

    public void ApplyForce(int i, Vector3f force) => applyForce[i] += force;

    private void LennardJonesForce(int first, int second)
    {
        if (Programm.Instance.Bonds.HasBond(first, second)) return;

        float dis = MyMath.GetDistance(x[first], x[second], y[first], y[second], z[first], z[second]);

        float s = (sigma[first] + sigma[second]) / 2;
        float e = MathF.Sqrt(epsilion[first] * epsilion[second]);

        float sd = s / dis;
        float sd6 = sd * sd * sd * sd * sd * sd;
        float scalar = 24 * e * (2 * sd6 * sd6 - sd6) / (dis * dis);

        float dx = x[second] - x[first];
        float dy = y[second] - y[first];
        float dz = z[second] - z[first];

        Vector3f dir = new Vector3f(dx, dy, dz);

        ApplyForce(first, dir * -scalar);
        ApplyForce(second, dir * scalar);
    }

    private void DragForce(int i)
    {
        Vector3f delta = new Vector3f(mousePosition.X - x[i], mousePosition.Y - y[i], 0);
        ApplyForce(i, delta * mass[i] * 5f);
    }

    public void CreateHydrogen(Vector3f position) => CreateAtom(position, Atom.Hydrogen);
    public void CreateOxygen(Vector3f position) => CreateAtom(position, Atom.Oxygen);

    public void CreateCustom(Vector3f position) => CreateAtom(position, Atom.Custom);
}

