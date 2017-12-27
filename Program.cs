using System;
using System.IO;
using System.Runtime.InteropServices;

namespace D2SHP
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FrameHeader
    {
        public ushort flags;
        public byte slices;
        public ushort width;
        public byte height;
        public ushort filesize;
        public ushort datasize;
    }

    static class Helpers
    {
        public static int SizeOf<T>() where T : struct
        {
            return Marshal.SizeOf(typeof(T));
        }
    }

    static class Extensions
    {
        public static byte[] ReadBytes(this Stream s, int nbytes)
        {
            var bytes = new byte[nbytes];
            s.Read(bytes, 0, nbytes);
            return bytes;
        }

        public static T TypeCastByteBuffer<T>(this byte[] buffer) where T : struct
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var p = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return p;
        }

        public static byte[] TypeCastStruct<T>(this T obj) where T : struct
        {
            var buf = new byte[Helpers.SizeOf<T>()];
            var p = Marshal.AllocHGlobal(buf.Length);
            Marshal.StructureToPtr(obj, p, true);
            Marshal.Copy(p, buf, 0, buf.Length);
            Marshal.FreeHGlobal(p);
            return buf;
        }

        public static T ReadStruct<T>(this Stream s) where T : struct
        {
            return s.ReadBytes(Helpers.SizeOf<T>()).TypeCastByteBuffer<T>();
        }

        
        public static string F(this string fmt, params object[] args)
        {
            return string.Format(fmt, args);
        }
    }

    class Program
    {
        const int FRAME_PALETTE = 1;
        const int FRAME_NOCOMPRESSION = 2;
        const int FRAME_VARLENTABLE = 4;

        static byte[] ReadPalette(Stream s)
        {
            var pal = s.ReadBytes(768);
            for(var i = 0; i < pal.Length; i++)
                pal[i] <<= 2;
            return pal;
        }

        static byte[] ReadFrame(Stream stream, out FrameHeader hdr)
        {
            hdr = stream.ReadStruct<FrameHeader>();
            var dataleft = hdr.filesize - Helpers.SizeOf<FrameHeader>();
            byte[] table = null;

            if( (hdr.flags & FRAME_PALETTE) != 0 )
            {
                var tablen = ((hdr.flags & FRAME_VARLENTABLE) != 0) ? stream.ReadByte(): 16;
                table = stream.ReadBytes(tablen);
                dataleft -= tablen;
            }
            // else
            // {
            //     table = new byte[256];
            //     for (var i = 0; i < 256; i++)
			// 			table[i] = (byte)i;
            //     table[1] = 0x7f;
            //     table[2] = 0x7e;
            //     table[3] = 0x7d;
            //     table[4] = 0x7c;
            // }

            var dst = new byte[hdr.width * hdr.height];
            var src = stream.ReadBytes(dataleft);
            if( (hdr.flags & FRAME_NOCOMPRESSION) == 0 )
            {
                var tmp = new byte[hdr.datasize];
                Format80.Decode(src, 0, tmp, 0, src.Length, false);
                src = tmp;
            }

            Format2.Decode(src, dst, 0);

            if(table != null)
                for(var i = 0; i < dst.Length; i++)
                    dst[i] = table[dst[i]];
            return dst;
        }

        static void ReadShp(Stream stream, out FrameHeader[] hdrs, out byte[][] frames)
        {
            var reader = new BinaryReader(stream);
            var nimages = reader.ReadUInt16();
            var offsets = new uint[nimages + 1];
            // read image data offsets
            for(var i = 0; i < offsets.Length; i++)
                offsets[i] = reader.ReadUInt32();
            hdrs = new FrameHeader[nimages];
            frames = new byte[nimages][];
            for(var i = 0; i < nimages; i++)
            {
                stream.Seek(2 + offsets[i], SeekOrigin.Begin);
                frames[i] = ReadFrame(stream, out hdrs[i]);
            }
        }

        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Usage: d2shp <mouse.shp> <temperat.pal>");
                return;
            }

            var shppath = args[0];
            var palpath = args[1];
            var name = Path.GetFileNameWithoutExtension(shppath);

            var palette = ReadPalette(File.Open(palpath, FileMode.Open));
            var stream = File.Open(shppath, FileMode.Open);
            FrameHeader[] hdrs;
            byte[][] frames;
            ReadShp(stream, out hdrs, out frames);

            for(var i = 0; i < hdrs.Length; i++)
            {
                var filename = "{0} {1:D4}.pcx".F(name, i);
                Console.WriteLine(filename);
                var f = File.Open(filename, FileMode.Create);
                PcxFile.Write(f, frames[i], hdrs[i].width, hdrs[i].height, palette);
                f.Close();
            }
        }
    }
}
