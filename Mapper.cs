using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using CASCLib;
using System.IO;
using SereniaBLPLib;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using ImageMagick;

namespace CascStuff
{
    class Mapper
    {
        public static void Make_ZoomMap(Wdt map, string base_dir, CASCHandler cs)
        {
            string zoom_fullmap = base_dir + "/map_zoom.png";
            Console.WriteLine("Making zoom map:");
            for (var cur_x = 0; cur_x < 64; cur_x++)
            {
                for (var cur_y = 0; cur_y < 64; cur_y++)
                {
                    if (map.all_claimed_tiles[cur_x,cur_y] == true) {
                        //Console.WriteLine("exists " + cur_x + " /"  + cur_y);
                        if (cur_x < map.min_y) { map.min_y = cur_x;}
                        if (cur_y < map.min_x) { map.min_x = cur_y;}
                        if (cur_x > map.max_y) { map.max_y = cur_x;}
                        if (cur_y > map.max_x) { map.max_x = cur_y;}
                    }
                }
            }
            Console.WriteLine("minx " + map.min_x + " / miny " + map.min_y);
            Console.WriteLine("maxx " + map.max_x + " / maxy " + map.max_y);
            map.size_x = map.max_x - map.min_x +1;
            map.size_y = map.max_y - map.min_y +1;
            if ((map.size_x > 0) && (map.size_y> 0)){
                Console.WriteLine("size " + map.size_y + " | " + map.size_x);
                MagickImage canvas = new MagickImage(new MagickColor(255, 255, 255),  (map.size_y * Toolbox.tile_size),  (map.size_x * Toolbox.tile_size));
                canvas.Transparent(new MagickColor(255, 255, 255));
                Console.WriteLine("black netvip");
                for (var cur_x = 0; cur_x < 64; cur_x++)
                {
                    for (var cur_y = 0; cur_y < 64; cur_y++)
                    {
                        using (var stream = new MemoryStream())
                        {
                            var minit = map.maid_chunk.minit[cur_x, cur_y];
                            if ((minit != 0) && cs.FileExists((int) minit))
                            {
                                using (var stream2 = new MemoryStream())
                                {
                                    new BlpFile(cs.OpenFile((int) minit)).GetBitmap(0).Save(stream2, System.Drawing.Imaging.ImageFormat.Png);

                                    stream2.Position = 0;
                                    MagickImage image = new MagickImage(stream2);

                                    if (image.Width != Toolbox.tile_size)
                                    {
                                        if (Toolbox.tile_size == 512 && image.Width == 256)
                                        {
                                            var size = new MagickGeometry(image.Width * 2, image.Height * 2);
                                            size.IgnoreAspectRatio = false;
                                            image.Resize(size);
                                        }
                                        else if (Toolbox.tile_size == 256 && image.Width == 512)
                                        {
                                            var size = new MagickGeometry(image.Width * 0,5, image.Height * 0,5);
                                            size.IgnoreAspectRatio = false;
                                            image.Resize(size);
                                        }
                                    }
                                    canvas.CopyPixels(image, new MagickGeometry(image.Width, image.Height), (cur_y - map.min_y) * Toolbox.tile_size, (cur_x - map.min_x) * Toolbox.tile_size);

                                }
                            }
                        }
                    }
                }
                Console.WriteLine("writing zoom map");
                //canvas.Write(zoom_fullmap);
                CutZoomMap(map, canvas, base_dir+"/zoom_tiles", 10);
            }
        }

        
        /* Unref map*/
        static public void Make_UnrefMap(string base_dir,Toolbox.UnrefDef[,] unrefs, Wdt wdt)
        {
            Console.WriteLine("Making unref map:");
            string unrefjson = "[";
            for(int i = 0; i < 64; i++) {
                for(int j = 0; j < 64; j++) {
                    if (unrefs[i,j] != null) {
                        if ((unrefs[i,j].exists_adtname) || (unrefs[i,j].exists_obj0name) || (unrefs[i,j].exists_obj1name) || (unrefs[i,j].exists_minit) || (unrefs[i,j].adtname > 0) || (unrefs[i,j].obj0name > 0) || (unrefs[i,j].obj1name > 0) || (unrefs[i,j].minit > 0)) {
                            if (unrefjson != "[")
                                unrefjson += ",";
                            //Console.WriteLine(unrefs[i,j].x + " / " + unrefs[i,j].y + " | " + wdt.min_x + " / " + wdt.min_y);
                            unrefjson += "{\"x\":"+unrefs[i,j].x+",\"y\":"+unrefs[i,j].y+",\"exists_adtname\":\""+unrefs[i,j].exists_adtname+"\",\"exists_obj0name\":\""+unrefs[i,j].exists_obj0name+"\",\"exists_obj1name\":\""+unrefs[i,j].exists_obj1name+"\",\"exists_minit\":\""+unrefs[i,j].exists_minit+"\",\"adtname\":\""+unrefs[i,j].adtname+"\",\"obj0name\":\""+unrefs[i,j].obj0name+"\",\"obj1name\":\""+unrefs[i,j].obj1name+"\",\"minit\":\""+unrefs[i,j].minit+"\"}";
                        }
                    }
                }
            }
            unrefjson +="]";
            File.WriteAllText(base_dir + "/unrefs.json", unrefjson);
        }

