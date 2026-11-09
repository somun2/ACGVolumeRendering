#version 330 core

in vec3 v_position;
in vec3 v_world_position;

uniform vec3 u_camera_position;
uniform mat4 u_model;


uniform vec4 u_color;
uniform vec3 u_background_color;
uniform float u_absorption;

out vec4 FragColor;

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

    // Compute optical thickness
    float opticalThickness = distanceInside * u_absorption;

    // Compute transmittance
    float transmittance = exp(-opticalThickness);

    // Compute final radiance
    vec3 finalColor = u_background_color * transmittance;

    FragColor = vec4(finalColor, 1.0);

}