#version 330 core

in vec3 v_position;
in vec3 v_world_position;

uniform vec3 u_camera_position;
uniform mat4 u_model;

uniform vec4 u_color;
uniform vec3 u_background_color;
uniform float u_absorption;
uniform float u_step_length;
uniform bool u_heterogeneous;
uniform float u_noise_scale;
uniform vec3 u_noise_offset;

out vec4 FragColor;

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

    for (int octave = 0; octave < 5; ++octave)
    {
        value += amplitude * snoise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Ray-box intersection using the Slab Method
vec2 intersectAABB(
    vec3 rayOrigin,
    vec3 rayDir,
    vec3 boxMin,
    vec3 boxMax
)
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
    // 1. Initialize the ray in world space
    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - rayOrigin);

    // 2. Transform ray to object space
    mat4 invModel = inverse(u_model);

    vec3 localOrigin = (invModel * vec4(rayOrigin, 1.0)).xyz;
    vec3 localDir = (invModel * vec4(rayDir, 0.0)).xyz;

    // 3. Compute intersection with the cube
    vec2 intersections = intersectAABB(
        localOrigin,
        localDir,
        vec3(-0.5),
        vec3(0.5)
    );

    float tNear = intersections.x;
    float tFar = intersections.y;

    // Check if the ray intersects the volume
    if (tFar < tNear || tFar < 0.0)
    {
        discard;
    }

    tNear = max(tNear, 0.0);

    // Compute distance inside the volume
    float distanceInside = tFar - tNear;
    float opticalThickness = 0.0;

    if (u_heterogeneous)
    {
        float stepLength = max(u_step_length, 0.0001);
        float position = tNear;
        int stepCount = int(ceil(distanceInside / stepLength));

        for (int stepIndex = 0; stepIndex < 1024; ++stepIndex)
        {
            if (stepIndex >= stepCount)
            {
                break;
            }

            float currentStepLength = min(stepLength, tFar - position);
            vec3 samplePosition = localOrigin + localDir * (position + 0.5 * currentStepLength);
            float density = clamp(
                0.5 + 0.5 * snoiseFractal(samplePosition * u_noise_scale + u_noise_offset),
                0.0,
                1.0
            );

            opticalThickness += density * u_absorption * currentStepLength;
            position += currentStepLength;
        }
    }
    else
    {
        // Preserve the original homogeneous volume behavior.
        opticalThickness = distanceInside * u_absorption;
    }

    // Compute transmittance
    float transmittance = exp(-opticalThickness);

    // Compute final radiance
    vec3 finalColor = u_background_color * transmittance;

    FragColor = vec4(finalColor, 1.0);
}
