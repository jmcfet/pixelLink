using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixeLINK;

namespace DynamicOanels
{
    public class TransferBits
    {
        public int hCamera;
        public PixeLINK.PixelFormat dataFormat;
        public FrameDescriptor frameDesc;
        public byte[] bits;
        public byte[] FormattedBuf;
    }
}
