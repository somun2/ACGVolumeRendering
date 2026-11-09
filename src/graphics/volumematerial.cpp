#include "volumeMaterial.h"
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
    this->shader->setUniform("u_absorption", this->absorption);
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

    ImGui::SliderFloat(
        "Absorption Coefficient",
        &this->absorption,
        0.0f,
        5.0f
    );
}

