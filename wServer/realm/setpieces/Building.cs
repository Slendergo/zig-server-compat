﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.realm.worlds;

namespace wServer.realm.setpieces
{
    class Building : ISetPiece
    {
        public int Size { get { return 21; } }

        static readonly string Floor = "Brown Lines";
        static readonly string Wall = "Wooden Wall";

        Random rand = new Random();
        public void RenderSetPiece(World world, IntPoint pos)
        {
            int w = rand.Next(19, 22), h = rand.Next(19, 22);
            int[,] t = new int[w, h];
            for (int x = 0; x < w; x++)                     //Perimeter
            {
                t[x, 0] = 1;
                t[x, h - 1] = 1;
            }
            for (int y = 0; y < h; y++)
            {
                t[0, y] = 1;
                t[w - 1, y] = 1;
            }

            int midPtH = h / 2 + rand.Next(-2, 3);          //Mid hori wall
            int sepH = rand.Next(2, 4);
            if (rand.Next() % 2 == 0)
            {
                for (int x = sepH; x < w; x++)
                    t[x, midPtH] = 1;
            }
            else
            {
                for (int x = 0; x < w - sepH; x++)
                    t[x, midPtH] = 1;
            }

            int begin, end;
            if (rand.Next() % 2 == 0)
            {
                begin = 0; end = midPtH;
            }
            else
            {
                begin = midPtH; end = h;
            }

            int midPtV = w / 2 + rand.Next(-2, 3);          //Mid vert wall
            int sepW = rand.Next(2, 4);
            if (rand.Next() % 2 == 0)
            {
                for (int y = begin + sepW; y < end; y++)
                    t[midPtV, y] = 1;
            }
            else
            {
                for (int y = begin; y < end - sepW; y++)
                    t[midPtV, y] = 1;
            }
            for (int x = 0; x < w; x++)                     //Flooring
                for (int y = 0; y < h; y++)
                    if (t[x, y] == 0)
                        t[x, y] = 2;

            for (int x = 0; x < w; x++)                     //Corruption
                for (int y = 0; y < h; y++)
                    if (rand.Next() % 2 == 0)
                        t[x, y] = 0;

            int rotation = rand.Next(0, 4);                 //Rotation
            for (int i = 0; i < rotation; i++)
                t = SetPieces.rotateCW(t);
            w = t.GetLength(0); h = t.GetLength(1);

            var dat = world.Manager.Resources.GameData;
            for (int x = 0; x < w; x++)                     //Rendering
                for (int y = 0; y < h; y++)
                {
                    if (t[x, y] == 1)
                    {
                        var tile = world.Map[x + pos.X, y + pos.Y];
                        tile.ObjType = dat.IdToObjectType[Wall];
                        tile.ObjDesc = dat.ObjectDescs[tile.ObjType];
                        if (tile.ObjId == 0) tile.ObjId = world.GetNextEntityId();
                        tile.UpdateCount++;
                    }
                    else if (t[x, y] == 2)
                    {
                        var tile = world.Map[x + pos.X, y + pos.Y];
                        tile.TileId = dat.IdToTileType[Floor];
                        tile.ObjType = 0;
                        tile.UpdateCount++;
                    }
                }
        }
    }
}
