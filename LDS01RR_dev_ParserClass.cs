using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LidarScanningTest1
{
    class LDS01RR_dev_ParserClass : LDS_ParserInterface
    {
        private Action<List<MeasuredPointT>> FrameReceived;

        private enum SyncState
        {
            NoSync = 0,
            ReceivedByte1,
            ReceivedByte2,
            ReceivingData,
        }

        private const int PACKET_SIZE_BYTES = 34;
        private const int SPEED_LOW_BYTE_IDX = 14;
        private const int START_ANG_LOW_BYTE_IDX = 6;
        private const int PAYLOAD_START_BYTE_IDX = 16;

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
                if (rxByte == 0xE7)
                {
                    currSync = SyncState.ReceivedByte1;
                    PacketPosCnt = 1;
                    CurrPacket = new List<byte>();
                    CurrPacket.Add(rxByte);
                }
            }
            else if (currSync == SyncState.ReceivedByte1)
            {
                if (rxByte == 0x7E)
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
                ExpectedPacketSize = rxByte;
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
            byte packetSeq = CurrPacket[4];
            int speed = ((UInt16)CurrPacket[SPEED_LOW_BYTE_IDX] + (UInt16)CurrPacket[SPEED_LOW_BYTE_IDX + 1] * 256);
            float speedF = (float)speed / 20.0f;

            //UInt16 startAngleInt = (UInt16)(CurrPacket[START_ANG_LOW_BYTE_IDX] | CurrPacket[START_ANG_LOW_BYTE_IDX + 1] << 8);
            //float startAngleDeg = (float)startAngleInt / 64.0f - 640.0f;

            //UInt16 stopAngleInt = (UInt16)(CurrPacket[START_ANG_LOW_BYTE_IDX+ PAYLOAD_SIZE + 2] | CurrPacket[START_ANG_LOW_BYTE_IDX + 1+ PAYLOAD_SIZE + 2] << 8);
            //float stopAngleDeg = (float)stopAngleInt / 64.0f - 640.0f;

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
