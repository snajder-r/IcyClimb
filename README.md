# IcyClimb

This mini-game is my first practice VR project.

## Development log

### Ambiance
* Added some ocean sound affects at the bottom and wind sound affects at the top, turned on spatial blend and added ONSP Audio source from the native oculus spatializer. 
Thus, the higher you climb, the more you get wind sounds and less ocean sounds.

### Cliff design

* Started by designing the first cliff "level" in Inkscape with a possible solution of where the player might place climbing anchors.
* Based on that design I created a cliff mesh in blender using Metaballs. 
* For the texture I first created a shader graph in Blender and exported the baked textures. However, even with 8192x8192 textures this looked blurry since it's a large 100m tall cliff.
* I then changed the strategy and instead created a shader graph in unity so that the texture can be computed in unity. It doesn't look as good as what I produced in Blender, but it will do.

### Terrain setup

* Used a free unity asset "Snowy Cliff Materials" and set up a basic terrain.
* The inner terrain has higher quality and a second outer terrain with very low resolution was built to avoid seeing the void
* I set up a new procedural skybox using unity's Procedural Skybox shader
* Performance evaluation: I had about 170k Tris and over 240 draw calls. This was shocking seeing how little was on screen. 
What greatly helped was reducing the Terrain quality. Specifically raising the outer terrain Pixel error to the max improved draw calls significantly.
Enabling "Draw Instanced" also helped reduce the draw calls! I now have around 100k Tris and 200 draw calls. 
* Baked global illumination and tested on the Quest 2. Aside from what looks like some screen tares it looks good.
* Decided to switch to a different terrain on the beach. This eliminates having to draw mountains on side of the horizon. Using a free water shader from the unity store.

### Initial setup
* Unity 3D URP project with the following packages:
  * OpenXR and XR Interaction toolkit with samples and simulator
  * Native oculus spatializer
  * Did NOT install the Mixed Reality features. I want to see if I need them at all.
* Two builds: High quality for PC and Balanced for Android (targeting Oculus Quest 2)
* Fixed timestep set for 72Hz (0.0138)
