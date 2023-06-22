# IcyClimb

![Banner](Assets/Logos/heroBanner.png)

This mini-game is my first practice VR project.

## Attributions

This project exclusively uses free assets, either from the Unity store or through other channels.

The following content is used in this project:

* 'Permafrost' by Scott Buckley - released under CC-BY 4.0. www.scottbuckley.com.au

## Development log

### Locomotion

* When the IcePick hits the wall, I freeze the IcePick's RigidBody. I then compute for each IcePick the distance between the Attach point and the controller location. This is the "pull" exacted on the body. I then pull the body towards the picks. If both picks are lodged in the wall, I take the average pull.
* In order to make the pull smoother and feel like an effort, the pull is then normalized and multiplied with a fixed pull speed.

### Collision
* Using a Character Controller for player collision with floor and meshes, combined with the XR Character controller driver.
It's acting a bit weird, though, and climbs up on meshes. I haven't found a decent solution yet, other than to make the collider narrow enough that the curvature of the capsule is steep.
It's also important to make sure the Player Controller collider interacts only with walls and floor and nothing else, otherwise it can glitch out.
* I wrote a shader to blindfold the player if they try to push their had through a wall. Unfortunately it seems to me Camera Overlay UI canvasas don't support transparency, which forces me to use World Space UI. 
When this clips with meshes it sometimes makes it so that the player can see through meshes still, or see the edge of meshes. Something to fix...
I wrote a script "NoWallhack" which detects collision between the Camera's near clipping plane and blacks out the screen if it clips with a wall. 
It also takes into account the direction of the collision, using Physics.ComputePenetration to estimate where the collision occurs and how deep in we are.
The shader takes these as input and adjusts the blindfold accordingly. Thus, if I step into the wall with my right side, I can still see on the left side.
* The IcePick contains a trigger collider at the tip. This is so that the collision is allowed to partially penetrate the ice. When the wall gets triggered, it calls a listener on the IcePick.
The IcePick then freezes its own rigidbody so it gets "stuck" which then allows locomotion. If the player presses the trigger, they can free the IcePick.
  * A complication I am facing here is when I should consider the collision to occur. I tried to make it based on velocity, but I'm facing the problem that sometimes even fast movements seem to result in low velocity at the time the trigger is called. 
    Vice versa I have the problem that sometimes "grinding" the pick against the wall results in fast involuntary collision which is then accepted as a hit but shouldn't. 
	I tried also checking the angle of the motion, using the RigidBodys GetPointVelocity function and by computing its dot product with the Ice Pick tip's position. It didn't help, though.. Not sure how to improve it so that it registers if and only if the player inteded it.
  * I attempted two things to fix it, which improved it somewhat, but not greatly: 
    One is smoothing the velocity over the last 10 frames before computing the direction of movement. 
	The other is sending a raycast from the IcePickTip to the wall and checking the wall normal. If it deviates more than 45 degrees from the Tip forward we don't register the hit. 

### Ambiance
* Added some ocean sound affects at the bottom and wind sound affects at the top, turned on spatial blend and added ONSP Audio source from the native oculus spatializer. 
Thus, the higher you climb, the more you get wind sounds and less ocean sounds.
* When you reach a certain height (currently 80 meters) a song from Scott Buckley starts playing. It only plays once and doesn't loop. This is of course inspired by the Kojima Productions game Death Stranding :-)

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
