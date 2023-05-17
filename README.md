# IcyClimb

This mini-game is my first practice VR project.

## Development log

### Terrain setup

* Used a free unity asset "Snowy Cliff Materials" and set up a basic terrain.
* The inner terrain has higher quality and a second outer terrain with very low resolution was built to avoid seeing the void
* I set up a new procedural skybox using unity's Procedural Skybox shader
* Performance evaluation: I had about 170k Tris and over 240 draw calls. This was shocking seeing how little was on screen. 
What greatly helped was reducing the Terrain quality. Specifically raising the outer terrain Pixel error to the max improved draw calls significantly.
I now have around 100k Tris and 200 draw calls.
* Baked global illumination and tested on the Quest 2. Aside from what looks like some screen tares it looks good.

### Initial setup
* Unity 3D URP project with the following packages:
  * OpenXR and XR Interaction toolkit with samples and simulator
  * Native oculus spatializer
  * Did NOT install the Mixed Reality features. I want to see if I need them at all.
* Two builds: High quality for PC and Balanced for Android (targeting Oculus Quest 2)
* Fixed timestep set for 72Hz (0.0138)
