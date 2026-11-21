# Switchyard Hub baseline before the ClickUp import (taken 2026-11-20, about 09:00 UTC)

Purpose: the ClickUp import into the hub runs on Sat Nov 21. Afterwards the PM compares the hub with this
snapshot and reports duplicates, omissions and overwritten hub-first work (report only, no fixes).

This session (wobbly-mods PM) has never written to ClickUp, so nothing of this project should arrive from ClickUp.
Everything below was created in the hub ("hub-first"). No task here has a ClickUp id (ext_id is null).

## Project "🐟 Wobbly Life mods (Skylar & family)" (hub project id 21): 16 tasks

| Hub id | Status | updated_at (UTC) | Comments | Title |
|---|---|---|---|---|
| 490 | done | 2026-11-16T21:07:44Z | | Test: hold LB+RB and hold Map opens the house menu |
| 491 | done | 2026-11-16T23:28:46Z | | Connect spawning: house menu places real furniture |
| 492 | done | 2026-11-16T23:28:45Z | | Test 6: spawning + giant fish tank in game |
| 493 | done | 2026-11-17T03:20:39Z | | Test 8: animal tanks + build mode + Scout 0.4 (replaces test 7) |
| 494 | done | 2026-11-17T06:19:17Z | | Test 10: quick building + pufferfish fix + no-clip in the main world (replaces test 9) |
| 495 | done | 2026-11-17T06:42:52Z | | Test 11: 2 m build pieces + Piece size button |
| 496 | cancelled | 2026-11-17T08:29:01Z | | Test 12: auto-turn build pieces (continuous walls) |
| 499 | done | 2026-11-18T04:12:28Z | | Test 13: overnight house set (save/load house, copy/turn/resize, lamps, plants, room kits) |
| 500 | done | 2026-11-18T04:12:28Z | | Test 14: picture menu for Skylar (run AFTER test 13 is reported) |
| 502 | done | 2026-11-18T09:05:52Z | | Test 15: game sounds via FMOD + blinking party lights + leftover checks |
| 503 | cancelled | 2026-11-19T02:40:31Z | | Test 16: Catfish rides (car, helicopter, speed boat) |
| 504 | to do | 2026-11-19T04:20:01Z | 3 (last id 1311) | Set up REMOTEWS2 (living room): automatic mod updates + shared saves |
| 505 | to do | 2026-11-19T03:04:30Z | 2 (last id 1308) | Test 19: invincible catfish rides + Million! + seaweed fix (replaces test 16) |
| 509 | done | 2026-11-19T06:35:19Z | 1 | Home PC: post the game's furniture prop list (from Scout's 02_props.txt) as a Switchyard doc |
| 510 | to do | 2026-11-20T07:43:36Z | 5 (last id 1339) | Test 20: Dress up (cat, bunny, puppy, redtail, T-Rex costumes) |
| 514 | to do | 2026-11-20T07:40:15Z | 2 (last id 1340) | Test 21: Skylar's own redtail on the catfish rides + costume fix (0.21.0) |

No task has a due date. Docs in this project: id 4 "PC session: how to install a test build (read me first)"
(2,889 chars, updated 2026-11-17T03:24:13Z); id 13 "Wobbly Life furniture prop list" (47,559 chars, updated
2026-11-19T06:35:12Z). Newest handoff in this project at snapshot time: id 146 (2026-11-20T05:44:37Z).

## This project's jobs in "🧰 Local worker jobs (Legion's models)" (hub project id 20)

| Hub id | Status | updated_at (UTC) | Title |
|---|---|---|---|
| 498 | done | 2026-11-18T20:23:39Z | wobbly-mods: extra edge-case tests for HouseLayout (house save format) |
| 506 | done | 2026-11-19T04:25:52Z | wobbly-mods A: costume shape library (V3, ShapeMesh, ellipsoid, cone, cylinder) + tests |
| 507 | done | 2026-11-19T05:27:21Z | wobbly-mods B: costume tails and fins (Bezier, Tube, Extrude) + tests — after job A |
| 508 | done | 2026-11-19T05:17:59Z | wobbly-mods C: sort the game's props into house rooms (PropRooms) + tests |
| 511 | done | 2026-11-19T06:44:11Z | wobbly-mods D: preview renderer (draw ShapeMesh parts to a picture file) + tests |
| 512 | done | 2026-11-19T06:48:10Z | wobbly-mods E: redtail catfish built from shapes (RedtailModel) + tests — after jobs B and D |
| 513 | done | 2026-11-20T01:01:52Z | Trial: pull qwen3.8:27b on the Legion and re-run job 507 with it (Ted approved the 18 GB download) |
| 515 | done | 2026-11-20T01:12:36Z | wobbly-mods F: costume models from shapes (cat, bunny, puppy, T-Rex) with preview pictures |

(That project also holds other teams' tasks and docs: 489, 501, 520-522, docs 3, 5-9. Not this session's.)

## After the import: what to check

1. Duplicates: any title above appearing twice in either project; any task in project 21 with a ClickUp id; doubled comments on 504, 505, 509, 510, 514; doubled docs 4 or 13.
2. Omissions: none expected (no ClickUp changes from this session). Report "none expected, none found" or what turned up.
3. Hub-first work: every row above still present with the same title, status and updated_at (or newer because of real work), comment counts not lower, docs 4 and 13 same size or larger.
