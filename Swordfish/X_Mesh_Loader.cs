using System;
using System.Collections.Generic;
using System.IO;

namespace Swordfish
{
    class X_Mesh_Loader
    {
        // List of tokens from the source file.
        private List<string> tokens;

        // List of vertices, indices, normals and texture coordinates 
        // from parsing the tokens.
        public List<float> vertices;
        public List<int> indices;
        public List<float> normals;
        public List<float> texUV;

        // Constructor
        public X_Mesh_Loader()
        {
            tokens = new List<string>();
            vertices = new List<float>();
            indices = new List<int>();
            normals = new List<float>();
            texUV = new List<float>();
        }

        // Read the source file and parse into tokens.
        public void readSourceFile(string srcFile)
        {
            int i = 0;
            int k = 0;
            char[] delimiters =
            {
                ' ', ',', ';', '\t'
            };

            // Read all the lines into an array to be parsed.
            string[] srcData = File.ReadAllLines(srcFile, System.Text.Encoding.UTF8);

            // Process one line at a time.
            while (true)
            {
                if (i >= srcData.Length)
                {
                    break;
                }

                string[] srcTokens = srcData[i].Split(delimiters);

                for (int j = 0; j < srcTokens.Length; j++)
                {
                    // Skip over empty strings.
                    if (srcTokens[j].Length == 0)
                    {
                        continue;
                    }
                    tokens.Add(srcTokens[j]);
                    k++;
                }

                i++;
            }
        }

        // Read the token list and parse the mesh vertices.
        public void readMeshTokens()
        {
            int j = 0;
            string mesh = "Mesh";
            string lBrace = "{";

            for (int i = 0; i < tokens.Count; i++)
            {
                // Search for the tokens "Mesh" and "{"
                if ((tokens[i] == mesh) && (tokens[i + 1] == lBrace))
                {
                    j = i + 2;

                    // Skip over the comment strings.
                    // j(0) = "//"
                    // j(1) = "Cube"
                    // j(2) = "mesh"
                    j += 3;

                    // Parse the next string as an integer.
                    // This value represents the number of vertices to follow.
                    int iNum = Convert.ToInt32(tokens[j]);
                    j++;

                    // Read the mesh vertices.
                    for (int k = 0; k < iNum; k++)
                    {
                        vertices.Add(Convert.ToSingle(tokens[j]));
                        vertices.Add(Convert.ToSingle(tokens[j + 1]));
                        vertices.Add(Convert.ToSingle(tokens[j + 2]));
                        j += 3;
                    }
                }
            }

            // Proceed with reading the index tokens.
            readIndexTokens(j);
        }

        // Read the mesh indices from the token list.
        public void readIndexTokens(int iTokenPosition)
        {
            int j = iTokenPosition;
            int iNum = Convert.ToInt32(tokens[j]);
            j++;

            for (int k = 0; k < iNum; k++)
            {
                // Skip over the first entry as the importer only 
                // supports triangles, not quads. Expect that it 
                // has value 3.
                j++;

                // Now add the indices to the list.
                indices.Add(Convert.ToInt32(tokens[j]));
                indices.Add(Convert.ToInt32(tokens[j + 1]));
                indices.Add(Convert.ToInt32(tokens[j + 2]));
                j += 3;
            }
        }

        // Read the token list and parse the mesh normals.
        public void readMeshNormalTokens()
        {
            string mesh = "MeshNormals";
            string lBrace = "{";

            for (int i = 0; i < tokens.Count; i++)
            {
                // Search for the tokens "MeshNormals" and "{"
                if ((tokens[i] == mesh) && (tokens[i + 1] == lBrace))
                {
                    int j = i + 2;

                    // Skip over the comment strings.
                    // j(0) = "//"
                    // j(1) = "Cube"
                    // j(2) = "normals"
                    j += 3;

                    // Parse the next string as an integer.
                    // This value represents the number of normals to follow.
                    int iNum = Convert.ToInt32(tokens[j]);
                    j++;

                    // Read the mesh normals.
                    for (int k = 0; k < iNum; k++)
                    {
                        normals.Add(Convert.ToSingle(tokens[j]));
                        normals.Add(Convert.ToSingle(tokens[j + 1]));
                        normals.Add(Convert.ToSingle(tokens[j + 2]));
                        j += 3;
                    }
                }
            }
        }

        // Read the token list and parse the mesh texture UV coordinates.
        public void readMeshTextureUVTokens()
        {
            string mesh = "MeshTextureCoords";
            string lBrace = "{";

            for (int i = 0; i < tokens.Count; i++)
            {
                // Search for the tokens "MeshTextureCoords" and "{"
                if ((tokens[i] == mesh) && (tokens[i + 1] == lBrace))
                {
                    int j = i + 2;

                    // Skip over the comment strings.
                    // j(0) = "//"
                    // j(1) = "Cube"
                    // j(2) = "UV"
                    // j(3) = "coordinates"
                    j += 4;

                    // Parse the next string as an integer.
                    // This value represents the number of coordinate sets 
                    // to follow.
                    int iNum = Convert.ToInt32(tokens[j]);
                    j++;

                    // Read the texture coordinates.
                    for (int k = 0; k < iNum; k++)
                    {
                        texUV.Add(Convert.ToSingle(tokens[j]));
                        texUV.Add(Convert.ToSingle(tokens[j + 1]));
                        j += 2;
                    }
                }
            }
        }

        // Print out the token list.
        public void printTokens()
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                Console.Write(tokens[i]);
                Console.Write(' ');
            }
        }

        // Print out the list of mesh vertices.
        public void printVertices()
        {
            Console.WriteLine("Vertex count: {0}", vertices.Count / 3);
            for (int i = 0; i < vertices.Count; i += 3)
            {
                Console.WriteLine("{0} {1} {2}", vertices[i], vertices[i + 1], vertices[i + 2]);
            }
        }

        // Print out the list of mesh indices.
        public void printIndices()
        {
            Console.WriteLine("Index count: {0}", indices.Count / 3);
            for (int i = 0; i < indices.Count; i += 3)
            {
                Console.WriteLine("{0} {1} {2}", indices[i], indices[i + 1], indices[i + 2]);
            }
        }

        // Print out the list of texture UV coordinates for the mesh.
        public void printTexUV()
        {
            Console.WriteLine("TexUV count: {0}", texUV.Count / 2);
            for (int i = 0; i < texUV.Count; i += 2)
            {
                Console.WriteLine("{0} {1}", texUV[i], texUV[i + 1]);
            }
        }

        // Print out the list of mesh normals.
        public void printNormals()
        {
            Console.WriteLine("Normals count: {0}", normals.Count / 3);
            for (int i = 0; i < normals.Count; i += 3)
            {
                Console.WriteLine("{0} {1} {2}", normals[i], normals[i + 1], normals[i + 2]);
            }
        }
    }
}
