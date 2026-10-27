static const float3 FaceN[6] = { float3( -1,0,0), float3( 1,0,0), float3(0,-1,0), float3(0,1,0), float3(0,0,-1), float3(0,0, 1) };
static const float3 FaceB[6] = { float3(0,1,0),  float3(0,0,1),  float3(0,0,1),  float3(1,0,0),  float3(1,0,0),  float3(0,1,0)  };
static const float3 FaceT[6] = { float3(0,0,1),  float3(0,1,0),  float3(1,0,0),  float3(0,0,1),  float3(0,1,0),  float3(1,0,0)  };


static const float2 uv00 = float2(0,0);
static const float2 uv10 = float2(1,0);
static const float2 uv01 = float2(0,1);
static const float2 uv11 = float2(1,1);
static const float2 UVs[6][4] =
{
    { uv10, uv00, uv11, uv01 },
    { uv00, uv01, uv10, uv11 },
    { uv01, uv11, uv00, uv10 },
    { uv01, uv11, uv00, uv10 },
    { uv00, uv01, uv10, uv11 },
    { uv10, uv00, uv11, uv01 }
};

static const int EdgeIndices[6][4] =
{
    // {3, 5, 2, 4}, // -X
    // {3, 4, 2, 5}, // +X
    // {0, 5, 1, 4}, // -Y
    // {4, 0, 5, 1}, // +Y
    // {3, 0, 2, 1}, // -Z
    // {3, 1, 2, 0}  // +Z
    {3, 5, 2, 4}, // -X
    {3, 4, 2, 5}, // +X
    {0, 5, 1, 4}, // -Y
    {0, 4, 1, 5}, // +Y
    {3, 0, 2, 1}, // -Z
    {3, 1, 2, 0}  // +Z
};