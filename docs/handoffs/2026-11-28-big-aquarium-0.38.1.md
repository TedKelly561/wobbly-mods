# Big aquarium 0.38.1: fixes from test 26 (for the ROADWS3 session)

Written to the repo because Switchyard Hub returned "Bad Gateway" when the PM tried to post this on task 556
(2026-11-28). If the hub is back, this is the same note as the comment that should be there.

Test 26 on ROADWS3 (house menu 0.38.0) built everything (312 of 312 animals and plants, 3915 objects, 47 lights) and
every check passed: bounce pads, the slow swim and its exit door, fish following at the glass, feeding, loose
touch-pool animals and gift-shop toys, a dark jellyfish floor, and the frame rate. Four things to improve came back.
House menu 0.38.1 (commit 40cc47b) does them. Untested in game.

1. **Ripples** were "hard to tell". Now two counter-turning spotlights per spot, about five times brighter with a
   longer reach and a bolder pattern; the floor on F1 is a little lighter so they show. New setting in
   `BepInEx\config\wobblymods.housemenu.cfg`: `[Aquarium] RippleStrength` (1 = normal; try 2 if still faint, 0.5 if
   Wobblies look washed out under the lights). It applies the next time a Big aquarium is placed. F3's ripple stays
   faint on purpose.
2. **Signs.** The little animal is measured in the sign's own space, turned side-on, shrunk to fit the plaque and set
   just in front of it. If any still look wrong, the tank's name is enough (each tank has one sign at its right end).
3. **The rock that hid a tank.** Rocks are now low and in the back half of every tank; corals and ice keep off the
   window too. Which tank it was isn't known ("catfish cousins", F4 right wall back, is the PM's guess), so it is fixed
   for all of them.
4. **Gathering.** Fish that come over keep swimming loops (smaller ones) on a ring round the visitor. The river's
   redtails circle about 3 m out from a swimmer and keep most of their loop.

A Big aquarium that is already placed keeps the old behaviour: undo it and place a new one after the update.

Retest: ripples on F1, F2 and F4 (a photo of a floor), the signs, the F4 tank view, and the swim. Lights went from 47
to 57: say if the frame rate drops.

Also answered by test 26: the game's hands DO pick up our plain loose objects (a Rigidbody and a box or capsule
collider, nothing from the game), so hub task 557 (grab research) needs no decompiling and can be closed.
