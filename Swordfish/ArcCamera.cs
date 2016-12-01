using System;
using SharpDX;

namespace Swordfish
{
    class ArcCamera
    {
        private Vector3 position;
        private Vector3 target;

        private float distance;
        private float minDistance;
        private float maxDistance;

        private float xRotation;
        private float yRotation;
        private float yMin;
        private float yMax;

        private DXInput dInput;

        public ArcCamera()
        {
            target = new Vector3(0.0f, 0.0f, 0.0f);
            position = new Vector3(50.0f, 30.0f, 40.0f);

            SetDistance(5.0f, 1.0f, 200.0f);
            SetRotation(0.0f, 0.0f, (float)(-1 * Math.PI / 2.0f), (float)Math.PI / 2.0f);
        }

        public void SetDistance(float dist, float minDist, float maxDist)
        {
            distance = dist;
            minDistance = minDist;
            maxDistance = maxDist;

            if (distance < minDistance)
            {
                distance = minDistance;
            }

            if (distance > maxDistance)
            {
                distance = maxDistance;
            }
        }

        public void SetRotation(float x, float y, float minY, float maxY)
        {
            xRotation = x;
            yRotation = y;
            yMin = minY;
            yMax = maxY;

            if (yRotation < yMin)
            {
                yRotation = yMin;
            }

            if (yRotation > yMax)
            {
                yRotation = yMax;
            }
        }

        public void SetTarget(Vector3 tgt)
        {
            target = tgt;
        }

        public void ApplyZoom(float zoomDelta)
        {
            distance += zoomDelta;

            if (distance < minDistance)
            {
                distance = minDistance;
            }

            if (distance > maxDistance)
            {
                distance = maxDistance;
            }
        }

        public void ApplyRotation(float yawDelta, float pitchDelta)
        {
            xRotation += pitchDelta;
            yRotation += yawDelta;

            if (xRotation < yMin)
            {
                xRotation = yMin + 0.0001f;
            }

            if (xRotation > yMax)
            {
                xRotation = yMax - 0.0001f;
            }
        }

        public Matrix GetViewMatrix()
        {
            Vector4 zoom = new Vector4(0.0f, 0.0f, distance, 1.0f);
            Matrix rotation = Matrix.RotationYawPitchRoll(yRotation, -xRotation, 0.0f);
            zoom = Vector4.Transform(zoom, rotation);

            Vector4 pos = new Vector4(position[0], position[1], position[2], 0.0f);
            Vector4 lookAt = new Vector4(target[0], target[1], target[2], 0.0f);

            pos = lookAt + zoom;
            position[0] = pos[0];
            position[1] = pos[1];
            position[2] = pos[2];

            Vector4 up = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            up = Vector4.Transform(up, rotation);

            Vector3 pos2 = new Vector3(pos[0], pos[1], pos[2]);
            Vector3 lookAt2 = new Vector3(lookAt[0], lookAt[1], lookAt[2]);

            Matrix viewMatrix = Matrix.LookAtLH(pos2, lookAt2, Vector3.Up);

            return viewMatrix;
        }

        public void Update()
        {

        }

        public void setDXInput(ref DXInput di)
        {
            dInput = di;
        }
    }
}
