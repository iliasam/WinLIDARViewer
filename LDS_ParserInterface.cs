using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarScanningTest1
{
    public interface LDS_ParserInterface
    {
        void ParseData(byte[] receivedData);

        void SetFrameReceivedCallback(Action<List<MeasuredPointT>> frameReceivedPtr);

    }
}
