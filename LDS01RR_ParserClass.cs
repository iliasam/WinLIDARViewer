using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//See https://github.com/Roborock-OpenSource/Cullinan


namespace LidarScanningTest1
{
    class LDS01RR_ParserClass : LDS_ParserInterface
    {
        private Action<List<MeasuredPointT>> FrameReceived;

        private enum SyncState
        {
            NoSync = 0,
            ReceivedByte1,
            ReceivedByte2,
            ReceivingData,
        }

        private const int PACKET_SIZE_BYTES = 22;
        private const int SPEED_LOW_BYTE_IDX = 2;
        private const int PAYLOAD_START_BYTE_IDX = 4;

        private const int PACKET_SAMPLES = 4;//number of samples in one packet
        private const int SAMPLE_SIZE_BYTES = 4;
        private const int PAYLOAD_SIZE = SAMPLE_SIZE_BYTES * PACKET_SAMPLES;


        SyncState currSync = SyncState.NoSync;
        int PacketPosCnt = 0;
        List<byte> CurrPacket = new List<byte>();
        byte ExpectedPacketSize = 0;
        UInt16 CurrentAngleDeg = 0;

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
                if (rxByte == 0xFA)
                {
                    currSync = SyncState.ReceivingData;
                    PacketPosCnt = 1;
                    CurrPacket = new List<byte>();
                    CurrPacket.Add(rxByte);
                    ExpectedPacketSize = PACKET_SIZE_BYTES;
                }
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
            byte packetSeq = CurrPacket[1];
            int speed = ((UInt16)CurrPacket[SPEED_LOW_BYTE_IDX] + (UInt16)CurrPacket[SPEED_LOW_BYTE_IDX + 1] * 256);
            float speedF = (float)speed / 3840.0f;

            int AngleCode = packetSeq - 160;
            if (AngleCode < 0)
                return;

            if (AngleCode == 0)
            {
                FrameReceived?.Invoke(pointsList);
                pointsList = new List<MeasuredPointT>();
                CurrentAngleDeg = 0;
            }

            for (int i = 0; i < PACKET_SAMPLES; i++)
            {
                int start = PAYLOAD_START_BYTE_IDX + i * SAMPLE_SIZE_BYTES;
                MeasuredPointT point = ParseMeasuredData(
                    CurrPacket[start], CurrPacket[start + 1],
                    CurrPacket[start + 2], CurrPacket[start + 3], CurrentAngleDeg);
                pointsList.Add(point);
                CurrentAngleDeg++;
            }

            //System.Diagnostics.Debug.WriteLine($"Seq: {PacketSeq}");
        }

        private MeasuredPointT ParseMeasuredData(byte byte1, byte byte2, byte byte3, byte byte4, float angleDeg)
        {
            MeasuredPointT res;

            UInt16 distance = (UInt16)((UInt16)byte1 + (UInt16)byte2 * 256);
            UInt16 Intensity = (UInt16)((UInt16)byte3 + (UInt16)byte4 * 256);

            if ((byte2 & 128) != 0)
                res.DistanceMM = -1;
            else if ((byte2 & 64) != 0)
                res.DistanceMM = -2;
            else
                res.DistanceMM = distance;

            res.Intensity = Intensity;
            res.AngleDeg = angleDeg;

            return res;
        }

        public void SetFrameReceivedCallback(Action<List<MeasuredPointT>> frameReceivedPtr)
        {
            FrameReceived = frameReceivedPtr;
        }
    } //end of class
}
