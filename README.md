This branch has the automatic texture merging and painting code to be used with the "xukmi/SkinPlusTess" shaders.  It attempts to "paint" clothing depression to the body mesh.

## Note
It's just a proof of concept and is extremely slow, with medicore results (Like 5-10 minutes slow).
Set plugin config keybind to trigger texture merge

## Description:
The script finds every cloth mesh, and grabs their textures.  It then raycasts to the body for each pixel int he texture.  Where it hits it decides what color to paint the body pixel based ont teh cloth above it.  Once done it takes the new body displacement texture and loads it into the shader (if the shader exists on the character).

It will also spit the texture out to your root game directory if you wan to see the result