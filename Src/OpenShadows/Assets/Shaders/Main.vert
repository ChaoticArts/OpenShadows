#version 450

struct DirectionalLightInfo
{
    vec3 Direction;
    float _padding;
    vec4 Color;
};

struct CameraInfo
{
    vec3 CameraPosition_WorldSpace;
    float _padding1;
    vec3 CameraLookDirection;
    float _padding2;
};

struct WorldAndInverseMats
{
    mat4 World;
    mat4 InverseWorld;
};

struct ClipPlaneInfo
{
    vec4 ClipPlane;
    int Enabled;
};

layout(set = 0, binding = 0) uniform Projection
{
    mat4 _Projection;
};

layout(set = 0, binding = 1) uniform View
{
    mat4 _View;
};

layout(set = 2, binding = 0) uniform WorldAndInverse
{
    WorldAndInverseMats _WorldAndInverse;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TexCoord;
layout(location = 0) out vec3 fsin_Position_WorldSpace;
layout(location = 1) out vec3 fsin_Normal;
layout(location = 2) out vec2 fsin_TexCoord;

void main()
{
    vec4 worldPosition = _WorldAndInverse.World * vec4(Position, 1);
    vec4 viewPosition = _View * worldPosition;
    gl_Position = _Projection * viewPosition;
    fsin_Position_WorldSpace = worldPosition.xyz;
    vec4 outNormal = _WorldAndInverse.InverseWorld * vec4(Normal, 1);
    fsin_Normal = normalize(outNormal.xyz);
    fsin_TexCoord = TexCoord;
}