        /* Area map*/
        static public void Make_AreaMap(string base_dir,Dictionary<int, List<Toolbox.Area>> Areas, Wdt wdt)
        {
            Console.WriteLine("Making area map:");
            string area_fullmap = base_dir + "/map_areas.png";
            var tilesize = 256;
            int size_per_mcnk = tilesize / 16;
            string areasjson = "[";
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            using (var areaall = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var area_graphics = System.Drawing.Graphics.FromImage(areaall)) {
                    areaall.MakeTransparent();
                    area_graphics.DrawImage(areaall, 0, 0, areaall.Width, areaall.Height);
                    foreach (KeyValuePair<int, List<Toolbox.Area>> area in Areas){
                        Random rnd = new Random();
                        var AreaColor = System.Drawing.Color.FromArgb(255/2,rnd.Next(255), rnd.Next(255), rnd.Next(255));
                        while(colors.Contains(AreaColor)) {
                            AreaColor = System.Drawing.Color.FromArgb(255/2,rnd.Next(255), rnd.Next(255), rnd.Next(255));
                        }
                        colors.Add(AreaColor);
                        if (areasjson != "[")
                            areasjson += ",";
                        //Console.WriteLine(AreaColor.R + " / " + AreaColor.G + " /  " + AreaColor.B);
                        areasjson +="{\"r\":"+(Math.Round(AreaColor.R * (1.0f-(64.0f/127f))))+",\"g\":" + (Math.Round(AreaColor.G * (1.0f-(64.0f/127f)))) + ",\"b\":"+(Math.Round(AreaColor.B* (1.0f-(64.0f/127f))))+",\"ID\":\""+area.Value[0].ID+"\",\"ZoneName\":\""+area.Value[0].ZoneName+"\",\"ContinentID\":"+area.Value[0].ContinentID+",\"AreaName_lang\":\""+area.Value[0].AreaName_lang+"\"}";
                        using (System.Drawing.SolidBrush AreaBrush = new System.Drawing.SolidBrush(AreaColor)) {
                            foreach(Toolbox.Area a in area.Value) {
                                area_graphics.FillRectangle(AreaBrush, ((a.y - wdt.min_y) * tilesize) + (a.sub_x * size_per_mcnk), ((a.x - wdt.min_x) * tilesize) + (a.sub_y * size_per_mcnk), size_per_mcnk, size_per_mcnk);
                            }
                        }
                    }
                }
                Console.WriteLine("writing area map");

                var stream = new MemoryStream();
                areaall.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                MagickImage image = new MagickImage(stream);
                var size = new MagickGeometry(image.Width * 2, image.Height * 2);
                size.IgnoreAspectRatio = false;
                image.Resize(size);
                //image.Write(area_fullmap);
                CutMap(image, base_dir + "/area_tiles", 10);
            }
            areasjson +="]";
            File.WriteAllText(base_dir + "/areas.json", areasjson);
        }

