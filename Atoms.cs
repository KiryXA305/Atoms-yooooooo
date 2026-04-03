using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Game.Utility;

using Color = SFML.Graphics.Color;
using System.Windows.Forms.VisualStyles;
using System.Numerics;
using System.Diagnostics;
using Simulation;

namespace Game.AtomPhysics;

public class Atoms
{
    public enum AtomVariation
    {
        Hydrogen,
        Oxygen,
        Custom
    }
    private const int MaxBonds = 1000;
    private int[] bondFirst = new int[MaxBonds];
    private int[] bondSecond = new int[MaxBonds];
    private float[] bondDe = new float[MaxBonds];
    private float[] bondA = new float[MaxBonds];
    private float[] bondRe = new float[MaxBonds];
    private float[] bondMaxDist = new float[MaxBonds];
    private int bondCount = 0;

    public void CreateBond(int first, int second)
    {
        if (bondCount >= MaxBonds) return;

        var p = BondParametrs.GetParameters(atomVariation[first], atomVariation[second]);

        bondFirst[bondCount] = first;
        bondSecond[bondCount] = second;
        bondDe[bondCount] = p.de;
        bondA[bondCount] = p.a;
        bondRe[bondCount] = p.re;

        float sqrtTerm = MathF.Sqrt(1 - 0.99f);
        bondMaxDist[bondCount] = p.re + MathF.Log(1 - sqrtTerm) / -p.a;

        valence[first]--;
        valence[second]--;

        bondCount++;
    }

    private void RemoveBond(int bondIndex)
    {
        valence[bondFirst[bondIndex]]++;
        valence[bondSecond[bondIndex]]++;

        int last = bondCount - 1;
        bondFirst[bondIndex] = bondFirst[last];
        bondSecond[bondIndex] = bondSecond[last];
        bondDe[bondIndex] = bondDe[last];
        bondA[bondIndex] = bondA[last];
        bondRe[bondIndex] = bondRe[last];
        bondMaxDist[bondIndex] = bondMaxDist[last];

        bondCount--;
    }

    public float KineticEnergy { get; private set; }
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

    private List<int> valence = new List<int>();

    private List<Color> color = new List<Color>();
    private List<bool> isDraged = new List<bool>();

    private List<AtomVariation> atomVariation = new List<AtomVariation>();

    public int Count
    {
        get => x.Count;
    }

    public void CreateAtom(Vector3f pos, AtomVariation variation = AtomVariation.Custom)
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
            case AtomVariation.Hydrogen:
                mass.Add(1);
                radius.Add(1);
                color.Add(Color.White);
                epsilion.Add(0.1f);
                sigma.Add(3f);
                valence.Add(1);
                covalentRadius.Add(0.37f);
                break;
            case AtomVariation.Oxygen:
                mass.Add(16);
                radius.Add(1.2f);
                color.Add(Color.Red);
                epsilion.Add(0.16f);
                sigma.Add(2.6f);
                valence.Add(2);
                covalentRadius.Add(0.73f);
                break;

            case AtomVariation.Custom:
                mass.Add(1);
                radius.Add(1);
                color.Add(Color.Yellow);
                epsilion.Add(1f);
                sigma.Add(3f);
                valence.Add(1);
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

