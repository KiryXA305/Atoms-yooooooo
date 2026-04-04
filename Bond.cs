using Game.Utility;
using SFML.Graphics;
using SFML.System;
using Simulation;

namespace Game.AtomPhysics;

public class Bonds
{
    private const int MaxBonds = 1000;
    private const float BondK = 1000f;
    private (int first, int second)[] bonds = new (int first, int second)[MaxBonds];
    private float[] bondDe = new float[MaxBonds];
    private float[] bondA = new float[MaxBonds];
    private float[] bondRe = new float[MaxBonds];
    private float[] bondMaxDist = new float[MaxBonds];
    private int bondCount = 0;

    private Atoms atoms => Programm.Instance.Atoms;

    public void CreateBond(int first, int second)
    {
        if (bondCount >= MaxBonds) return;

        var p = BondParametrs.GetParameters(atoms.AtomVariation[first], atoms.AtomVariation[second]);

        bonds[bondCount] = (first, second);
        bondDe[bondCount] = p.de;
        bondA[bondCount] = p.a;
        bondRe[bondCount] = p.re;

        float sqrtTerm = MathF.Sqrt(1 - 0.99f);
        bondMaxDist[bondCount] = p.re + MathF.Log(1 - sqrtTerm) / -p.a;

        atoms.Valence[first]--;
        atoms.Valence[second]--;

        bondCount++;
    }
    public void Update()
    {
        for (int i = bondCount - 1; i >= 0; i--)
        {
            int f = bonds[i].first;
            int s = bonds[i].second;

            float dis = MyMath.GetDistance(atoms.X[f], atoms.X[s], atoms.Y[f], atoms.Y[s], atoms.Z[f], atoms.Z[s]);
            Vector3f dir;
            float force;

            if (dis < (atoms.CovalentRadius[f] + atoms.CovalentRadius[s]) * 0.8f)
            {
                dir = MyMath.GetDelta(atoms.X[f], atoms.X[s], atoms.Y[f], atoms.Y[s], atoms.Z[f], atoms.Z[s]) / dis;

                float p = ((atoms.CovalentRadius[f] + atoms.CovalentRadius[s]) * 0.8f) - dis;
                float p2 = p * p;
                float p4 = p2 * p2;
                force = BondK * bondDe[i] * p4 * p2;

                atoms.ApplyForce(f, dir * -force);
                atoms.ApplyForce(s, dir * force);
                continue;
            }

            float dr = dis - bondRe[i];

            float exp = MathF.Exp(-bondA[i] * dr);
            float pE = bondDe[i] * (1 - exp) * (1 - exp);

            if (pE > bondDe[i] * 0.94f)
            {
                RemoveBond(i);
                continue;
            }
            force = 2 * bondDe[i] * bondA[i] * (exp * exp - exp);

            dir = MyMath.GetDelta(atoms.X[f], atoms.X[s], atoms.Y[f], atoms.Y[s], atoms.Z[f], atoms.Z[s]) / dis;

            atoms.ApplyForce(f, dir * force);
            atoms.ApplyForce(s, dir * -force);
        }
    }
    public bool HasBond(int i, int j)
    {
        return bonds.Contains((i, j)) || bonds.Contains((j, i));
    }


    public void RemoveBond(int bondIndex)
    {
        Programm.Instance.Atoms.Valence[bonds[bondIndex].first]++;
        Programm.Instance.Atoms.Valence[bonds[bondIndex].second]++;

        int last = bondCount - 1;
        bonds[bondIndex] = bonds[last];
        bondDe[bondIndex] = bondDe[last];
        bondA[bondIndex] = bondA[last];
        bondRe[bondIndex] = bondRe[last];
        bondMaxDist[bondIndex] = bondMaxDist[last];

        bonds[last] = (-1, -1);

        bondCount--;
    }
    
    public void Draw(RenderWindow window)
    {
        VertexArray lines = new VertexArray(PrimitiveType.Lines, (uint)(bondCount * 2));

        for (int i = 0; i < bondCount; i++)
        {
            int f = bonds[i].first;
            int s = bonds[i].second;

            Vector2f posF = new Vector2f(atoms.X[f] / Const.Pixel2Angstrom, atoms.Y[f] / Const.Pixel2Angstrom);
            Vector2f posS = new Vector2f(atoms.X[s] / Const.Pixel2Angstrom, atoms.Y[s] / Const.Pixel2Angstrom);

            lines[(uint)(i * 2)] = new Vertex(posF, atoms.Color[f]);
            lines[(uint)(i * 2 + 1)] = new Vertex(posS, atoms.Color[s]);
        }

        window.Draw(lines);
    }
}

public static class BondParametrs
{
    public static Dictionary<(Atoms.Atom, Atoms.Atom), (float de, float a, float r0)> parameters = new Dictionary<(Atoms.Atom, Atoms.Atom), (float de, float a, float r0)>
    {
        {(Atoms.Atom.Hydrogen, Atoms.Atom.Hydrogen), (4.75f, 1.94f, 0.741f)},
        {(Atoms.Atom.Oxygen, Atoms.Atom.Hydrogen), (4.80f, 2.2f, 0.96f)},
        {(Atoms.Atom.Oxygen, Atoms.Atom.Oxygen), (5.21f, 2.67f, 1.20f) },
        {(Atoms.Atom.Custom, Atoms.Atom.Custom), (4f, 2f, 0.7f)},
    };

    public static (float de, float a, float re) GetParameters(Atoms.Atom first, Atoms.Atom second)
    {
        var key = first < second ? (second, first) : (first, second);
        if (parameters.TryGetValue(key, out var value))
            return value;
        else
            return parameters[(Atoms.Atom.Custom, Atoms.Atom.Custom)];
    }

    public static bool HasKey(Atoms.Atom first, Atoms.Atom second)
    {
        var key = first < second ? (first, second) : (second, first);
        return parameters.TryGetValue(key, out var _);
    }
}
