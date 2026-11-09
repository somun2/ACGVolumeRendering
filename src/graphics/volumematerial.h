#pragma once

#include "../graphics/material.h"

class VolumeMaterial : public Material
{
public:
    VolumeMaterial();
    ~VolumeMaterial();

    void setUniforms(Camera* camera, glm::mat4 model) override;
    void render(Mesh* mesh, glm::mat4 model, Camera* camera) override;
    void renderInMenu() override;

    float absorption = 1.0f;



};

