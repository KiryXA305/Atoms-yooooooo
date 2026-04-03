using System.Numerics;
using System.Reflection.Metadata;
using Game.Utility;
using SFML.Graphics;
using SFML.System;
using Simulation;

namespace Game.AtomPhysics;

public static class BondParametrs
{
    public static Dictionary<(Atoms.AtomVariation, Atoms.AtomVariation), (float de, float a, float r0)> parameters = new Dictionary<(Atoms.AtomVariation, Atoms.AtomVariation), (float de, float a, float r0)>
    {
        {(Atoms.AtomVariation.Hydrogen, Atoms.AtomVariation.Hydrogen), (4.75f, 1.94f, 0.741f)},
        {(Atoms.AtomVariation.Oxygen, Atoms.AtomVariation.Hydrogen), (4.80f, 2.2f, 0.96f)},
        {(Atoms.AtomVariation.Oxygen, Atoms.AtomVariation.Oxygen), (5.21f, 2.67f, 1.20f) },
        {(Atoms.AtomVariation.Custom, Atoms.AtomVariation.Custom), (4f, 2f, 0.7f)},
    };

    public static (float de, float a, float re) GetParameters(Atoms.AtomVariation first, Atoms.AtomVariation second)
    {
        var key = first < second ? (second, first) : (first, second);
        if (parameters.TryGetValue(key, out var value))
            return value;
        else
            return parameters[(Atoms.AtomVariation.Custom, Atoms.AtomVariation.Custom)];
    }

    public static bool HasKey(Atoms.AtomVariation first, Atoms.AtomVariation second)
    {
        var key = first < second ? (first, second) : (second, first);
        return parameters.TryGetValue(key, out var _);
    }
}