#version 330 core

in vec3 v_world_position;

uniform vec3 u_camera_position;
uniform mat4 u_model;

uniform vec4 u_emission_color;
uniform vec3 u_background_color;
uniform float u_absorption;
uniform float u_density_scale;
uniform float u_density_threshold;
uniform float u_density_contrast;
uniform int u_model_type;

out vec4 FragColor;

const int VOLUME_MODEL_HOMOGENEOUS = 0;
const int VOLUME_MODEL_HETEROGENEOUS = 1;
const int VOLUME_MODEL_ABSORPTION_EMISSION = 2;

vec3 random3(vec3 c)
{
    float j = 4096.0 * sin(dot(c, vec3(17.0, 59.4, 15.0)));
    vec3 r;
    r.z = fract(512.0 * j);
    j *= 0.125;
    r.x = fract(512.0 * j);
    j *= 0.125;
    r.y = fract(512.0 * j);
    return r - 0.5;
}

float snoise(vec3 p)
{
    const float K1 = 0.333333333;
    const float K2 = 0.166666667;

    vec3 i = floor(p + (p.x + p.y + p.z) * K1);
    vec3 d0 = p - (i - (i.x + i.y + i.z) * K2);

    vec3 e = step(vec3(0.0), d0 - d0.yzx);
    vec3 i1 = e * (1.0 - e.zxy);
    vec3 i2 = 1.0 - e.zxy * (1.0 - e);

    vec3 d1 = d0 - (i1 - K2);
    vec3 d2 = d0 - (i2 - K1);
    vec3 d3 = d0 - (0.5 - 3.0 * K2);

    vec4 h = max(0.6 - vec4(
        dot(d0, d0),
        dot(d1, d1),
        dot(d2, d2),
        dot(d3, d3)
    ), 0.0);

    vec4 n = h * h * h * h * vec4(
        dot(d0, random3(i)),
        dot(d1, random3(i + i1)),
        dot(d2, random3(i + i2)),
        dot(d3, random3(i + 1.0))
    );

    return dot(n, vec4(31.316));
}

float snoiseFractal(vec3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    float amplitudeSum = 0.0;

    for (int octave = 0; octave < 5; ++octave)
    {
        value += amplitude * snoise(p * frequency);
        amplitudeSum += amplitude;
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    // Normalize the accumulated octaves before remapping them to [0, 1].
    return value / amplitudeSum;
}

float sampleDensity(vec3 position)
{
    float noiseValue = 0.5 + 0.5 * clamp(
        snoiseFractal(position * u_density_scale),
        -1.0,
        1.0
    );

    // Thresholding creates empty regions; contrast controls how quickly the
    // remaining regions become opaque.
    float density = max(
        (noiseValue - u_density_threshold) /
        max(1.0 - u_density_threshold, 0.0001),
        0.0
    );
    return pow(clamp(density, 0.0, 1.0), max(u_density_contrast, 0.0001));
}

vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax)
{
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;

    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);

    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    return vec2(tNear, tFar);
}

void main()
{
    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - rayOrigin);

    mat4 invModel = inverse(u_model);
    vec3 localOrigin = (invModel * vec4(rayOrigin, 1.0)).xyz;
    vec3 localDir = (invModel * vec4(rayDir, 0.0)).xyz;

    vec2 intersections = intersectAABB(
        localOrigin,
        localDir,
        vec3(-0.5),
        vec3(0.5)
    );

    float tNear = intersections.x;
    float tFar = intersections.y;
    if (tFar < tNear || tFar < 0.0)
    {
        discard;
    }

    tNear = max(tNear, 0.0);
    float distanceInside = tFar - tNear;

    // A fixed object-space step keeps the shader compatible with the controls
    // already exposed by VolumeMaterial while preserving enough detail in the
    // noise field.
    const float stepLength = 0.01;
    int stepCount = min(int(ceil(distanceInside / stepLength)), 1024);
    float position = tNear;
    float transmittance = 1.0;
    vec3 emittedRadiance = vec3(0.0);

    for (int stepIndex = 0; stepIndex < 1024; ++stepIndex)
    {
        if (stepIndex >= stepCount)
        {
            break;
        }

        float currentStepLength = min(stepLength, tFar - position);
        vec3 samplePosition = localOrigin +
            localDir * (position + 0.5 * currentStepLength);

        float density = 1.0;
        if (u_model_type != VOLUME_MODEL_HOMOGENEOUS)
        {
            density = sampleDensity(samplePosition);
        }

        float absorption = max(u_absorption, 0.0) * density;
        float sampleTransmittance = exp(-absorption * currentStepLength);

        if (u_model_type == VOLUME_MODEL_ABSORPTION_EMISSION)
        {
            emittedRadiance += transmittance *
                (1.0 - sampleTransmittance) *
                u_emission_color.rgb;
        }

        transmittance *= sampleTransmittance;
        position += currentStepLength;
    }

    vec3 finalColor = emittedRadiance + u_background_color * transmittance;
    FragColor = vec4(finalColor, 1.0);
}
