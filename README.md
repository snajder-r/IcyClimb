# IcyClimb

![Banner](Assets/Logos/heroBanner.png)

IcyClimb is my first mini-project with Unity. Ascend an icy wall using your trusty ice picks and lead climbing.

## Attributions

This project exclusively uses free assets, either from the Unity store or through other channels.

The following content is used in this project:

* 'Permafrost' by Scott Buckley - released under CC-BY 4.0. [www.scottbuckley.com.au](www.scottbuckley.com.au)
* Wind loop and water Loop from: Nature Sound FX by Lumino [Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/nature-sound-fx-180413) | [Publisher Website](https://luminoassets.com/)
* Snow footsteps sound effects from: Footstep(Snow and Grass) by MGWSoundDesign [Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/footstep-snow-and-grass-90678) | [Publisher Website](https://soundcloud.com/valery-oleynikov)
* Slipping sound effect from: Footsteps Gravel by Sound Works 12 [Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/foley/footsteps-gravel-175348) | [Publisher Website](https://soundcloud.com/udk62msvdvkx)
* IceAxe by RRFreelance: [Asset Store](https://assetstore.unity.com/packages/3d/props/tools/ice-axe-20492#reviews) | [Publisher Website](https://www.robertramsay.co.uk/)
* Simple Water Shader URP by IgniteCoders: [Asset Store](https://assetstore.unity.com/packages/2d/textures-materials/water/simple-water-shader-urp-191449) | [Publisher Website](https://github.com/IgniteCoders)
* Yughues Free Sand Materials by Nobiax / Yughues: [Asset Store](https://assetstore.unity.com/packages/2d/textures-materials/floors/yughues-free-sand-materials-12964) | [Publisher Website](https://www.artstation.com/yughues)


## Development log

Below find some select highlights of elements that went into the development of IcyClimb.

### 3D Modeling

The cliff was modeled in Blender. It was first prototyped using metaballs, then converted to a mesh and details added using sculpting tools. 
A texture was generated procedurally using a shader consisting of about 40 nodes and then baked into a 12,288x12,888 texture and normals, as well as a 8192x8192 metallic map (baked from glossy).
The procedural shader was heavily inspired by [this Blender tutorial by YouTuber polygonartist.](https://www.youtube.com/watch?v=0Eg0uZDEktk).

In Unity, the texture is further complemented with a detail Texure from Yughues Free Sand Materials Asset pack.

![Modeling the cliff](DevLog/cliffblender.png)

Other objects I modeled in Blender were the wall anchors, the belay device, and the hands.
The hands were painted with two different materials, one using a procedural shader to create the knobbed texture of the palm grips, and the black texture mostly painted manually. 

![Other models created in Blender](DevLog/blender_other.png)

### Animation

The only animation I created for IcyClimb is that of closing your hand to grip something. 
Since I had never done this before, I learned from [CGDive's Blender tutorials on YouTube](https://www.youtube.com/watch?v=hdGkKbtQxE0).

I rigged the hand with a game rig and a control rig, and created two poses: 1) natural and 2) grabbing. 
The animation controller was kept simple.
A single bool parameter `b_grab` controls transitions. 
It can transition to the Grab state from any state except itself (that is, from Idle or Ungrab if `b_grab` is true. 
Only if `b_grab` is false, can it transition from Grab to Ungrab, which plays the grab animation in reverse From any state except the grab state (disallo) whether the animation controller transition.

![Rigging and animating the hand](DevLog/hand_animation.png)

### Sound design

Two ambient sound loops are used from the Nature Sound FX by Lumino from the Unity Asset store:
At the bottom of the cliff a spatial audio source plays the Water sound effect representing the sea, while at the top another spatial audio source plays the Wind loop. 
This way, as you ascend the cliff, the water becomes quieter and the wind becomes stronger.

As the player approaches about 80\% of the cliff's height. Permafrost by Scott Buckley is played and the credits appear over the horizon. You can probably guess that this was inspired by Kojima Productions' Death Stranding. 

A number of sound effects were taken from free Unity Assets from the Asset Store (see the Attributions section at the top of this page). 
These include:
* A random snow footstep sound whenever the player moves 0.75m
* A gravel footstep sound whenever the player loses solid ground (slips off a slope)

A few sound effects in the game are **home-made**, recorded with my regular Antlion ModMic (no professional audio equipment) and edited in Audacity:
* 6 different ice sound effects for when the ice pick or wall anchor lodged into ice: [1](Assets/Audio/SFX/ice1.wav)  [2](Assets/Audio/SFX/ice2.wav) [3](Assets/Audio/SFX/ice3.wav) [4](Assets/Audio/SFX/ice4.wav) [5](Assets/Audio/SFX/ice5.wav) [6](Assets/Audio/SFX/ice6.wav)     
What it is: Me smashing ice cubes with a stone mortar and pestle.
* 1 hand slipping sound played when you run out of stamina and thus lose your grip: [1](Assets/Audio/SFX/handslip1.wav)     
What it is: Me grabbing my Shinai and pulling on the Tsukagawa until I lose my grip and it slips out of my hand.
* 1 dislodge sound played when you dislodge the ice pick or the wall anchor from the ice: [1](Assets/Audio/SFX/dislodge1.wav)    
What it is: Me pulling a kitchen knife out of some ice cubes
* 1 wind-in-ear sound which is played when you fall very fast: [1](Assets/Audio/SFX/WindInEarLoop.wav)     
What it is: This is just be blowing into the microphone. Aside from general pitch/speed adjustments, I also looped it by copying the sound and reversing the copy, thus creating a smooth loop.

### Locomotion

There are three ways in which the player can move:
* Controller input: continuous movement and turning
* Pulling: The player can use the ice picks, lodge them in the wall, and then pull (or push) themselves to (from) the picks. 
The same principle of movement can also be used with rope. A player can grab the rope and pull themselves towards where they grabbed the rope. 
This can be done with both hands - also simultaneously.
* Falling: If the player stands on a slope, the player will slide down the slope. 
If the player does not have solid ground beneath their feet, they will also fall. 
However, the rope secures the player, so the player must fall as one would expect when secured with a rope.
That is to say, the player cannot overstretch the rope when falling and must "pendulum" towards the point where the rope would rest.

Since these three forms of movement are also highly connected, I implemented a single dedicated LocomotionProvider which implements all these features. 

At the basis for pulling movement is the `IPullProvider` interface, which provides the direction and strenght of pull, as well as whether this pull provider is strong enough to ignore gravity.
For example, lodging an ice pick into ice and pulling yourself towards it, does not initiate a fall.
However, grabbing the rope while climbing lead should still initiate a fall.

Whether or not a player falls is determined through a combination of the `CharacterController`'s `IsGrounded`, as well as an extra `SlopeScanner` I implemented to determine how even the ground is.

![Slope scanner](DevLog/slope_scanner.png)

The `SlopeScanner` emits a number of Rays diagonally in a circle around the player's feet. 
For each ray, the normal of the terrain is evaluated and an average normal is computed. 
The angle between the UP vector and the average normal is then used to determine whether the player can continue to move or will fall.
Furthermore, the average normal will be used to determine fall direction, such that players will fall away from the wall (in addition to gravity pulling them down), thus causing the player to slide.

A player can also lodge a wall anchor in the wall and secure the rope in it. 
When a player falls or is about to fall, the game computes the distance between the player and the wall anchor in order determine how much "rope" the player has. 
Like with a real belay device, the belay device at top of the belt "locks" the rope lenght when the player falls (when the rope comes out the top of the belay device).

![Slope scanner](DevLog/falling.png)

### Blindfold shader

### Rope physics

### Photospheres

### Visual effects

### Coding conventions

Coding conventions with regards to variable naming and class organization follow the propositions published by https://github.com/justinwasilenko/Unity-Style-Guide