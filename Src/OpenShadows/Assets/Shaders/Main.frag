#version 450

struct DirectionalLightInfo
{
    vec3 Direction;
    float _padding;
    vec4 Color;
};

struct CameraInfos
{
    vec3 CameraPosition_WorldSpace;
    float _padding1;
    vec3 CameraLookDirection;
    float _padding2;
};

struct ClipPlaneInformation
{
    vec4 ClipPlane;
    int Enabled;
};

layout(set = 1, binding = 4) uniform LightInfo
{
    DirectionalLightInfo _LightInfo;
};

layout(set = 1, binding = 5) uniform CameraInfo
{
    CameraInfos _CameraInfo;
};

layout(set = 2, binding = 2) uniform texture2D SurfaceTexture;
layout(set = 2, binding = 3) uniform sampler RegularSampler;
layout(set = 2, binding = 4) uniform texture2D AlphaMap;
layout(set = 2, binding = 5) uniform sampler AlphaMapSampler;
layout(set = 3, binding = 3) uniform ClipPlaneInfo
{
    ClipPlaneInformation _ClipPlaneInfo;
};

bool InRange(float val, float min, float max)
{
    return val >= min && val <= max;
}

vec4 WithAlpha(vec4 baseColor, float alpha)
{
    return vec4(baseColor.xyz, alpha);
}

layout(location = 0) in vec3 Position_WorldSpace;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TexCoord;
layout(location = 0) out vec4 _outputColor_;

layout(constant_id = 100) const bool ClipSpaceInvertedY = true;
layout(constant_id = 101) const bool TextureCoordinatesInvertedY = false;
layout(constant_id = 102) const bool ReverseDepthRange = true;

vec2 ClipToUV(vec4 clip)
{
    vec2 ret = vec2((clip.x / clip.w) / 2 + 0.5, (clip.y / clip.w) / -2 + 0.5);
    if (ClipSpaceInvertedY || TextureCoordinatesInvertedY)
    {
        ret.y = 1 - ret.y;
    }

    return ret;
}

bool IsDepthNearer(float a, float b)
{
    if (ReverseDepthRange) { return a > b; }
    else { return a < b; }
}

void main()
{
    if (_ClipPlaneInfo.Enabled == 1)
    {
        if (dot(_ClipPlaneInfo.ClipPlane, vec4(Position_WorldSpace, 1)) < 0)
        {
            discard;
        }
    }

    float alphaMapSample = texture(sampler2D(AlphaMap, AlphaMapSampler), TexCoord).x;
    if (alphaMapSample == 0)
    {
        discard;
    }

    vec4 surfaceColor = texture(sampler2D(SurfaceTexture, RegularSampler), TexCoord);

    vec4 ambientLight = vec4(0.05f, 0.05f, 0.05f, 1.f);
    vec3 lightDir = -_LightInfo.Direction;
    vec4 directionalColor = ambientLight * surfaceColor;
    float lightIntensity = 0.f;
    vec4 directionalSpecColor = vec4(0, 0, 0, 0);
    vec3 vertexToEye = normalize(Position_WorldSpace - _CameraInfo.CameraPosition_WorldSpace);
    vec3 lightReflect = normalize(reflect(_LightInfo.Direction, Normal));
    float specularFactor = dot(vertexToEye, lightReflect);
    /*if (specularFactor > 0)
    {
        specularFactor = pow(abs(specularFactor), _MaterialProperties.SpecularPower);
        directionalSpecColor = vec4(_LightInfo.Color.xyz * _MaterialProperties.SpecularIntensity * specularFactor, 1.0f);
    }*/

    lightIntensity = clamp(dot(Normal, lightDir), 0, 1);
    if (lightIntensity > 0.0f)
    {
        directionalColor = surfaceColor * lightIntensity * _LightInfo.Color;
    }

    vec4 pointDiffuse = vec4(0, 0, 0, 1);
    vec4 pointSpec = vec4(0, 0, 0, 1);

    _outputColor_ = WithAlpha(clamp(directionalSpecColor + directionalColor + pointSpec + pointDiffuse, 0, 1), alphaMapSample);
}
