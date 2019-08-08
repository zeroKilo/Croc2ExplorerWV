using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Croc2ExplorerWV
{
    public static class Helper
    {
        public static uint ReadU32LE(Stream s)
        {
            ulong result = 0;
            result = (byte)s.ReadByte();
            result = (result << 8) | (byte)s.ReadByte();
            result = (result << 8) | (byte)s.ReadByte();
            result = (result << 8) | (byte)s.ReadByte();
            return (uint)result;
        }

        public static ushort ReadU16LE(Stream s)
        {
            ulong result = 0;
            result = (byte)s.ReadByte();
            result = (result << 8) | (byte)s.ReadByte();
            return (ushort)result;
        }
        public static uint ReadU32BE(Stream s)
        {
            ulong result = 0;
            result = (byte)s.ReadByte();
            result |= (ulong)((byte)s.ReadByte() << 8);
            result |= (ulong)((byte)s.ReadByte() << 16);
            result |= (ulong)((byte)s.ReadByte() << 24);
            return (uint)result;
        }

        public static ushort ReadU16BE(Stream s)
        {
            ulong result = 0;
            result = (byte)s.ReadByte();
            result |= (ulong)((byte)s.ReadByte() << 8);
            return (ushort)result;
        }

        public static void WriteU16BE(Stream s, ushort v)
        {
            s.WriteByte((byte)(v & 0xFF));
            s.WriteByte((byte)(v >> 8));
        }

        public static void WriteU32BE(Stream s, uint v)
        {
            s.WriteByte((byte)(v & 0xFF));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 24));
        }

        public static byte[] DecompressRLE16(byte[] data)
        {
            MemoryStream m = new MemoryStream(data);
            MemoryStream r = new MemoryStream();
            while (m.Position < data.Length)
            {
                short s = (short)ReadU16BE(m);
                if (s >= 0)
                    for (int i = 0; i < s; i++)
                        WriteU16BE(r, ReadU16BE(m));
                else
                {
                    s *= -1;
                    short v = (short)ReadU16BE(m);
                    for (int i = 0; i < s; i++)
                        WriteU16BE(r, (ushort)v);
                }
            }
            return r.ToArray();
        }
    }
}
