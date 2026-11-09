#include "volumematerial.h"
#include "../application.h"

VolumeMaterial::VolumeMaterial()
{
    this->color = glm::vec4(1.0f);
    this->absorption = 1.0f;

    this->shader = Shader::Get(
        "res/shaders/basic.vs",
        "res/shaders/volume.fs"
    );
}

VolumeMaterial::~VolumeMaterial()
{
}

void VolumeMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
    this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
    this->shader->setUniform("u_camera_position", camera->eye);
    this->shader->setUniform("u_model", model);
    this->shader->setUniform("u_color", this->color);
    this->shader->setUniform("u_emission_color", this->color);
    this->shader->setUniform("u_absorption", this->absorption);
    this->shader->setUniform("u_density_scale", this->density_scale);
    this->shader->setUniform("u_density_threshold", this->density_threshold);
    this->shader->setUniform("u_density_contrast", this->density_contrast);
    this->shader->setUniform("u_model_type", (int)this->model_type);
    this->shader->setUniform("u_background_color", Application::instance->background_color);
}

void VolumeMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
    if (mesh && this->shader)
    {
        this->shader->enable();

        setUniforms(camera, model);

        mesh->render(GL_TRIANGLES);

        this->shader->disable();
    }
}

void VolumeMaterial::renderInMenu()
{
    ImGui::Text("Material Type: Volume");

    const char* model_names[] = { "Homogeneous Absorption", "Heterogeneous Absorption", "Absorption + Emission" };
    int current_model = static_cast<int>(this->model_type);
    if (ImGui::Combo("Volume Model", &current_model, model_names, IM_ARRAYSIZE(model_names)))
    {
        this->model_type = static_cast<VolumeModelType>(current_model);
    }

    ImGui::SliderFloat("Absorption Coefficient", &this->absorption, 0.0f, 8.0f);

    if (this->model_type != VOLUME_MODEL_HOMOGENEOUS)
    {
        ImGui::SliderFloat("Density Scale", &this->density_scale, 0.5f, 12.0f);
        ImGui::SliderFloat("Density Threshold", &this->density_threshold, 0.0f, 1.0f);
        ImGui::SliderFloat("Density Contrast", &this->density_contrast, 0.5f, 8.0f);
    }

    ImGui::ColorEdit3("Emission Color", (float*)&this->color);
}

