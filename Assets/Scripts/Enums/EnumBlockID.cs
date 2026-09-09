using UnityEngine;

public enum BlockID : short
{
    Invalid = -1,
    Air = 0,
    Grass = 1, 
    Dirt = 2,
    Stone = 3,
    Planks = 4,
    Log = 5,
    Leaves = 6,
    StoneBricks = 7,
    ClayBricks=8, 
    
    Machine = 99,

    Rocky_Dirt = 10, 
    Stone_Sandstone = 11,
    Stone_Limestone = 12, 
    Stone_Dolomite = 13, 
    Stone_Marble = 14, 
    Stone_Shale = 15, 
    Stone_Slate = 16, 
    Stone_Basalt = 17,

    Sand = 20, GrassySand = 21,
    Silt = 30, GrassySilt = 31,
    Clay = 40, GrassyClay = 41,

    StoneBrick_Small = 101,
    StoneBrick_Long = 102,
    StoneSlab_H = 103,
    StoneSlab_V = 104,
    StoneTile_L = 105, 
    StoneTile_M = 106,

    Tiles_2x2 = 120, 
    Tiles_1x2 = 121, 
    Tiles_1x1 = 122,
    Tiles_1x1_Smooth = 123,

    WoodFloor_S = 150, 
    WoodFloor_L = 151,

    Color_Block = 200,

    Test = 999,
}