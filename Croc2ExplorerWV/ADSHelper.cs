using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Croc2ExplorerWV
{
    public unsafe static class ADSHelper
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);
        public delegate uint ADS_Decode(byte* dataOut, byte* dataIn, uint zero);
        public static ADS_Decode ads_decode;
        public static bool init = false;

        public static bool Init()
        {
            if (!File.Exists("ads.dll"))
                return false;
            IntPtr base_address = LoadLibrary("ads.dll");
            if (base_address == IntPtr.Zero)
                return false;
            base_address += 0x7740;
            ads_decode = (ADS_Decode)Marshal.GetDelegateForFunctionPointer(base_address, typeof(ADS_Decode));
            init = true;
            return true;
        }

        public static byte[] Convert(byte[] data)
        {
            int len = data.Length;
            len /= 16;
            len *= 56;
            byte[] result = new byte[len];
            fixed (byte* pDataIn = &data[28], pDataOut = result)
            {
                ads_decode(pDataOut, pDataIn, 0);
            }
            MemoryStream m = new MemoryStream();
            m.Write(data, 0, 28);
            return MakeWav(m.ToArray(), result);
        }

        private static byte[] MakeWav(byte[] header, byte[] data)
        {
            MemoryStream m = new MemoryStream(header);
            m.Seek(8, 0);
            uint sampleRate = Helper.ReadU32BE(m);
            uint byteRate = sampleRate * 2;
            MemoryStream result = new MemoryStream();
            Helper.WriteU32BE(result, 0x46464952);
            Helper.WriteU32BE(result, (uint)(data.Length + 0x24));
            Helper.WriteU32BE(result, 0x45564157);
            Helper.WriteU32BE(result, 0x20746D66);
            Helper.WriteU32BE(result, 0x10);
            Helper.WriteU16BE(result, 0x1);
            Helper.WriteU16BE(result, 0x1);
            Helper.WriteU32BE(result, sampleRate);
            Helper.WriteU32BE(result, byteRate);
            Helper.WriteU16BE(result, 0x2);
            Helper.WriteU16BE(result, 0x10);
            Helper.WriteU32BE(result, 0x61746164);
            Helper.WriteU32BE(result, (uint)(data.Length));
            result.Write(data, 0, data.Length);
            return result.ToArray();
        }

        public static void Play(byte[] wav)
        {
            new SoundPlayer(new MemoryStream(wav)).Play();
        }
    }
}
