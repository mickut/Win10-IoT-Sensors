using System;

namespace I2CSensors
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Converts two bytes in big-endian to Int16
        /// </summary>
        /// <param name="data">byte array</param>
        /// <param name="index">index of MSB</param>
        /// <returns>int</returns>
        public static int BeToInt16(this byte[] data, int index=0)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToInt16(new[] { data[index+1], data[index] }, 0);
            else
                return BitConverter.ToInt16(data, index);
        }
        /// <summary>
        /// Converts two bytes in big-endianto UInt16
        /// </summary>
        /// <param name="data">byte array</param>
        /// <param name="index">index of MSB</param>
        /// <returns>int</returns>
        public static uint BeToUInt16(this byte[] data, int index=0)
        {
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToUInt16(new[] { data[index+1], data[index] }, 0);
            else
                return BitConverter.ToUInt16(data, index);
        }
    }
}