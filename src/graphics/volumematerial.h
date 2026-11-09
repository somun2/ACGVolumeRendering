#pragma once

#include "../graphics/material.h"

enum VolumeModelType
{
    VOLUME_MODEL_HOMOGENEOUS = 0,
    VOLUME_MODEL_HETEROGENEOUS = 1,
    VOLUME_MODEL_ABSORPTION_EMISSION = 2
};

class VolumeMaterial : public Material
{
public:
    VolumeMaterial();
    ~VolumeMaterial();

    void setUniforms(Camera* camera, glm::mat4 model) override;
    void render(Mesh* mesh, glm::mat4 model, Camera* camera) override;
    void renderInMenu() override;

    VolumeModelType model_type = VOLUME_MODEL_HOMOGENEOUS;
    float absorption = 1.0f;
    float density_scale = 3.0f;
    float density_threshold = 0.45f;
    float density_contrast = 2.4f;
};

