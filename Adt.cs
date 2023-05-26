using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CASCLib;
using System.IO;

namespace CascStuff
{
    class Adt
    {
        public CASCHandler cs;
        public bool adt_claims_tile = false;
        public List<Toolbox.IngameObject> objects = new List<Toolbox.IngameObject>();
        public int x = 0;
        public int y = 0;
        public ulong[,] holes_high_res= new ulong[16,16];
        public ushort[,] holes_low_res=new ushort[16,16];
        public bool flaghole=false;
        public Dictionary<uint, string> listfile;
        public Dictionary<string, uint> filelist;
        public Wdt main_wdt;
        public int[,] impasses = new int[16, 16];
        public int[,] unknown = new int[16, 16];
        public Int32 adtflags = 0;
        public List<Toolbox.Area> areas = new List<Toolbox.Area>();
        public List<Toolbox.CsvArea> CsvAreas = new List<Toolbox.CsvArea>();
        public Toolbox.plane plane_min = new Toolbox.plane();
        public Toolbox.plane plane_max = new Toolbox.plane();
        public float[] mcnkpos = new float[3];
        List<string> filenameswmo = new List<string>();
        List<string> filenamesm2 = new List<string>();
        List<uint> mmid = new List<uint>();
        List<uint> mwid = new List<uint>();
        public float[,][] mcvt = new float[16,16][];
        public MCNKflags[,] mcnkflags = new MCNKflags[16, 16];

        public struct MCNKflags
        {
            public bool has_mcsh; 
            public bool impass;
            public bool lq_river;
            public bool lq_ocean;
            public bool lq_magma;
            public bool lq_slime;
            public bool has_mccv;
            public bool unknown_0x80;
            public bool do_not_fix_alpha_map;  
            public bool high_res_holes;  
        }

        
        public MCNKflags ReadMCNKflags(BinaryReader stream)
        {
            MCNKflags mcnkFlags = new MCNKflags();
            // <Flags> 4 bytes
            byte[] arrayOfBytes = new byte[4];
            stream.Read(arrayOfBytes, 0, 4);
            BitArray flags = new BitArray(arrayOfBytes);
            mcnkFlags.has_mcsh = flags[0]; 
            mcnkFlags.impass = flags[1];
            mcnkFlags.lq_river = flags[2];
            mcnkFlags.lq_ocean = flags[3];
            mcnkFlags.lq_magma = flags[4];
            mcnkFlags.lq_slime = flags[5];
            mcnkFlags.has_mccv = flags[6];
            mcnkFlags.unknown_0x80 = flags[7];
            mcnkFlags.do_not_fix_alpha_map = flags[15]; 
            mcnkFlags.high_res_holes = flags[16];  
            return mcnkFlags;
        }
        public Adt(CASCHandler cascH, int adt_x, int adt_y, Dictionary<uint, string> lf, List<Toolbox.CsvArea> listareas,Dictionary<string, uint> fl, Wdt wdt)
        {
            cs = cascH;
            x = adt_x;
            y = adt_y;
            listfile = lf;
            filelist = fl;
            CsvAreas= listareas;
            main_wdt = wdt;
        }

