### stuff I had to ask AI for or that took way too long to debug

**how add velocity/force to player???**
- player has no rigidbody, so use `FirstPersonController`. either add to moveDirection vector, like force zones do, or use some variation of addForce.
- in general, force is meant to be applied over time/multiple frames, while impulses are for single use (FPC doesn't have impulse option tho...).
- my force still stopped short, I'm guessing because of friction. i solved this by teleporting the player upwards before launching.

**why isn't my marksman coin redirecting to me?**
- there's a layer 16 "SelfBody" that's different from 11 for "body". layer detection in `Gun.ShootServer` means that bullets will treat your own body like a wall.
- ALSO: was confused about trail not appearing but didn't know why I didn't look into the trail code... the trail starts from a shootPoint

**how do i like, do real mod dev in unity?**
- use assetripper to start the project. use it IN THE ROOT FOLDER OF THE GAME. find the asset bundle you want, and make a copy of the game folder with just those.

**why is my custom weapon crosshair weird?**
- each weapon defines its crosshair in itembehavior.
- default unity setting is to compress sprite dimensions as much as possible. find the sprite sheet for the crosshair and resize as necessary.

**why are weapon visuals breaking?**
- assetripper doesn't export shaders, which many visuals rely on. my solution to this is to make a copy of the material and export it with the mod, then modify the material shader w one pulled from the game at runtime.

**why is my weapon outline weird?**
- the outline relies on the mesh's scale. edit the import settings scale so that your game object can be 1, 1, 1.