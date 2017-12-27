using System;
using System.IO;
using System.Runtime.InteropServices;

namespace D2SHP
{
    public class PcxFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe private struct PcxHeader
        {
            public byte manufacturer;
            public byte version;
            public byte encoding;
            public byte bits_per_pixel;
            public ushort xmin;
            public ushort ymin;
            public ushort xmax;
            public ushort ymax;
            public ushort hres;
            public ushort vres;
            public fixed byte pal16[48];
            public byte reserved;
            public byte num_bits_planes;
            public ushort bytes_per_line;
            public ushort pal_type;
            public ushort h_scr_size;
            public ushort v_scr_size;
            public fixed byte padding[54];
        }

        const int MARK = 0xC0;

        public static void Write(Stream stream, byte[] src, int w, int h, byte[] palette)
        {
            var hdr = new PcxHeader{
                manufacturer = 10,
                version = 5,
                encoding = 1,
                xmin = 0,
                ymin = 0,
                xmax = (ushort)(w - 1),
                ymax = (ushort)(h - 1),
                bits_per_pixel = 8,
                num_bits_planes = 1,
                bytes_per_line = (ushort)w,
                h_scr_size = 1,
                v_scr_size = 1,
            };
            var hdrbuf = hdr.TypeCastStruct();
            stream.Write(hdrbuf, 0, hdrbuf.Length);

            // World's worst RLE compression, but I can't be bothered.
            for(var i = 0; i < src.Length; i++)
            {
                var ch = src[i];
                if(ch >= MARK)
                    stream.WriteByte((byte)(MARK | 1));
                stream.WriteByte(ch);
            }

            if(palette != null)
            {
                stream.WriteByte(0x0C);
                stream.Write(palette, 0, 768);
            }
        }
    }
}