        /* Unknown map */
        static public void Make_UnknownMap(string base_dir,Adt[,] adts, Wdt wdt) {
            Console.WriteLine("Making unknown map:");
            string unknown_fullmap = base_dir + "/map_unknown.png";
            var tilesize = 256;
            int size_per_mcnk = tilesize / 16;
            //System.Drawing.SolidBrush unknown_brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(170, 255, 255, 255));


            MagickImage image = new MagickImage(new MagickColor(255, 255, 255, 0), wdt.size_y * tilesize, wdt.size_x * tilesize);
            for (int x  = 0; x < 64; x++) {
            for (int y = 0; y < 64; y++) {
                for (int sub_x = 0; sub_x< 16; sub_x++) {
                for (int sub_y = 0; sub_y< 16; sub_y++) {
                    if(adts[x,y].unknown[sub_x,sub_y] == 1) {

                        new Drawables()
                            .StrokeColor(new MagickColor(255, 255, 255, 170))
                            .FillColor(new MagickColor(255, 255, 255, 170))
                            .Rectangle((double)(((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk)), (double)(((x - wdt.min_x) * tilesize) + (sub_y * size_per_mcnk)), (double)(((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk)) + size_per_mcnk, (double)(((x - wdt.min_x) * tilesize) + (sub_y * size_per_mcnk)) + size_per_mcnk)
                                .Draw(image);
                    }
                }
                }
            }
            }
            var size = new MagickGeometry(image.Width * 2, image.Height * 2);
            size.IgnoreAspectRatio = false;
            image.Resize(size);
            //image.Write(unknown_fullmap);
            CutMap(image,base_dir+"/unknown_tiles", 10);
        }

        static public System.Drawing.Color GetColor(float orig_height, float cur_height, float max_height, float min_height, int tilesize){
            int A = 150;
            int R = 0;
            int G = 0;
            int B = 0;
            if ((orig_height >= 0) && (max_height != 0)){
                //int val = (int) ((orig_height/max_height) *765f);
                //int val  = (int) (Math.Log10((double) (orig_height)+1) * 106.6f);
                //Console.WriteLine(Math.Log10((double) (orig_height)+1));
                int val = (int) ((orig_height/max_height) *765f);
                if (val > 510) {
                    R = 255 - (val - 510);
                }
                else if (val > 255) {
                    G = 510 - val;
                    R = 255;
                }
                else {
                    R = val;
                    G = 255;
                }
                /*R = val;
                G = val;
                B = val;*/
                //Console.WriteLine(val + " / " + R + ","+G+","+B+ " / " +orig_height + " / " + cur_height + " / " + max_height + " / " + min_height);
            }
            else if ((orig_height == 0) && (max_height == 0)){
                G = 255;
            }
            else {
                int val = (int) ((orig_height/min_height) *510f);
                if (val > 255){
                    R = val - 255;
                    B = 255;
                }
                else {
                    G = 255 - val;
                    B = 255;
                }
                //Console.WriteLine(val + " / " + R + ","+G+","+B+ " / " +orig_height + " / " + cur_height + " / " + max_height + " / " + min_height);
            }
            return System.Drawing.Color.FromArgb(A,R,G,B);
        }
        /* Height map */
        static public void Make_MCVT(string base_dir,Adt[,] adts, Wdt wdt) {
            Console.WriteLine("Making mcvt map:");
            string height_fullmap = base_dir + "/map_height.png";
            var tilesize = 256;
            int size_per_mcnk = (tilesize / 16);
            float size_x_outer = size_per_mcnk / 9.0f;
            float size_y_outer = size_per_mcnk / 17.0f;
            float size_x_inner = size_per_mcnk / 8.0f;
            float size_y_inner = size_per_mcnk / 17.0f;
            using (var areaall = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var area_graphics = System.Drawing.Graphics.FromImage(areaall)) {
                    for (int x  = 0; x < 64; x++) {
                    for (int y = 0; y < 64; y++) {
                        for (int sub_x = 0; sub_x< 16; sub_x++) {
                        for (int sub_y = 0; sub_y< 16; sub_y++) {
                            if(adts[x,y].mcvt[sub_x,sub_y] != null){
                            for (int m = 0; m< 145; m++) {
                                int num_o = 0;
                                int num_i =0;
                                int cols = -1;
                                string where = "o"; // 1 = outer 2 inner
                                while (cols < m){
                                    cols += 9;
                                    num_o++;
                                    if (cols >= m){
                                        where = "o";
                                        break;
                                    }
                                    cols +=8;
                                    num_i++;
                                    where = "i";
                                }
                                int posx = m - ((num_o-1) *9) - ((num_i * 8));
                                if (where == "i")
                                    posx--;
                                var cur_height = adts[x,y].mcvt[sub_x,sub_y][m] - wdt.min_height;
                                System.Drawing.Color mcvt_color = GetColor(adts[x,y].mcvt[sub_x,sub_y][m], cur_height, wdt.max_height, wdt.min_height, tilesize);
                                System.Drawing.SolidBrush unknown_brush = new System.Drawing.SolidBrush(mcvt_color);
                                if (where == "o") {
                                    area_graphics.FillRectangle(unknown_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx*size_x_outer), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_outer, size_y_outer);
                                }
                                else {
                                    area_graphics.FillRectangle(unknown_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx * size_x_inner), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_inner, size_y_inner);
                                }
                                unknown_brush.Dispose();  
                                if ((isHoleHigh(adts[x,y].holes_high_res[sub_x,sub_y], num_o, num_i, posx, where)) || (isHoleLow(adts[x,y].holes_low_res[sub_x,sub_y], num_o, num_i, posx, where))) {
                                    HatchBrush hole_brush = new HatchBrush(HatchStyle.LargeGrid, Color.FromArgb(127,0,0,0),Color.FromArgb(0, 255, 255, 255));
                                    SolidBrush hole_fill = new SolidBrush(Color.FromArgb(127,0,0,0));
                                    if (where == "o") {
                                        area_graphics.FillRectangle(hole_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx*size_x_outer), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_outer, size_y_outer);
                                        area_graphics.FillRectangle(hole_fill, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx*size_x_outer), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_outer, size_y_outer);
                                    }
                                    else {
                                        area_graphics.FillRectangle(hole_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx * size_x_inner), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_inner, size_y_inner);
                                        area_graphics.FillRectangle(hole_fill, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx * size_x_inner), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_inner, size_y_inner);
                                    }
                                    hole_brush.Dispose();
                                    hole_fill.Dispose();
                                }
                            }
                            }
                        }
                        }
                    }
                    }
                }
                Console.WriteLine("writing mcvt height map");

                var stream = new MemoryStream();
                areaall.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                MagickImage image = new MagickImage(stream);
                var size = new MagickGeometry(image.Width * 2, image.Height * 2);
                size.IgnoreAspectRatio = false;
                image.Resize(size);
                //image.Write(height_fullmap);
                CutMap(image, base_dir + "/height_tiles", 10);
            }
        }

        /* Height map direct */
        static public void Make_MCVT_Direct(string base_dir,Adt[,] adts, Wdt wdt) {
            Console.WriteLine("Making mcvt direct map:");
            //Console.WriteLine(wdt.min_height + " / " + wdt.max_height);
            string height_direct_fullmap = base_dir + "/map_height_direct.png";
            var tilesize = 256;
            int size_per_mcnk = (tilesize / 16);
            //Console.WriteLine("sizes : " +(wdt.size_y*tilesize) + " / " +  (wdt.size_x*tilesize));
            float size_x_outer = size_per_mcnk / 9.0f;
            float size_y_outer = size_per_mcnk / 17.0f;
            float size_x_inner = size_per_mcnk / 8.0f;
            float size_y_inner = size_per_mcnk / 17.0f;
            using (var areaall = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var area_graphics = System.Drawing.Graphics.FromImage(areaall)) {
                    for (int x  = 0; x < 64; x++) {
                    for (int y = 0; y < 64; y++) {
                        for (int sub_x = 0; sub_x< 16; sub_x++) {
                        for (int sub_y = 0; sub_y< 16; sub_y++) {
                            if(adts[x,y].mcvt[sub_x,sub_y] != null){
                            for (int m = 0; m< 145; m++) {
                                int num_o = 0;
                                int num_i =0;
                                int cols = -1;
                                string where = "o"; // 1 = outer 2 inner
                                while (cols < m){
                                    cols += 9;
                                    num_o++;
                                    if (cols >= m){
                                        where = "o";
                                        break;
                                    }
                                    cols +=8;
                                    num_i++;
                                    where = "i";
                                }
                                int posx = m - ((num_o-1) *9) - ((num_i * 8));
                                if (where == "i")
                                    posx--;
                                var cur_height = adts[x,y].mcvt[sub_x,sub_y][m] - wdt.min_height;
                                float exact = adts[x,y].mcvt[sub_x,sub_y][m];
                                if (exact <0)
                                    exact *= -1;
                                var R = 0;
                                var G = Math.Truncate(exact / 255);
                                var B = exact - (G *255);
                                if (adts[x,y].mcvt[sub_x,sub_y][m] >=0)
                                    R +=100;
                                var d = Math.Truncate((exact - Math.Truncate(exact)) *100) ;
                                R += (int) d;
                                System.Drawing.Color mcvt_color = System.Drawing.Color.FromArgb(255,(int)R,(int)G,(int)B);
                                System.Drawing.SolidBrush unknown_brush = new System.Drawing.SolidBrush(mcvt_color);
                                if (where == "o") {
                                    area_graphics.FillRectangle(unknown_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx*size_x_outer), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_outer, size_y_outer);
                                }
                                else {
                                    area_graphics.FillRectangle(unknown_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx * size_x_inner), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_inner, size_y_inner);
                                }
                                unknown_brush.Dispose();  
                                if ((isHoleHigh(adts[x,y].holes_high_res[sub_x,sub_y], num_o, num_i, posx, where)) || (isHoleLow(adts[x,y].holes_low_res[sub_x,sub_y], num_o, num_i, posx, where))) {
                                    SolidBrush hole_fill = new SolidBrush(Color.FromArgb(255,255,255,255));
                                    if (where == "o") {
                                        area_graphics.FillRectangle(hole_fill, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx*size_x_outer), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_outer, size_y_outer);
                                    }
                                    else {
                                        area_graphics.FillRectangle(hole_fill, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk) + (posx * size_x_inner), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk) + (((num_o-1) * size_y_outer) + (num_i * size_y_inner)), size_x_inner, size_y_inner);
                                    }
                                    hole_fill.Dispose();
                                }
                            }
                            }
                        }
                        }
                    }
                    }
                }
                Console.WriteLine("writing mcvt height map");
                var stream = new MemoryStream();
                areaall.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                MagickImage image = new MagickImage(stream);
                var size = new MagickGeometry(image.Width * 2, image.Height * 2);
                size.IgnoreAspectRatio = false;
                image.Resize(size);
                //image.Write(height_direct_fullmap);
                CutMap(image, base_dir + "/height_direct_tiles", 10);
            }
        }

        /* Death borders */
        static public void Make_DeathMap(string output_dir, Adt[,] adts, Wdt wdt){
            JObject deathareas_json = new JObject();
            for (int x = 0; x < 64; x++){
                for (int y = 0; y < 64; y++){
                    if ( (adts[x,y].plane_min.a != null) && (adts[x,y].plane_min.b != null) && (adts[x,y].plane_min.c != null) && (adts[x,y].plane_max.a != null) &&  (adts[x,y].plane_max.b != null) &&  (adts[x,y].plane_max.c != null))
                        deathareas_json[((y - wdt.min_y)).ToString("00")+"_"+((x - wdt.min_x)).ToString("00")] = "Min : "+adts[x,y].plane_min.a[2]+"/"+adts[x,y].plane_min.b[2]+"/"+adts[x,y].plane_min.c[2]+"<br> Max : "+adts[x,y].plane_max.a[2]+"/"+adts[x,y].plane_max.b[2]+"/"+adts[x,y].plane_max.c[2];
                }
            }
            File.WriteAllText(output_dir + "/deathareas.json", deathareas_json.ToString());
        }

        static public void CreateModelsJson(Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj,string map_dir,Wdt wdt, int x, int y){
           // Console.WriteLine("Making models json:");
            if (!Directory.Exists(map_dir+"/models"))
                Directory.CreateDirectory(map_dir+"/models");
            if ((wdt.wmo != null) && (wdt.found_unref == false)){
                string wmo_json = "[";
                wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                wmo_json += "]";
                File.WriteAllText(map_dir + "/wdt_model.json", wmo_json);
            }
            else {
                string models = "[";
                foreach(KeyValuePair<uint, List<Toolbox.IngameObject>> obj in IG_Obj){
                    if (models != "[")
                        models +=",";
                    models += "{\"name\":\"" + obj.Value[0].name + "\",\"coords\":[";
                    var b = false;
                    foreach (Toolbox.IngameObject o in obj.Value){
                        if (b == true)
                            models += ",";
                        models += "{\"posx\":" + o.posz + ",\"posy\":" + o.posx+",\"posz\":"+o.posy+"}";
                        if (b == false)
                            b =true;
                    }
                    models += "],\"type\":" + obj.Value[0].type + ",\"id\":\""+obj.Key+"\"}";
                }
                models += "]";
                if (IG_Obj.Count > 0)
                    File.WriteAllText(map_dir + "/models/models"+x.ToString("00")+"_"+y.ToString("00")+".json", models);
                            
                if ((wdt.wmo != null) && (wdt.found_unref == false)){
                    string wmo_json = "[";
                    wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                    wmo_json += "]";
                    File.WriteAllText(map_dir + "/wdt_model.json", wmo_json);
                }
            }
        }

        static public bool isHoleLow(ushort holes_low_res, int num_o, int num_i, int posx, string where){
            byte[] bytes = BitConverter.GetBytes(holes_low_res);
            BitArray bits = new BitArray(bytes);
            bool[,] bitmap = new bool[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    bitmap[i, j] = bits[(j * 4) + i];
            num_o--;
            var edge_o = num_o % 2;
            var edge_x = posx % 2;
            var isHole = false;
            if (where == "o") {
                if (edge_o == 0) {
                    if (posx==8) {
                        if (num_o ==8)
                            isHole = bitmap[(posx/2)-1,(num_o/2)-1];
                        else if (num_o == 0)
                            isHole = bitmap[(posx/2)-1,(num_o/2)];
                        else
                            isHole = bitmap[(posx/2)-1,(num_o/2)-1] || bitmap[(posx/2)-1,(num_o/2)];
                    }
                    else if (posx==0){
                        if (num_o ==8)
                            isHole = bitmap[(posx/2),(num_o/2)-1];
                        else if (num_o == 0)
                            isHole = bitmap[(posx/2),(num_o/2)];
                        else
                            isHole = bitmap[(posx/2),(num_o/2)-1] || bitmap[(posx/2),(num_o/2)];
                    }
                    else {
                        if (edge_x == 0) {
                            if (num_o ==8)
                                isHole = bitmap[(posx/2),(num_o/2)-1] || bitmap[(posx/2)-1,(num_o/2)-1];
                            else if (num_o == 0)
                                isHole = bitmap[(posx/2),(num_o/2)] || bitmap[(posx/2)-1,(num_o/2)];
                            else
                                isHole = bitmap[(posx/2),(num_o/2)-1] || bitmap[(posx/2),(num_o/2)] || bitmap[(posx/2)-1,(num_o/2)] || bitmap[(posx/2)-1,(num_o/2)-1];
                        }
                        else {
                            if (num_o ==8)
                                isHole = bitmap[(posx/2),(num_o/2)-1];
                            else if (num_o == 0)
                                isHole = bitmap[(posx/2),(num_o/2)];
                            else
                                isHole = bitmap[(posx/2),(num_o/2)-1] || bitmap[(posx/2),(num_o/2)];
                        }
                    }
                }
                else {
                    if (edge_x == 0) {
                        if (posx == 8)
                            isHole = bitmap[(posx/2)-1,(num_o/2)];
                        else if (posx==0)
                            isHole = bitmap[(posx/2),(num_o/2)];
                        else
                            isHole = bitmap[(posx/2),(num_o/2)] || bitmap[(posx/2)-1,(num_o/2)];
                    }
                    else {
                            isHole = bitmap[(posx/2),(num_o/2)];
                    }
                }
            }

            if (where == "i"){
                isHole = bitmap[(posx/2),(num_i-1)/2];
            }
            return isHole;
        }
        static public bool isHoleHigh(ulong holes_high_res, int num_o, int num_i, int posx, string where){
            bool[,] bitmap = new bool[8, 8];
            byte[] bytes = BitConverter.GetBytes(holes_high_res);
            BitArray bits = new BitArray(bytes);
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++){
                    bitmap[i, j] = bits[(j * 8) + i];
                }
            }
            num_o--;
            var isHole = false;
            if (where == "o") {
                if ((posx == 0) || (posx == 8)) { // check i 
                    if ((num_o == 0) || (num_o == 8)) {
                        if (posx ==8) {
                            if (num_o == 8)
                                isHole = bitmap[posx-1,num_o-1];
                            else
                                isHole = bitmap[posx-1,num_o];
                        }
                        else {
                            if (num_o == 8)
                                isHole = bitmap[posx,num_o-1];
                            else
                                isHole = bitmap[posx,num_o];
                        }
                    }
                    else {
                        if (posx == 8)
                            isHole = bitmap[posx-1,num_o] || bitmap[posx-1,num_o-1];
                        else
                            isHole = bitmap[posx,num_o] || bitmap[posx,num_o-1];
                    }
                }
                else { // check i and i-1
                    if ((num_o == 0) || (num_o == 8)) {
                        if (num_o == 8)
                            isHole = bitmap[posx,num_o-1] || bitmap[posx-1,num_o-1];
                        else
                            isHole = bitmap[posx,num_o] || bitmap[posx-1,num_o];
                    }
                    else {
                        isHole = bitmap[posx,num_o] || bitmap[posx-1,num_o] || bitmap[posx,num_o-1] || bitmap[posx-1,num_o-1];
                    }
                }
            }

            if (where == "i"){
                isHole = bitmap[posx,num_i-1];
            }
            return isHole;
        }
        static public void Make_Models(string output_dir, Wdt wdt,Adt[,] adts, ObjAdt[,] obj0, ObjAdt[,] obj1){
            
            Console.WriteLine("Making models json:");
            Dictionary<uint, List<Toolbox.IngameObject>> listobj = new Dictionary<uint, List<Toolbox.IngameObject>>();
            if (!Directory.Exists(output_dir+"/models"))
                Directory.CreateDirectory(output_dir+"/models");
                
            if ((wdt.wmo != null) && (wdt.found_unref == false)){
                string wmo_json = "[";
                wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                wmo_json += "]";
                File.WriteAllText(output_dir + "/wdt_model.json", wmo_json);
            }
            else {
                for (int x  = 0; x < 64; x++) {
                    for (int y = 0; y < 64; y++) {
                        listobj.Clear();
                        string models = "[";
                        foreach(Toolbox.IngameObject c in obj0[x,y].objects){
                            if (!listobj.ContainsKey(c.filedataid))
                                listobj.Add(c.filedataid, new List<Toolbox.IngameObject>());
                            listobj[c.filedataid].Add(c);
                            
                        }
                        foreach(Toolbox.IngameObject c in adts[x,y].objects){
                            if (!listobj.ContainsKey(c.filedataid))
                                listobj.Add(c.filedataid, new List<Toolbox.IngameObject>());
                            listobj[c.filedataid].Add(c);
                        }
                        foreach(Toolbox.IngameObject c in obj1[x,y].objects){
                            if (!listobj.ContainsKey(c.filedataid))
                                listobj.Add(c.filedataid, new List<Toolbox.IngameObject>());
                            listobj[c.filedataid].Add(c);
                        }
                        foreach(KeyValuePair<uint, List<Toolbox.IngameObject>> obj in listobj){
                            if (models != "[")
                                models +=",";
                            models += "{\"name\":\"" + obj.Value[0].name + "\",\"coords\":[";
                            var b = false;
                            foreach (Toolbox.IngameObject o in obj.Value){
                                if (b == true)
                                    models += ",";
                                models += "{\"posx\":" + o.posz + ",\"posy\":" + o.posx+",\"posz\":"+o.posy+"}";
                                if (b == false)
                                    b =true;
                            }
                            models += "],\"type\":" + obj.Value[0].type + ",\"id\":\""+obj.Key+"\"}";
                        }
                        models += "]";
                        if (listobj.Count > 0)
                            File.WriteAllText(output_dir + "/models/models"+x.ToString("00")+"_"+y.ToString("00")+".json", models);
                    }
                }
                if ((wdt.wmo != null) && (wdt.found_unref == false)){
                    string wmo_json = "[";
                    wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                    wmo_json += "]";
                    File.WriteAllText(output_dir + "/wdt_model.json", wmo_json);
                }
            }
        }

        /* Wdt borders */
        static public void Make_WdtBorders(string output_dir, Wdt wdt){
            string border_string = "[";
            for (int x = 0; x < 64; x++){
                for (int y = 0; y < 64; y++){
                    if (wdt.wdt_claimed_tiles[x, y])
                    {
                        if (x == 0 || !wdt.wdt_claimed_tiles[x - 1, y])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border left" + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+((x - wdt.min_y)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"],["+((x - wdt.min_y)/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"]]}}"; 
                        }
                        if (x == 63 || !wdt.wdt_claimed_tiles[x + 1, y])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border right" + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"],["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"]]}}"; 
                        }
                        if (y == 0 || !wdt.wdt_claimed_tiles[x, y - 1])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border top " + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+((x - wdt.min_y)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"],["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"]]}}"; 
                        }
                        if (y == 63 || !wdt.wdt_claimed_tiles[x, y + 1])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border bot " + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+(((x - wdt.min_y))/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"],["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"]]}}"; 
                        }
                    }
                }
            }
            border_string += "]";
            File.WriteAllText(output_dir + "/wdtborders.json", border_string);
        }

        /* Impass map */
        static public void Make_ImpassMap(string base_dir, Adt[,] adts, Wdt wdt)
        {
            Console.WriteLine("Making impass map:");
            string impass_fullmap = base_dir + "/map_impass.png";
            var tilesize = 256;
            int size_per_mcnk = tilesize / 16;
            Pen impass_pen = new Pen(Color.FromArgb(220, 255, 255, 0), 2.5f);
            Pen mid_impass_pen = new Pen(Color.FromArgb(220, 255, 0, 0), 2.5f);

            //Console.WriteLine(wdt.size_x + " / "+  wdt.size_y);

            using (var bitmap = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var g_impass = System.Drawing.Graphics.FromImage(bitmap)) {
                    bitmap.MakeTransparent();
                    g_impass.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                    for (int a = 0; a < 64;a++)
                    {
                        for (int b = 0; b < 64; b++)
                        {
                            for (int i = 0; i < 16;i++)
                            {
                                for (int j = 0; j < 16; j++)
                                {
                                    Point topright = new Point((b-wdt.min_y)*tilesize + (int)size_per_mcnk * (int)i,(a-wdt.min_x)*tilesize+(int)size_per_mcnk * ((int)j + 1));
                                    Point topleft = new Point((b-wdt.min_y)*tilesize +(int)size_per_mcnk * (int)i, (a-wdt.min_x)*tilesize+(int)size_per_mcnk * (int)j);
                                    Point bottomright = new Point((b-wdt.min_y)*tilesize +(int)size_per_mcnk * ((int)i + 1), (a-wdt.min_x)*tilesize+(int)size_per_mcnk * ((int)j + 1));
                                    Point botttomleft = new Point((b-wdt.min_y)*tilesize +(int)size_per_mcnk * ((int)i + 1), (a-wdt.min_x)*tilesize+(int)size_per_mcnk * (int)j);

                                    int up = 0;
                                    int down = 0;
                                    int left = 0;
                                    int right = 0;
                                    int current = adts[a,b].impasses[i, j];

                                    if (j > 0)
                                        left = adts[a,b].impasses[i, j - 1];
                                    else if (a > 0)
                                        left = adts[a-1,b].impasses[i, 15];
                                    else
                                        left = -1; // wall


                                    if (j < 15)
                                        right = adts[a,b].impasses[i, j +1];
                                    else if (a < 63)
                                        right = adts[a+1,b].impasses[i, 0];
                                    else
                                        right = -1; // wall


                                    if (i > 0)
                                        up = adts[a,b].impasses[i - 1, j];
                                    else if (b > 0)
                                        up = adts[a,b-1].impasses[15, j];
                                    else
                                        up = -1; // wall

                                    if (i <15)
                                        down = adts[a,b].impasses[i + 1, j];
                                    else if (b <63)
                                        down = adts[a,b+1].impasses[0, j];
                                    else
                                        down = -1; // wall

                                    if (current == 1)
                                    { 
                                        if (left == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, botttomleft);
                                        else if (left == -1)
                                            g_impass.DrawLine(impass_pen, topleft, botttomleft);
                                        if (right == 1)
                                            g_impass.DrawLine(mid_impass_pen, topright, bottomright);
                                        else if (right == -1)
                                            g_impass.DrawLine(impass_pen, topright, bottomright);
                                        if (up == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, topright);
                                        else if (up == -1)
                                            g_impass.DrawLine(impass_pen, topleft, topright);
                                        if (down == 1)
                                            g_impass.DrawLine(mid_impass_pen, botttomleft, bottomright);
                                        else if (down == -1)
                                            g_impass.DrawLine(impass_pen, botttomleft, bottomright);
                                    }
                                    else if (current == 0) {
                                        if (left == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, botttomleft);
                                        if (right == 1)
                                            g_impass.DrawLine(mid_impass_pen, topright, bottomright);
                                        if (up == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, topright);
                                        if (down == 1)
                                            g_impass.DrawLine(mid_impass_pen, botttomleft, bottomright);
                                    }
                                }
                            }
                        }
                    }
                }
                impass_pen.Dispose();
                mid_impass_pen.Dispose();
                var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                MagickImage image = new MagickImage(stream);
                var size = new MagickGeometry(image.Width * 2, image.Height * 2);
                size.IgnoreAspectRatio = false;
                image.Resize(size);
                //image.Write(impass_fullmap);
                CutMap(image, base_dir + "/impass_tiles", 10);
            }
        }
        
        public static void CutZoomMap(Wdt wdt, MagickImage image, string outdir, int maxzoom){
            for (var zoom = maxzoom; zoom > 1; zoom--)
            {
                var zoom_dir = outdir + "/"+zoom;
                //Console.WriteLine(zoom);

                if (zoom != maxzoom)
                {
                    var size = new MagickGeometry(image.Width / 2, image.Height / 2);
                    size.IgnoreAspectRatio = false;
                    image.Resize(size);
                }

                var width = image.Width;
                var height = image.Height;

                // Always make sure that the image is dividable by 256
                if (width % 256 != 0)
                {
                    width = (width - (width % 256) + 256);
                }

                if (height % 256 != 0)
                {
                    height = (height - (height % 256) + 256);
                }

                if ((image.Width < 256) || (image.Height < 256)) {
                    if (zoom < 10)
                        wdt.minNative = zoom+1;
                    else 
                        wdt.minNative = 7;
                    Console.WriteLine("ending cut, too small");
                    break;
                }

                if (!Directory.Exists(zoom_dir))
                {
                    Directory.CreateDirectory(zoom_dir);
                }

                var w = 0;
                for (var x = 0; x < width; x += 256)
                {
                    var h = 0;
                    for (var y = 0; y < height; y += 256)
                    {
                        MagickImage tempImg = new MagickImage(new MagickColor(255, 255, 255, 0), 256, 256);
                        tempImg.Transparent(new MagickColor(255, 255, 255));
                        var sizeX = 256;
                        var sizeY = 256;
                        if (x + 256 > image.Width)
                            sizeX = image.Width - x;
                        if (y + 256 > image.Height)
                            sizeY = image.Height - y;
                        tempImg.CopyPixels(image, new MagickGeometry(x, y, sizeX, sizeY));
                        tempImg.Write(System.IO.Path.Combine(zoom_dir, w + "-" + h + ".png"));
                        h++;
                    }
                    w++;
                }
            }
            Console.WriteLine("End cut");
            //File.Delete(inpng);
        }
        
        public static void CutMap(MagickImage image, string outdir, int maxzoom){
            for (var zoom = maxzoom; zoom > 4; zoom--)
            {

                var zoom_dir = outdir + "/"+zoom;
                //Console.WriteLine(zoom);

                if (zoom != maxzoom)
                {
                    var size = new MagickGeometry(image.Width / 2, image.Height / 2);
                    size.IgnoreAspectRatio = false;
                    image.Resize(size);
                }

                var width = image.Width;
                var height = image.Height;

                // Always make sure that the image is dividable by 256
                if (width % 256 != 0)
                {
                    width = (width - (width % 256) + 256);
                }

                if (height % 256 != 0)
                {
                    height = (height - (height % 256) + 256);
                }

                if ((image.Width < 256) || (image.Height < 256)) {
                    Console.WriteLine("ending cut, too small");
                    break;
                }

                if (!Directory.Exists(zoom_dir))
                {
                    Directory.CreateDirectory(zoom_dir);
                }

                var w = 0;
                for (var x = 0; x < width; x += 256)
                {
                    var h = 0;
                    for (var y = 0; y < height; y += 256)
                    {
                        MagickImage tempImg = new MagickImage(new MagickColor(255, 255, 255, 0), 256, 256);
                        tempImg.Transparent(new MagickColor(255, 255, 255));
                        var sizeX = 256;
                        var sizeY = 256;
                        if (x + 256 > image.Width)
                            sizeX = image.Width - x;
                        if (y + 256 > image.Height)
                            sizeY = image.Height - y;
                        tempImg.CopyPixels(image, new MagickGeometry(x, y, sizeX, sizeY));
                        tempImg.Write(System.IO.Path.Combine(zoom_dir, w + "-" + h + ".png"));
                        h++;
                    }
                    w++;
                }
            }
            Console.WriteLine("End cut");
            //File.Delete(inpng);
        }
    }    
}
