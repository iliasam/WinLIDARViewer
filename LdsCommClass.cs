using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Thanks for https://github.com/Vidicon/camsense-X1

namespace LidarScanningTest1
{
    class LdsCommClass
    {
        public Action<List<MeasuredPointT>> FrameReceived;

        private enum SyncState
        {
            NoSync = 0,
            ReceivedByte1,
            ReceivedByte2,
            ReceivingData,
        }

        public struct MeasuredPointT
        {
            public int DistanceMM;
            public UInt16 Intensity;
            public float AngleDeg;
        }

        private const int PACKET_SIZE_BYTES = 36;
        private const int SPEED_LOW_BYTE_IDX = 4;
        private const int START_ANG_LOW_BYTE_IDX = 6;
        private const int PAYLOAD_START_BYTE_IDX = 8;

        private const int PACKET_SAMPLES = 8;//number of samples in one packet
        private const int SAMPLE_SIZE_BYTES = 3;
        private const int PAYLOAD_SIZE = SAMPLE_SIZE_BYTES * PACKET_SAMPLES;

        private float prevAngleDeg = 0.0f;

        
        SyncState currSync = SyncState.NoSync;
        int PacketPosCnt = 0;
        List<byte> CurrPacket = new List<byte>();
        byte ExpectedPacketSize = 0;

        List<MeasuredPointT> pointsList = new List<MeasuredPointT>();

        public void ParseData(byte[] receivedData)
        {
            foreach (var item in receivedData)
            {
                ParseReceivedByte(item);
            }
        }


        private void ParseReceivedByte(byte rxByte)
        {
            if (currSync == SyncState.NoSync)
            {
                if (rxByte == 0x55)
                {
                    currSync = SyncState.ReceivedByte1;
                    PacketPosCnt = 1;
                    CurrPacket = new List<byte>();
                    CurrPacket.Add(rxByte);
                }
            }
            else if (currSync == SyncState.ReceivedByte1)
            {
                if (rxByte == 0xaa)
                {
                    currSync = SyncState.ReceivedByte2;
                    PacketPosCnt = 2;
                    CurrPacket.Add(rxByte);
                }
                else
                    currSync = SyncState.NoSync;
            }
            else if (currSync == SyncState.ReceivedByte2)
            {
                //ExpectedPacketSize = rxByte;
                ExpectedPacketSize = PACKET_SIZE_BYTES;
                currSync = SyncState.ReceivingData;
                PacketPosCnt = 3;
                CurrPacket.Add(rxByte);
            }
            else if (currSync == SyncState.ReceivingData)
            {
                CurrPacket.Add(rxByte);
                PacketPosCnt++;
                if (PacketPosCnt >= ExpectedPacketSize)
                {
                    currSync = SyncState.NoSync;

                    if (PacketPosCnt == PACKET_SIZE_BYTES)
                        ParseMeasurementDataPacket();
                }
            }
        }//end of ParseReceivedByte()


        private void ParseMeasurementDataPacket()
        {
            byte PacketSeq = CurrPacket[4];
            int speed = ((UInt16)CurrPacket[SPEED_LOW_BYTE_IDX] + (UInt16)CurrPacket[SPEED_LOW_BYTE_IDX + 1] * 256);
            float speedF = (float)speed / 3840.0f;

            UInt16 startAngleInt = (UInt16)(CurrPacket[START_ANG_LOW_BYTE_IDX] | CurrPacket[START_ANG_LOW_BYTE_IDX + 1] << 8);
            float startAngleDeg = (float)startAngleInt / 64.0f - 640.0f;

            UInt16 stopAngleInt = (UInt16)(CurrPacket[START_ANG_LOW_BYTE_IDX+ PAYLOAD_SIZE + 2] | CurrPacket[START_ANG_LOW_BYTE_IDX + 1+ PAYLOAD_SIZE + 2] << 8);
            float stopAngleDeg = (float)stopAngleInt / 64.0f - 640.0f;

            /// Angle between samples
            float diffDeg = 0.0f;
            if (stopAngleDeg > startAngleDeg)
                diffDeg = (stopAngleDeg - startAngleDeg) / (float)PACKET_SAMPLES;
            else
                diffDeg = (stopAngleDeg - (startAngleDeg - 360.0f)) / (float)PACKET_SAMPLES;

            for (int i = 0; i < PACKET_SAMPLES; i++)
            {
                float angleDeg = startAngleDeg + diffDeg * i;

                int startByte = PAYLOAD_START_BYTE_IDX + i * SAMPLE_SIZE_BYTES;
                MeasuredPointT point = ParseMeasuredData(
                    CurrPacket[startByte], CurrPacket[startByte + 1],
                    CurrPacket[startByte + 2], angleDeg);
                pointsList.Add(point);

                if (angleDeg < prevAngleDeg) //detect zero cross
                {
                    if (pointsList.Count < 400)
                    {
                        FrameReceived?.Invoke(pointsList);
                        //System.Diagnostics.Debug.WriteLine($"FRAME");
                    }
                    else
                        System.Diagnostics.Debug.WriteLine($"ERR");
                    pointsList = new List<MeasuredPointT>();
                }
                prevAngleDeg = angleDeg;
            }
        }

        private MeasuredPointT ParseMeasuredData(byte byte1, byte byte2, byte byte3, float angleDeg)
        {
            MeasuredPointT res;

            UInt16 distance = (UInt16)((UInt16)byte1 + (UInt16)byte2 * 256);
            UInt16 Intensity = (UInt16)byte3;

            res.DistanceMM = distance;
            res.Intensity = Intensity;
            res.AngleDeg = angleDeg;

            return res;
        }
    } //end of class
}