        public void Parse(int adt_name, Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj)
        {
            if ((adt_name > 0) && cs.FileExists(adt_name))
            {
                //Console.WriteLine("parsing " + adt_name + " : " +x + " / " + y);
                try {
                    using (Stream stream = cs.OpenFile(adt_name))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            while (reader.BaseStream.Position != reader.BaseStream.Length)
                            {
                                var magic = reader.ReadUInt32();
                                var size = reader.ReadUInt32();
                                var pos = reader.BaseStream.Position;

                                if (magic == Toolbox.mk("MFBO"))
                                {
                                    plane_max.a = new short[3];
                                    plane_max.a[0] = reader.ReadInt16();
                                    plane_max.a[1] = reader.ReadInt16();
                                    plane_max.a[2] = reader.ReadInt16();
                                    plane_max.b = new short[3];
                                    plane_max.b[0] = reader.ReadInt16();
                                    plane_max.b[1] = reader.ReadInt16();
                                    plane_max.b[2] = reader.ReadInt16();
                                    plane_max.c = new short[3];
                                    plane_max.c[0] = reader.ReadInt16();
                                    plane_max.c[1] = reader.ReadInt16();
                                    plane_max.c[2] = reader.ReadInt16();

                                    plane_min.a = new short[3];
                                    plane_min.a[0] = reader.ReadInt16();
                                    plane_min.a[1] = reader.ReadInt16();
                                    plane_min.a[2] = reader.ReadInt16();
                                    plane_min.b = new short[3];
                                    plane_min.b[0] = reader.ReadInt16();
                                    plane_min.b[1] = reader.ReadInt16();
                                    plane_min.b[2] = reader.ReadInt16();
                                    plane_min.c = new short[3];
                                    plane_min.c[0] = reader.ReadInt16();
                                    plane_min.c[1] = reader.ReadInt16();
                                    plane_min.c[2] = reader.ReadInt16();
                                }
                                if (magic == Toolbox.mk("MCNK"))
                                {
                                    MCNKflags f = new MCNKflags();
                                    f = ReadMCNKflags(reader);
                                    reader.BaseStream.Position -=4;
                                    var flags = reader.ReadInt32();
                                    var sub_x = reader.ReadUInt32();
                                    var sub_y = reader.ReadUInt32();
                                    var nLayers = reader.ReadUInt32();
                                    var nDoodadRefs = reader.ReadUInt32();
                                    var holeshigh = reader.ReadUInt64();
                                    var ofsLayer = reader.ReadUInt32();
                                    var ofsRefs = reader.ReadUInt32();
                                    var ofsAlpha = reader.ReadUInt32();
                                    var sizeAlpha = reader.ReadUInt32();
                                    var ofsShadow = reader.ReadUInt32();
                                    var sizeShadow = reader.ReadUInt32();
                                    var areaid = reader.ReadUInt32();
                                    var nMapObjRefs = reader.ReadUInt32();
                                    var holeslow = reader.ReadUInt16();
                                    impasses[sub_x, sub_y] = -1;
                                    adtflags = flags;
                                    mcnkflags[sub_x,sub_y] =f;
                                    holes_low_res[sub_x,sub_y] =holeslow;
                                    holes_high_res[sub_x,sub_y] =holeshigh;
                                    if (areaid == 0) {
                                        unknown[sub_x, sub_y] = 1;
                                    }
                                    else {
                                        Toolbox.CsvArea csvdefault = CsvAreas.Find(e => e.ID == (int)areaid);
                                        Toolbox.Area area = new Toolbox.Area();
                                        area.x = x;
                                        area.y = y;
                                        area.sub_x = sub_x;
                                        area.sub_y = sub_y;
                                        area.ID = (int) areaid;
                                        if (csvdefault != null) {
                                            area.ZoneName = csvdefault.ZoneName;
                                            area.AreaName_lang = csvdefault.AreaName_lang;
                                            area.ContinentID = csvdefault.ContinentID;
                                        }
                                        areas.Add(area);
                                    }
                                    if ((flags & 2) == 2)
                                        impasses[sub_x, sub_y] = 1;
                                    reader.BaseStream.Position = pos + 104;
                                    mcnkpos[0] = reader.ReadSingle();
                                    mcnkpos[1] = reader.ReadSingle();
                                    mcnkpos[2] = reader.ReadSingle();
                                    reader.BaseStream.Position = pos + 128;
                                    while (reader.BaseStream.Position<size+pos){
                                    // Console.WriteLine(size+pos + " <= " + reader.BaseStream.Position);
                                        var sub_magic = reader.ReadUInt32();
                                        var sub_size = reader.ReadUInt32();
                                        var sub_pos = reader.BaseStream.Position;
                                    // Console.WriteLine(sub_magic + " / " + sub_size + " / " + sub_pos);
                                        if (sub_magic == Toolbox.mk("MCVT")){
                                            //Console.WriteLine("parsing mcvt " + adt_name + " : " +x + " / " + y);
                                        // Console.WriteLine("mcvt");
                                            mcvt[sub_x,sub_y] = new float[145];
                                            for (int i = 0; i < 145; i++)
                                                {
                                                    var height = reader.ReadSingle() + mcnkpos[2];
                                                    if (height > main_wdt.max_height) {
                                                        //Console.WriteLine("new max in " + adt_name + " | " +main_wdt.max_height + " => " + height+ " ( " + i + " )");
                                                        main_wdt.max_height = height;
                                                    }
                                                    if (height < main_wdt.min_height) {
                                                        //Console.WriteLine("new min in " + adt_name + " | " +main_wdt.min_height + " => " + height + " ( " + i + " )");
                                                        main_wdt.min_height = height;
                                                    }
                                                    mcvt[sub_x,sub_y][i] = height;
                                                }
                                        }
                                        reader.BaseStream.Position = sub_pos + sub_size;
                                    }
                                }

                                if (magic == Toolbox.mk("MWMO")) // wmo filenames
                                {
                                    char c;
                                    int i = (int)size;
                                    while (i > 0)
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        while ((c = Convert.ToChar(reader.ReadByte())) != '\0')
                                        {
                                            sb.Append(c);
                                            i--;
                                        }
                                        i--;
                                        if (!filenameswmo.Contains(sb.ToString()))
                                        {
                                            filenameswmo.Add(sb.ToString());
                                        }
                                    }
                                }
                                if (magic == Toolbox.mk("MMDX")) // m2 filenames
                                {
                                    char c;
                                    int i = (int)size;
                                    while (i > 0)
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        while ((c = Convert.ToChar(reader.ReadByte())) != '\0')
                                        {
                                            sb.Append(c);
                                            i--;
                                        }
                                        i--;
                                        if (!filenamesm2.Contains(sb.ToString()))
                                        {
                                            filenamesm2.Add(sb.ToString());
                                        }
                                    }
                                }

                                if (magic == Toolbox.mk("MMID")) // m2 offset names
                                {
                                    while (reader.BaseStream.Position < pos + size)
                                    {
                                        mmid.Add(reader.ReadUInt32());
                                    }
                                }
                                if (magic == Toolbox.mk("MWID")) // wmo offset names
                                {
                                    while (reader.BaseStream.Position < pos + size)
                                    {
                                        mwid.Add(reader.ReadUInt32());
                                    }
                                }

                                if (magic == Toolbox.mk("MODF")) // placement WMO
                                {
                                    while (reader.BaseStream.Position < pos + size)
                                    {
                                        objects.Add(Toolbox.MakeObject(reader, 0, listfile, x, y, mwid, filenameswmo, filelist, IG_Obj));
                                    }
                                }
                                if (magic == Toolbox.mk("MDDF")) // placement m2
                                {
                                    while (reader.BaseStream.Position < pos + size)
                                    {
                                        objects.Add(Toolbox.MakeObject(reader,1, listfile, x, y, mmid, filenamesm2, filelist, IG_Obj));
                                    }
                                }

                                reader.BaseStream.Position = pos + size;
                            }
                        }
                    }
                }
                catch(Exception ex) {

                }
                adt_claims_tile = true;
            }
        }
    }
}
