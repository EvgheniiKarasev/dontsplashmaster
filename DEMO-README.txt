DONT SPLASH — DEMO
Open Assets/Scenes/DontSplash.unity and press Play.
The level is generated when Play starts; an empty Scene view before Play is expected.
If references need repair: Dont Splash > Prepare demo.

Controls
PC: WASD/arrows, mouse look, Space jump, E hold valve, Q bottle, F flare, P pause.
Mobile: drag on left to move, drag on right to look, on-screen action buttons.
Assist mode on the title screen increases jump height and the water rescue window.

Content
9 themes x 3 route variants = 27 chambers; 648 main-route platforms.
Two valves per chamber, two intermediate safe islands, chapter/sector save.
Orange sinking tiles, moving platforms, turning bridges, false red floors, blackout.
Blind creature patrols independently and investigates the most recent sound.
Bottles redirect it; flares stun it for five seconds. Water allows a short escape.
Target first-play duration: approximately 20 minutes; not a timed lock or a measured guarantee.

Playgama
Uses Playgama Bridge 2.4.0. Startup waits for Bridge storage before loading progress.
Gameplay, host pause, audio state and interstitial events are connected.
Interstitials are requested at level completion and game over; Bridge controls frequency.
Progress uses Bridge Storage. CONTINUE restores the saved chapter and sector.
No rewarded ads or purchases are offered.

Web build
Dont Splash > Build Playgama release creates Build/Playgama without regenerating the scene.
Dont Splash > Apply Playgama release settings (no build) applies settings only.
Bridge Web template, Brotli compression with decompression fallback,
IL2CPP Release, preserved SDK callbacks and procedural collider types.
See PLAYGAMA-RELEASE.txt for submission and validation status.

Scope
Single-player; no multiplayer or paid progression.
The third-person character/UI asset packs and portal VFX are not required by
this first-person demo. The supplied monster and bottle models are used.

Visual update: nine architectural palettes, arcade/ruin/reservoir/pump/machine/service spaces; supplied cooler, first-aid kit, radio, barrel and emergency-button props. Linear mist hides geometry by 22–37 metres; labels and water fade into the same mist.

Creature update: native Monster 10 Attack 1 clip, procedural spine/neck idle and listening, spatial breathing/growl/water footsteps, telegraphed attack with impact at 0.48 seconds, flare interruption and camera impact. The Playgama port uses imported recorded clips for all audio.
Monster Play Mode validation: 24 checks passed; report Logs/CreaturePlayProbe.txt. Fixed false impact sounds on misses, footsteps while stationary, and reaction timers after respawn. Temporary QA scripts removed. Sound sources and PCM validated; subjective listening not performed. No build.


UI update: Layer Lab rounded button/frame/title prefabs with Sen SDF and Cairo SDF fonts replace the old immediate-mode UI. Canvas uses safe-area scaling and Input System UI events; mobile action zones use the same coordinates. TMP Essential Resources imported from the installed Unity UI package.

Water and FX: Stylized Water 2 mobile preset, separate PoolWaterMobile material, flat pool surface, depth enabled, no refraction/planar/environment reflections, waves, caustics or open-water foam. Native Unity fog retained. Patched the asset's Vertex.hlsl and ForwardPass.hlsl for Unity 6 / URP 17 OUTPUT_SH4 and SAMPLE_GI signatures; retain these changes when updating the asset. Three Lana FX prefabs (water blast, flash, shine) use a shared lightweight URP particle shader and two pooled instances per effect, with particle counts capped, no lights/collision/trails. Actual Web performance and compressed size remain unverified; no build was run.

Economy / lobby update (2026-10-30)
- New wallet starts with 80 coins. First visit to each checkpoint pays 40; first chamber clear pays 80; a first dry clear pays an additional 40; final escape adds 200. Optional dead-end coins pay 15.
- Reward claims persist per chamber and location: retries and reloading cannot duplicate them. New-run does not erase wallet, upgrades or reward history.
- UPGRADES and EQUIPMENT stalls in each arrival room. E / mobile HOLD USE at the pad, or SHOP while in the lobby. Pause/death > LOBBY / UPGRADES preserves the in-session checkpoint; shop > RETURN TO CHECKPOINT returns there.
- Permanent upgrade caps: speed +15%, jump velocity +9%, throwing range +60%, flare stun 7.25s. Three levels each. Speed/jump cost 120/180/240; range/flare cost 80/140/200.
- Reserve flare: 35 coins (cap 20). Three reserve bottles: 25 (cap 30). Free chamber supplies are consumed first; paid reserves are saved and are not erased by death or changing chambers.
- When bottles are exhausted, Q/mobile BOTTLE throws a free pebble with three splashes and a two-second cooldown.
- Water raised from -0.625 to +0.10; immersion threshold, buoyancy, splash FX and creature waterline coordinated.
- Full campaign remains 27 sections. At maximum upgraded speed, 27 x 213m / 6.095m/s is about 15.7 minutes of straight traversal alone, excluding route searches, valves and deaths. This is a layout estimate, not an observed full-run completion time; no artificial wait gate.
- Runtime scripts compile. Shop transactions and mobile presentation still need a hands-on Play Mode pass. No Web build made.
Research used for feature selection: https://bloxron.com/dont-splash and https://dontsplash-roblox.wiki/guides/equipment-and-upgrades/ . Fan guides disagree on stages/prices; this demo uses its own solo economy and balance.

POOLROOM LIGHTING / COLUMNS
- Added tiled square columns between all nine routes; checkpoint strips remain clear.
- Smaller world-space tiles, warm haze, green pool water and ceiling fixtures across the hall.
- Static arrival room uses two baked non-directional lightmaps (512 + 256).
- Runtime prefab: Assets/DontSplash/BakedLighting/Lobby.prefab.
- Bake source scene: Assets/DontSplash/BakedLighting/LobbyBake.unity.
- Regenerate in Edit Mode via Dont Splash > Bake lobby lighting. This does not build the game.
- Changing chamber geometry retains inexpensive procedural lighting; full halls are not lightmapped.
- Verified successful Unity bake, C# compilation and lobby rendering in Play Mode. No Web build.

Mobile controls: 11 synthetic multitouch checks passed in Unity Play Mode. Dedicated USE finger prevents camera drags from triggering interactions; pause/reset clears pending actions. Logs/MobileInputProbe.txt records the run. Physical device testing remains outstanding.
