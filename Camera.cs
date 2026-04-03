using SFML.System;
using SFML.Graphics;
using SFML.Window;
using Game.Utility;

namespace CameraUtility;

public class Camera
{
    public View cameraView { get; private set; }

    private Vector2f initialViewSize;

    public Camera()
    {
        cameraView = new View((Vector2f)Const.MaxSizeWindow / 2f, (Vector2f)Const.MaxSizeWindow);
        initialViewSize = cameraView.Size;
    }

    public float GetZoomValue()
    {
        return initialViewSize.X / cameraView.Size.X / 2;
    }

    public void Reset()
    {
        cameraView.Move(-1 * ( cameraView.Center - new Vector2f(Const.MinEdgeX * 5, Const.MaxEdgeY * 5)));
    }

    public void Move(Vector2f offset)
    {
        cameraView.Move(offset);
    }
    public void Zoom(float zoomy)
    {
        cameraView.Zoom(zoomy);
    }
}