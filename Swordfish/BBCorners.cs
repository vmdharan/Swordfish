using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swordfish
{
    class BBCorners
    {
        public struct Vertex2
        {
            public Vector4 Position;
        };

        private BoundingBox bb;
        private Vertex2[] bb8corners;


        public BBCorners(Vector3 minV, Vector3 maxV)
        {
            // Prepare the bounding box.
            //
            // 2,1 ****** 5,6
            //     ******
            //     ******
            // 3,0 ****** 4,7
            //
            bb = new BoundingBox(minV, maxV);
            var bbcorners = bb.GetCorners();

            // Generate the bounding box. 
            bb8corners = new Vertex2[20];
            bb8corners[0].Position = new Vector4(bbcorners[0], 1.0f);
            bb8corners[1].Position = new Vector4(bbcorners[1], 1.0f);
            bb8corners[2].Position = new Vector4(bbcorners[2], 1.0f);
            bb8corners[3].Position = new Vector4(bbcorners[3], 1.0f);

            bb8corners[4].Position = new Vector4(bbcorners[0], 1.0f);
            bb8corners[5].Position = new Vector4(bbcorners[4], 1.0f);
            bb8corners[6].Position = new Vector4(bbcorners[7], 1.0f);
            bb8corners[7].Position = new Vector4(bbcorners[3], 1.0f);

            bb8corners[8].Position = new Vector4(bbcorners[0], 1.0f);
            bb8corners[9].Position = new Vector4(bbcorners[1], 1.0f);
            bb8corners[10].Position = new Vector4(bbcorners[5], 1.0f);
            bb8corners[11].Position = new Vector4(bbcorners[4], 1.0f);

            bb8corners[12].Position = new Vector4(bbcorners[7], 1.0f);
            bb8corners[13].Position = new Vector4(bbcorners[6], 1.0f);
            bb8corners[14].Position = new Vector4(bbcorners[5], 1.0f);
            bb8corners[15].Position = new Vector4(bbcorners[1], 1.0f);

            bb8corners[16].Position = new Vector4(bbcorners[2], 1.0f);
            bb8corners[17].Position = new Vector4(bbcorners[6], 1.0f);
            bb8corners[18].Position = new Vector4(bbcorners[7], 1.0f);
            bb8corners[19].Position = new Vector4(bbcorners[3], 1.0f);
        }

        public Vertex2[] getBB8Corners()
        {
            return bb8corners;
        }

        public BoundingBox getBoundingBox()
        {
            return bb;
        }
    }
}
