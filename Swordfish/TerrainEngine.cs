using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swordfish
{
    class TerrainEngine
    {
        private int TERRAIN_SIZE;
        private int RANDOM_SEED;
        private float ROUGHNESS;
        private float MAXHEIGHT;
        private int PD_INSTANCES;
        private float TSCALE;

        private System.Random rnd;
        private PerlinNoise pn;

        public struct HeightMap
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector4 colour;
        }

        public HeightMap[,] heightmap;

        // Constructor
        public TerrainEngine()
        {
            // Initialise all parameters.
            TERRAIN_SIZE = 129;
            RANDOM_SEED = 11092016;
            ROUGHNESS = 0.85f;
            MAXHEIGHT = 10.0f;
            PD_INSTANCES = 1000;
            TSCALE = 0.025f;

            // Initialise the random number generator with a seed.
            rnd = new Random(RANDOM_SEED);

            // Initialise the heightmap.
            heightmap = new HeightMap[TERRAIN_SIZE, TERRAIN_SIZE];
            for (int i = 0; i < TERRAIN_SIZE; i++)
            {
                for (int j = 0; j < TERRAIN_SIZE; j++)
                {
                    heightmap[i, j] = new HeightMap()
                    {
                        position = new Vector3(i, 0.0f, j),
                        normal = new Vector3(0.0f, 0.0f, 0.0f),
                        colour = new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
                    };
                }
            }

            // Generate terrain using random midpoint displacement
            RMPD();

            // Generate terrain using particle deposition
            //ParticleDeposition();

            // Generate terrain using Perlin Noise algorithm.
            double pow = 4.0;
            pn = new PerlinNoise();
            for (int i = 0; i < TERRAIN_SIZE; i++)
            {
                for (int j = 0; j < TERRAIN_SIZE; j++)
                {
                    double nx = i;
                    double ny = j;
                    double nz = heightmap[i, j].position.Y;

                    double e = 1 * pn.noise(1 * nx, 1 * ny, 1 * nz)
                            + 0.5 * pn.noise(2 * nx, 2 * ny, 2 * nz)
                            + 0.25 * pn.noise(4 * nx, 4 * ny, 4 * nz)
                            + 0.125 * pn.noise(8 * nx, 8 * ny, 8 * nz);

                    //heightmap[i, j].position.Y += (float) pn.noise(i, j, heightmap[i, j].position.Y);
                    double h = Math.Pow(e, pow);
                    heightmap[i, j].position.X += (float)h;
                    heightmap[i, j].position.Y += (float)h;
                    heightmap[i, j].position.Z += (float)h;
                }
            }

            // Translate to origin
            TTO();

            // Smooth terrain
            //smooth(); smooth(); smooth(); smooth(); smooth();

            // Calculate normals
            setNormals();

            // Calculate vertex normals
            vertexNormals();
        }

        // Get Terrain size.
        public int getTerrainSize()
        {
            return TERRAIN_SIZE;
        }

        // Random Midpoint Displacement.
        public void RMPD()
        {
            Vector3 TL, TR, BR, BL;  // Corner vertices
            double range;             // Height range

            range = MAXHEIGHT;

            // -------- Generate random values for the corner points -------- //

            // Top left corner of terrain
            TL.X = 0;
            TL.Z = 0;
            TL.Y = (float)randomHeight(range);

            // Top right corner of terrain
            TR.X = 0;
            TR.Z = TERRAIN_SIZE - 1;
            TR.Y = (float)randomHeight(range);

            // Bottom left corner of terrain
            BL.X = TERRAIN_SIZE - 1;
            BL.Z = 0;
            BL.Y = (float)randomHeight(range);

            // Bottom right corner of terrain
            BR.X = TERRAIN_SIZE - 1;
            BR.Z = TERRAIN_SIZE - 1;
            BR.Y = (float)randomHeight(range);

            // -------------------------------------------------------------- //

            // Begin the main part of the algorithm
            RMPD2(TL, TR, BL, BR, range);
        }

        // Recursive step for RMPD.
        public void RMPD2(Vector3 TL, Vector3 TR, Vector3 BL, Vector3 BR, double range)
        {
            Vector3 TM, BM, LM, RM, CTR;  // Middle vertices
            double range2;                 // Height range

            CTR = new Vector3(0.0f, 0.0f, 0.0f);
            TM = new Vector3(0.0f, 0.0f, 0.0f);
            BM = new Vector3(0.0f, 0.0f, 0.0f);
            LM = new Vector3(0.0f, 0.0f, 0.0f);
            RM = new Vector3(0.0f, 0.0f, 0.0f);

            // -------------- Calculate the midpoint values ----------------- //

            // Centre
            MID_CTR(ref TL, ref TR, ref BL, ref BR, ref CTR, range);

            // Left middle
            MID(ref LM, ref TL, ref BL, ref CTR, range);

            // Right middle
            MID(ref RM, ref TR, ref BR, ref CTR, range);

            // Top middle
            MID(ref TM, ref TL, ref TR, ref CTR, range);

            // Bottom middle
            MID(ref BM, ref BL, ref BR, ref CTR, range);

            // -------------------------------------------------------------- //

            // Update heightmap
            ASG_HMAP(ref TL);
            ASG_HMAP(ref TR);
            ASG_HMAP(ref BL);
            ASG_HMAP(ref BR);
            ASG_HMAP(ref LM);
            ASG_HMAP(ref RM);
            ASG_HMAP(ref TM);
            ASG_HMAP(ref BM);
            ASG_HMAP(ref CTR);

            // Reduce height range
            range2 = Math.Pow(2, -ROUGHNESS) * range;

            // Recursive call
            if ((TM.Z - TL.Z) != 1.0)
            {
                RMPD2(TL, TM, LM, CTR, range2);
                RMPD2(TM, TR, CTR, RM, range2);
                RMPD2(LM, CTR, BL, BM, range2);
                RMPD2(CTR, RM, BM, BR, range2);
            }
        }

        // Generate a random height value between 0 and range.
        public double randomHeight(double range)
        {
            double height;
            //height = rnd.NextDouble(0, range);
            height = NormalDist(range, range / 32.0);

            return height;
        }

        // Generate a random sign.
        public int sign()
        {
            int num;
            num = rnd.Next(0, 2);

            if ((num % 2) == 0)
            {
                return 1;
            }
            return -1;
        }

        // Translate to origin
        public void TTO()
        {
            int i, j;

            for (i = 0; i < TERRAIN_SIZE; i++)
            {
                for (j = 0; j < TERRAIN_SIZE; j++)
                {
                    heightmap[i, j].position.X -= (TERRAIN_SIZE - 1) / 2;
                    heightmap[i, j].position.Z -= (TERRAIN_SIZE - 1) / 2;
                }
            }
        }

        // Terrain smoothing filter.
        public void smooth()
        {
            int i, j;

            for (i = 1; i < TERRAIN_SIZE - 1; i++)
            {
                for (j = 1; j < TERRAIN_SIZE - 1; j++)
                {
                    heightmap[i, j].position.Y = heightmap[i - 1, j - 1].position.Y
                        + heightmap[i, j - 1].position.Y
                        + heightmap[i + 1, j - 1].position.Y
                        + heightmap[i - 1, j].position.Y
                        + heightmap[i, j].position.Y
                        + heightmap[i + 1, j].position.Y
                        + heightmap[i - 1, j + 1].position.Y
                        + heightmap[i, j + 1].position.Y
                        + heightmap[i + 1, j + 1].position.Y;
                    heightmap[i, j].position.Y = heightmap[i, j].position.Y / 9.0f;
                }
            }
        }

        // Calculate normals.
        public void setNormals()
        {
            int i, j;
            Vector3 temp;

            for (i = 0; i < TERRAIN_SIZE - 1; i++)
            {
                for (j = 0; j < TERRAIN_SIZE - 1; j++)
                {
                    temp = calculateNormal(heightmap[i, j + 1].position,
                                           heightmap[i, j].position,
                                           heightmap[i + 1, j].position);

                    assignNormals(i, j, temp);
                    assignNormals(i, j + 1, temp);
                    assignNormals(i + 1, j, temp);

                    temp = calculateNormal(heightmap[i + 1, j].position,
                                           heightmap[i + 1, j + 1].position,
                                           heightmap[i, j + 1].position);
                    assignNormals(i + 1, j + 1, temp);
                }
            }
        }

        // Given two vectors, this function calculates the cross product.
        public Vector3 crossProd(Vector3 p1, Vector3 p2)
        {
            Vector3 n = new Vector3();

            n.X = ((p1.Y) * (p2.Z) - (p2.Y) * (p1.Z));
            n.Y = -((p1.X) * (p2.Z) - (p2.X) * (p1.Z));
            n.Z = ((p1.X) * (p2.Y) - (p2.X) * (p1.Y));

            return n;
        }

        // Calculate the difference between two vertices.
        public Vector3 calculateV(Vector3 p2, Vector3 p1)
        {
            Vector3 v = new Vector3();

            v.X = p2.X - p1.X;
            v.Y = p2.Y - p1.Y;
            v.X = p2.Z - p1.Z;

            return v;
        }

        // Given three vertices, this method calculates the normal in 
        // unit vector form and returns this.
        public Vector3 calculateNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 normal, unit_normal;
            Vector3 v21, v32;
            double magnitude;

            // Calculate v2 - v1
            v21 = calculateV(v2, v1);

            // Calculate v3 - v2
            v32 = calculateV(v3, v2);

            // Get the cross product of (v2 - v1) x (v3 - v2)
            normal = crossProd(v21, v32);

            // Get the magnitude
            magnitude = Math.Sqrt((normal.X) * (normal.X)
                              + (normal.Y) * (normal.Y)
                              + (normal.Z) * (normal.Z)
                            );

            // Divide the i,j and k components of the normal vector by the
            //	magnitude to get the unit vector form.
            unit_normal.X = normal.X / (float)magnitude;
            unit_normal.Y = normal.Y / (float)magnitude;
            unit_normal.Z = normal.Z / (float)magnitude;

            return unit_normal;
        }

        // Calculate vertex normals by averaging face normals.
        public void assignNormals(int i, int j, Vector3 temp)
        {
            if ((heightmap[i, j].normal.X == 0.0) &&
            (heightmap[i, j].normal.Y == 0.0) &&
            (heightmap[i, j].normal.Z == 0.0))
            {
                heightmap[i, j].normal.X = temp.X;
                heightmap[i, j].normal.Y = temp.Y;
                heightmap[i, j].normal.Z = temp.Z;
            }
            else
            {
                heightmap[i, j].normal.X = AVG_2f(heightmap[i, j].normal.X, temp.X);
                heightmap[i, j].normal.Y = AVG_2f(heightmap[i, j].normal.Y, temp.Y);
                heightmap[i, j].normal.Z = AVG_2f(heightmap[i, j].normal.Z, temp.Z);
            }
        }

        // Process the Vertex normals.
        public void vertexNormals()
        {
            int i, j;

            // Initialise a duplicate heightmap;
            HeightMap[,] heightmap2;
            heightmap2 = new HeightMap[TERRAIN_SIZE, TERRAIN_SIZE];
            for (i = 0; i < TERRAIN_SIZE; i++)
            {
                for (j = 0; j < TERRAIN_SIZE; j++)
                {
                    heightmap2[i, j] = new HeightMap()
                    {
                        position = new Vector3(i, 0.0f, j), // not used
                        normal = new Vector3
                        (
                            heightmap[i, j].normal.X,
                            heightmap[i, j].normal.Y,
                            heightmap[i, j].normal.Z
                        ),
                        colour = new Vector4(0.5f, 0.5f, 0.5f, 1.0f)  // not used
                    };
                }
            }

            // Average the normals
            for (i = 1; i < TERRAIN_SIZE - 1; i++)
            {
                for (j = 1; j < TERRAIN_SIZE - 1; j++)
                {
                    heightmap[i, j].normal.X
                        = (heightmap2[i - 1, j - 1].normal.X
                        + heightmap2[i, j - 1].normal.X
                        + heightmap2[i + 1, j - 1].normal.X
                        + heightmap2[i - 1, j].normal.X
                        + heightmap2[i, j].normal.X
                        + heightmap2[i + 1, j].normal.X
                        + heightmap2[i - 1, j + 1].normal.X
                        + heightmap2[i, j + 1].normal.X
                        + heightmap2[i + 1, j + 1].normal.X) / 9.0f;

                    heightmap[i, j].normal.Y
                        = (heightmap2[i - 1, j - 1].normal.Y
                        + heightmap2[i, j - 1].normal.Y
                        + heightmap2[i + 1, j - 1].normal.Y
                        + heightmap2[i - 1, j].normal.Y
                        + heightmap2[i, j].normal.Y
                        + heightmap2[i + 1, j].normal.Y
                        + heightmap2[i - 1, j + 1].normal.Y
                        + heightmap2[i, j + 1].normal.Y
                        + heightmap2[i + 1, j + 1].normal.Y) / 9.0f;

                    heightmap[i, j].normal.Z
                        = (heightmap2[i - 1, j - 1].normal.Z
                        + heightmap2[i, j - 1].normal.Z
                        + heightmap2[i + 1, j - 1].normal.Z
                        + heightmap2[i - 1, j].normal.Z
                        + heightmap2[i, j].normal.Z
                        + heightmap2[i + 1, j].normal.Z
                        + heightmap2[i - 1, j + 1].normal.Z
                        + heightmap2[i, j + 1].normal.Z
                        + heightmap2[i + 1, j + 1].normal.Z) / 9.0f;
                }
            }
        }


        // Particle deposition.
        public void ParticleDeposition()
        {
            int i, xp, zp, n;
            double max = MAXHEIGHT / 8.0;
            float yp = 0.0f;
            double range = max;

            // Random starting point
            xp = rnd.Next(0, TERRAIN_SIZE);
            zp = rnd.Next(0, TERRAIN_SIZE);

            for (i = 0; i < PD_INSTANCES; i++)
            {
                // Random height value
                yp = AVG_2f(sign() * (float)randomHeight(range), yp);
                heightmap[xp, zp].position.X = xp;
                heightmap[xp, zp].position.Y = yp;
                heightmap[xp, zp].position.Z = zp;

                // Figure out which way to go next
                n = rnd.Next(0, 4);
                switch (n)
                {
                    case 0: xp++; break;
                    case 1: xp--; break;
                    case 2: zp++; break;
                    case 3: zp--; break;
                }
                range /= 2;

                // If x or z is out of terrain bounds
                if ((xp >= TERRAIN_SIZE) || (zp >= TERRAIN_SIZE) || (xp < 0) || (zp < 0))
                {
                    xp = rnd.Next(0, TERRAIN_SIZE);
                    zp = rnd.Next(0, TERRAIN_SIZE);
                    range = max;
                }
            }
        }


        ///////////////////////
        // Auxiliary methods //
        ///////////////////////

        // Average of two floats.
        private float AVG_2f(float x, float y)
        {
            return ((x + y) / 2.0f);
        }

        // Average of three floats.
        private float AVG_3f(float x, float y, float z)
        {
            return ((x + y + z) / 3.0f);
        }

        // Average of four floats.
        private float AVG_4f(float w, float x, float y, float z)
        {
            return ((w + x + y + z) / 4.0f);
        }

        // Calculate side midpoints.
        private float RND_MID(float x, float y, float z, double range)
        {
            return sign() * (float)randomHeight(range) * TSCALE + AVG_3f(x, y, z);
        }

        // Calculate center midpoint.
        private float RND_CTR(float w, float x, float y, float z, double range)
        {
            return sign() * (float)randomHeight(range) + AVG_4f(w, x, y, z);
        }

        // Assign into heightmap.
        private void ASG_HMAP(ref Vector3 A)
        {
            heightmap[(int)A.X, (int)A.Z].position.X = A.X;
            heightmap[(int)A.X, (int)A.Z].position.Y = A.Y;
            heightmap[(int)A.X, (int)A.Z].position.Z = A.Z;
        }

        // Process midpoint.
        private void MID(ref Vector3 W, ref Vector3 A, ref Vector3 B,
            ref Vector3 CTR, double range)
        {
            W.X = AVG_2f(A.X, B.X);
            W.Z = AVG_2f(A.Z, B.Z);

            if (heightmap[(int)W.X, (int)W.Z].position.Y == 0.0)
            {
                W.Y = RND_MID(A.Y, B.Y, CTR.Y, range);
            }
            else
            {
                W.Y = heightmap[(int)W.X, (int)W.Z].position.Y;
            }
        }

        // Process center midpoint.
        private void MID_CTR(ref Vector3 A, ref Vector3 B, ref Vector3 C,
            ref Vector3 D, ref Vector3 CTR, double range)
        {
            CTR.Y = RND_CTR(A.Y, B.Y, C.Y, D.Y, range);
            CTR.X = AVG_4f(A.X, B.X, C.X, D.X);
            CTR.Z = AVG_4f(A.Z, B.Z, C.Z, D.Z);
        }

        // Normal distribution.
        private double NormalDist(double mean, double sigma)
        {
            double u1 = rnd.NextDouble();
            double u2 = rnd.NextDouble();
            double stdNormal = Math.Sqrt(-2.0 * Math.Log(u1))
                * Math.Sin(2.0 * Math.PI * u2);
            double rndNormal = mean + sigma * stdNormal;

            return rndNormal;
        }
    }
}
