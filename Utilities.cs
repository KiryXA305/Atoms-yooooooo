using System.Numerics;
using SFML.System;

namespace Game.Utility;

static class Const
{
    public const double Pixel2Meter = 5.3e-12;
    public const double MassHydrogen = 1.67e-27;
    public const double Joule2Ev = 6.242e18;
    public const float Pixel2Angstrom = 0.12f;


    public static float TimeSpeed = 1f;
    public const float Delta = 0.0005f;
    public static float MinEdgeX = 00;
    public static float MinEdgeY = 00;
    public const float MinEdgeZ = 0;

    public static float MaxEdgeX = 70;
    public static float MaxEdgeY = 70;
    public const float MaxEdgeZ = 5;
    public static readonly Vector3f Gravity = new Vector3f(0, 9.81e-14f, 0);
    public static readonly Random random = new Random();
    public static readonly Vector2u MaxSizeWindow = new Vector2u(1600, 900);

}

static class MyMath
{
    public static float Abs(float x) => x >= 0 ? x : -x;
    public static int Abs(int x) => x >= 0 ? x : -x;
    public static double Abs(double x) => x >= 0 ? x : -x;

    public static float GetDistance(Vector3f first, Vector3f second) => (first - second).Length;

    public static float GetDistance(float x1, float x2, float y1, float y2, float z1, float z2) => MathF.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2));
    public static float GetDistanceSqr(float x1, float x2, float y1, float y2, float z1, float z2) => (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2);

    public static Vector3f GetDelta(float x1, float x2, float y1, float y2, float z1, float z2) => new Vector3f(x2-x1, y2-y1, z2-z1);
}

static class Parameters
{
    public static float VisualRadiusMultiplier = 1f;

    public static bool IsPause = false;
}