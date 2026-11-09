#include "volumenode.h"

VolumeNode::VolumeNode(const char* name)
    : SceneNode(name)
{
    this->type = NODE_VOLUME;
}

VolumeNode::~VolumeNode()
{
}