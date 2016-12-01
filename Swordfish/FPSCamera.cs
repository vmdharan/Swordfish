using System;
using SharpDX;

namespace Swordfish
{
    class FPSCamera
    {
        private Matrix mView;
        private Matrix mProj;

        private Vector3 vRight;
        private Vector3 vUp;
        private Vector3 vLook;

        private Vector3 vPosition;
        private Vector3 vLookAt;

        private float yaw;
        private float pitch;
        private float maxPitch;
        private float minPitch;


        private DXInput dInput;

        public FPSCamera()
        {
            vPosition = new Vector3(0.0f, 2.0f, -10.0f);
            vUp = new Vector3(0.0f, 1.0f, 0.0f);
            vRight = new Vector3(1.0f, 0.0f, 0.0f);
            vLook = new Vector3(0.0f, 0.0f, 1.0f);

            mView = Matrix.Identity;
            mProj = Matrix.Identity;

        }

        // Get the camera X-coordinate.
        public float getCamX()
        {
            return vPosition.X;
        }

        // Get the camera Y-coordinate.
        public float getCamY()
        {
            return vPosition.Y;
        }

        // Get the camera Z-coordinate.
        public float getCamZ()
        {
            return vPosition.Z;
        }

        public void setProjectionMatrix(int w, int h)
        {
            mProj = Matrix.PerspectiveFovLH((float)MathUtil.Pi / 4.0f,
                ((float)w / (float)h), 0.1f, 1000.0f);
        }

        public void lookAt(Vector3 pos, Vector3 target, Vector3 up)
        {
            vPosition = pos;
            vLook = Vector3.Normalize(target - pos);
            vRight = Vector3.Normalize(Vector3.Cross(up, vLook));
            vUp = Vector3.Cross(vLook, vRight);
        }

        public void setCamY(float y)
        {
            vPosition.Y = y;
        }

        public void MoveForward(float units)
        {
            vPosition += vLook * units;
        }

        public void Strafe(float units)
        {
            vPosition += vRight * units;
        }

        public void MoveVertically(float units)
        {
            vPosition += vUp * units;
        }

        public void Pitch(float angle)
        {
            Matrix rot = Matrix.RotationAxis(vRight, angle);
            vUp = Vector3.TransformNormal(vUp, rot);
            vLook = Vector3.TransformNormal(vLook, rot);
        }

        public void Yaw(float angle)
        {
            Matrix rot = Matrix.RotationY(angle);
            vRight = Vector3.TransformNormal(vRight, rot);
            vUp = Vector3.TransformNormal(vUp, rot);
            vLook = Vector3.TransformNormal(vLook, rot);
        }

        public void UpdateViewMatrix()
        {
            Vector3 r = vRight;
            Vector3 u = vUp;
            Vector3 l = vLook;
            Vector3 p = vPosition;

            l = Vector3.Normalize(l);
            u = Vector3.Normalize(Vector3.Cross(l, r));

            r = Vector3.Cross(u, l);

            float x = -Vector3.Dot(p, r);
            float y = -Vector3.Dot(p, u);
            float z = -Vector3.Dot(p, l);

            vRight = r;
            vUp = u;
            vLook = l;

            Matrix v = new Matrix();
            v[0, 0] = vRight.X;
            v[1, 0] = vRight.Y;
            v[2, 0] = vRight.Z;
            v[3, 0] = x;

            v[0, 1] = vUp.X;
            v[1, 1] = vUp.Y;
            v[2, 1] = vUp.Z;
            v[3, 1] = y;

            v[0, 2] = vLook.X;
            v[1, 2] = vLook.Y;
            v[2, 2] = vLook.Z;
            v[3, 2] = z;

            v[0, 3] = 0.0f;
            v[1, 3] = 0.0f;
            v[2, 3] = 0.0f;
            v[3, 3] = 1.0f;

            mView = v;
        }

        public Matrix GetViewMatrix()
        {
            UpdateViewMatrix();

            return mView;
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
