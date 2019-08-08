using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Croc2ExplorerWV
{
    public class WADFile
    {
        public uint filesize;
        public string myPath;
        public List<WADSection> sections;

        public WADFile(string path)
        {
            myPath = path;
            byte[] data = File.ReadAllBytes(path);
            MemoryStream m = new MemoryStream(data);
            m.Seek(0, 0);
            filesize = Helper.ReadU32BE(m);
            Log.WriteLine("File Size = 0x" + filesize.ToString("X8"));
            sections = new List<WADSection>();
            while (m.Position < data.Length)
                sections.Add(new WADSection(m));
        }
        public void Resave()
        {
            MemoryStream m = new MemoryStream();
            byte[] buff;
            foreach (WADSection sec in sections)
            {
                buff = sec.ToRaw();
                m.Write(buff, 0, buff.Length);
            }
            buff = m.ToArray();
            m = new MemoryStream();
            Helper.WriteU32BE(m, (uint)buff.Length);
            m.Write(buff, 0, buff.Length);
            File.WriteAllBytes(myPath, m.ToArray());
            Log.WriteLine("Saved to " + myPath);
        }

        public class WADSection
        {
            public string type;
            public int size;
            public byte[] raw;
            public int leftOverStart;
            public List<WADSound> sounds = new List<WADSound>();
            public List<WADPalette> palettes = new List<WADPalette>();
            public List<WADTexture> textures = new List<WADTexture>();
            public WADSection(Stream s)
            {
                type = "";
                for (int i = 0; i < 4; i++)
                    type = (char)s.ReadByte() + type;
                size = (int)Helper.ReadU32BE(s);
                Log.WriteLine("Loaded section : " + type + " at 0x" + (s.Position - 8).ToString("X8"));
                raw = new byte[size];
                s.Read(raw, 0, size);
                MemoryStream m = new MemoryStream(raw);
                uint count;
                switch(type)
                {
                    case "INFO":
                    case "VERS":
                    case "WFPC":
                        Log.WriteLine(" Value = 0x" + Helper.ReadU32BE(m).ToString("X8"));
                        break;
                    case "SMPC":
                        count = Helper.ReadU32BE(m);
                        Log.WriteLine(" [" + count + " sounds to load]");
                        sounds = new List<WADSound>();
                        while (m.Position < raw.Length && count-- > 0)
                            sounds.Add(new WADSound(m));
                        break;
                    case "TEXT":
                        palettes = new List<WADPalette>();
                        textures = new List<WADTexture>();
                        count = Helper.ReadU32BE(m);
                        Log.WriteLine(" [" + count + " palettes to load]");
                        while (m.Position < raw.Length && count-- > 0)
                            palettes.Add(new WADPalette(m));
                        count = Helper.ReadU32BE(m);
                        Log.WriteLine(" [" + count + " textures to load]");
                        while (m.Position < raw.Length && count-- > 0)
                            textures.Add(new WADTexture(m));
                        leftOverStart = (int)m.Position;
                        break;
                }
            }

            public byte[] ToRaw()
            {
                MemoryStream m = new MemoryStream();
                m.WriteByte((byte)type[3]);
                m.WriteByte((byte)type[2]);
                m.WriteByte((byte)type[1]);
                m.WriteByte((byte)type[0]);
                byte[] buff;
                switch (type)
                {
                    case "TEXT":
                        MemoryStream t = new MemoryStream();
                        Helper.WriteU32BE(t, (uint)palettes.Count);
                        foreach(WADPalette pal in palettes)
                        {
                            buff = pal.ToRaw();
                            t.Write(buff, 0, buff.Length);
                        }
                        Helper.WriteU32BE(t, (uint)textures.Count);
                        foreach (WADTexture tex in textures)
                        {
                            buff = tex.ToRaw();
                            t.Write(buff, 0, buff.Length);
                        }
                        t.Write(raw, leftOverStart, raw.Length - leftOverStart);
                        buff = t.ToArray();
                        Helper.WriteU32BE(m, (uint)buff.Length);
                        m.Write(buff, 0, buff.Length);
                        break;
                    default:
                        Helper.WriteU32BE(m, (uint)raw.Length);
                        m.Write(raw, 0, raw.Length);
                        break;

                }
                return m.ToArray();
            }
        }

        public class WADSound
        {
            public uint u1;
            public uint size;
            public byte[] data;
            public WADSound(Stream s)
            {
                long pos = s.Position;
                u1 = Helper.ReadU32BE(s);
                size = Helper.ReadU32BE(s);
                s.Seek(-8, SeekOrigin.Current);
                data = new byte[size + 8];
                s.Read(data, 0, (int)size + 8);
                Log.WriteLine(" Loaded Sound @0x" + pos.ToString("X8") + " Size=0x" + size.ToString("X8"));
            }
        }

        public class WADPalette
        {
            public uint u1;
            public List<Color> colors;
            public WADPalette(Stream s)
            {
                colors = new List<Color>();
                u1 = Helper.ReadU32BE(s);
                uint count = Helper.ReadU32BE(s);
                for (int i = 0; i < count; i++)
                    colors.Add(Color.FromArgb(255, (byte)s.ReadByte(), (byte)s.ReadByte(), (byte)s.ReadByte()));
                Log.WriteLine(" Loaded Palette with " + count + " colors");
            }

            public byte[] ToRaw()
            {
                MemoryStream m = new MemoryStream();
                Helper.WriteU32BE(m, u1);
                Helper.WriteU32BE(m, (uint)colors.Count);
                foreach(Color c in colors)
                {
                    m.WriteByte(c.R);
                    m.WriteByte(c.G);
                    m.WriteByte(c.B);
                }
                return m.ToArray();
            }
        }

        public class WADTexture
        {
            public uint flags;
            public uint sizeX;
            public uint sizeY;
            public uint sizeData;
            public byte[] data;
            public WADTexture(Stream s)
            {
                long pos = s.Position;
                flags = Helper.ReadU32BE(s);                
                sizeX = Helper.ReadU32BE(s);
                sizeY = Helper.ReadU32BE(s);
                if ((flags & 0x80) == 0)
                {
                    int size = (int)(sizeX * sizeY);
                    if (flags != 0)
                        size *= 2;
                    data = new byte[size];
                    s.Read(data, 0, size);
                }
                else
                {
                    sizeData = Helper.ReadU32BE(s);
                    byte[] buff = new byte[sizeData];
                    s.Read(buff, 0, (int)sizeData);
                    data = Helper.DecompressRLE16(buff);
                }
                Log.WriteLine(" Loaded texture " + sizeX + "x" + sizeY + " (flag = 0x" + flags.ToString("X") + " size = 0x" + data.Length.ToString("X8") + " @0x" + pos.ToString("X8") + ")");
            }

            public void ImportData(Bitmap bmp)
            {
                if (flags == 0)
                    return;
                if ((flags & 0x80) != 0) //remove compression
                    flags &= 0x7F;
                MemoryStream m = new MemoryStream();
                for (int y = 0; y < sizeY; y++)
                    for (int x = 0; x < sizeX; x++)
                    {
                        Color c = bmp.GetPixel(x, y);
                        byte _r = (byte)(c.R >> 3);
                        byte _g = (byte)(c.G >> 3);
                        byte _b = (byte)(c.B >> 3);

                        ushort v = _b;
                        v <<= 5;
                        v |= _g;
                        v <<= 5;
                        v |= _r;
                        Helper.WriteU16BE(m, v);
                    }
                data = m.ToArray();
            }

            public byte[] ToRaw()
            {
                MemoryStream m = new MemoryStream();
                if ((flags & 0x80) != 0) //remove compression
                    flags &= 0x7F;
                Helper.WriteU32BE(m, flags);
                Helper.WriteU32BE(m, sizeX);
                Helper.WriteU32BE(m, sizeY);
                m.Write(data, 0, data.Length);
                return m.ToArray();
            }

            public Bitmap GetBitmap(WADPalette palette)
            {
                Bitmap result = new Bitmap((int)sizeX, (int)sizeY);
                MemoryStream m = new MemoryStream(data);
                if (flags == 0)
                {
                    for (int y = 0; y < sizeY; y++)
                        for (int x = 0; x < sizeX; x++)
                        {
                            byte v = (byte)m.ReadByte();
                            result.SetPixel(x, y, palette.colors[v % palette.colors.Count]);
                        }
                }
                else
                {
                    for (int y = 0; y < sizeY; y++)
                        for (int x = 0; x < sizeX; x++)
                        {
                            ushort v = Helper.ReadU16BE(m);
                            byte _r = (byte)(v & 0x1f);
                            v >>= 5;
                            byte _g = (byte)(v & 0x1f);
                            v >>= 5;
                            byte _b = (byte)(v & 0x1f);
                            _r <<= 3;
                            _g <<= 3;
                            _b <<= 3;
                            result.SetPixel(x, y, Color.FromArgb(255, _r, _g, _b));
                        }
                }
                return result;
            }
        }
    }
}