                if (r < radius[i] * radius[i] * 1.3f)
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
            }
        };
    }

    public float GetAtomKineticEnergy(int i) => 0.5f * mass[i] * (vx[i] * vx[i] + vy[i] * vy[i] + vz[i] * vz[i]);
    public void Update(float delta)
    {
        KineticEnergy = 0;
        mousePosition = Programm.Instance.Window.MapPixelToCoords(Mouse.GetPosition(Programm.Instance.Window)) * Const.Pixel2Angstrom;

        UpdateBonds();
        TryFormBonds();

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

    private void UpdateBonds()
    {
        for (int i = bondCount - 1; i >= 0; i--)
        {
            int f = bondFirst[i];
            int s = bondSecond[i];

            float dis = MyMath.GetDistance(x[f], x[s], y[f], y[s], z[f], z[s]);
            float dr = dis - bondRe[i];

            float exp = MathF.Exp(-bondA[i] * dr );
            float pE = bondDe[i] * (1 - exp) * (1 - exp);

            if (pE > bondDe[i]*0.9f)
            {
                RemoveBond(i);
                continue;
            }

            exp = MathF.Exp(-bondA[i] * dr);
            float force = 2 * bondDe[i] * bondA[i] * (exp * exp - exp);

            float dx = x[s] - x[f];
            float dy = y[s] - y[f];
            float dz = z[s] - z[f];
            Vector3f dir = new Vector3f(dx / dis, dy / dis, dz / dis);

            ApplyForce(f, dir * force);
            ApplyForce(s, dir * -force);
        }
    }

    private void TryFormBonds()
    {
        for (int i = 0; i < Count; i++)
        {
            if (valence[i] <= 0) continue;

            for (int j = i + 1; j < Count; j++)
            {
                if (valence[j] <= 0) continue;
                if (HasBond(i, j)) continue;

                float dis = MyMath.GetDistance(x[i], x[j], y[i], y[j], z[i], z[j]);
                float bondThreshold = (covalentRadius[i] + covalentRadius[j]) * 1.2f;

                if (dis < bondThreshold)
                    CreateBond(i, j);
            }
        }
    }

    private bool HasBond(int i, int j)
    {
        for (int k = 0; k < bondCount; k++)
            if ((bondFirst[k] == i && bondSecond[k] == j) ||
                (bondFirst[k] == j && bondSecond[k] == i))
                return true;
        return false;
    }

    public void Draw(RenderWindow window)
    {   
        float maxVX = vx.Max();
        float maxVY = vy.Max();
        float maxVZ = vz.Max();

        float newMaxVelocity = MathF.Abs(maxVX * maxVX + maxVY * maxVY + maxVZ + maxVZ);
        maxVelocity = maxVelocity * 0.999f + newMaxVelocity * 0.001f;

        DrawBonds(window);

        for (int i = 0; i < Count; i++)
        {
            Vector2f position = new Vector2f(x[i] / Const.Pixel2Angstrom, y[i] / Const.Pixel2Angstrom);

            float r = radius[i] / 2;
            float visualRadius = r / (Const.Pixel2Angstrom * 3) - (z[i] / Const.MaxEdgeZ) * (r / (Const.Pixel2Angstrom * 3) - 0.5f * r / (Const.Pixel2Angstrom * 3));

            CircleShape circle = new CircleShape(visualRadius);

            var v = MathF.Sqrt(vx[i] * vx[i] + vy[i] * vy[i] + vz[i] * vz[i]);
            var t = v / maxVelocity;

            var accelerationColor = new Color((byte)(t * 255), 0, (byte)((1 - t) * 255));

            circle.FillColor = accelerationColor;
            circle.Position = position - new Vector2f(visualRadius / 2, visualRadius / 2);

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

    private void DrawBonds(RenderWindow window)
    {
        VertexArray lines = new VertexArray(PrimitiveType.Lines, (uint)(bondCount * 2));

        for (int i = 0; i < bondCount; i++)
        {
            int f = bondFirst[i];
            int s = bondSecond[i];

            Vector2f posF = new Vector2f(x[f] / Const.Pixel2Angstrom, y[f] / Const.Pixel2Angstrom);
            Vector2f posS = new Vector2f(x[s] / Const.Pixel2Angstrom, y[s] / Const.Pixel2Angstrom);

            lines[(uint)(i * 2)] = new Vertex(posF, color[f]);
            lines[(uint)(i * 2 + 1)] = new Vertex(posS, color[s]);
        }

        window.Draw(lines);
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

        float border = 1;
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

    private void ApplyForce(int i, Vector3f force) => applyForce[i] += force;

    private void LennardJonesForce(int first, int second)
    {
        if (HasBond(first, second)) return;

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
        ApplyForce(i, delta * mass[i] / Const.TimeSpeed * 5f);
    }

    public void CreateHydrogen(Vector3f position) => CreateAtom(position, AtomVariation.Hydrogen);
    public void CreateOxygen(Vector3f position) => CreateAtom(position, AtomVariation.Oxygen);

    public void CreateCustom(Vector3f position) => CreateAtom(position, AtomVariation.Custom);
}

